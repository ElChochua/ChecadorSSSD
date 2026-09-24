using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChecadorSSSD.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChecadorSSSD.ViewModels;

public class ChecadorViewModel : ViewModelBase, IDisposable
{
    private readonly FingerprintService _fingerprintService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ImageService _imageService;

    private string _horaActual = DateTime.Now.ToString("HH:mm:ss");
    private string _fechaActual = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy").ToUpper();
    private string _matricula = string.Empty;
    private string _mensaje = string.Empty;
    private bool _esError;
    private bool _mostrarMensaje;
    private bool _lectorHuellaConectado;
    private string _estadoLector = "Lector no detectado";

    private Bitmap? _logoPrograma;
    private Bitmap? _imagenLugar;
    private Bitmap? _imagenUniversidad;
    private Bitmap? _imagenPersona;
    private bool _mostrarFotoPersona;

    public string HoraActual { get => _horaActual; set => SetProperty(ref _horaActual, value); }
    public string FechaActual { get => _fechaActual; set => SetProperty(ref _fechaActual, value); }
    public string Matricula { get => _matricula; set => SetProperty(ref _matricula, value); }
    public string Mensaje { get => _mensaje; set => SetProperty(ref _mensaje, value); }
    public bool EsError { get => _esError; set => SetProperty(ref _esError, value); }
    public bool MostrarMensaje { get => _mostrarMensaje; set => SetProperty(ref _mostrarMensaje, value); }
    public bool LectorHuellaConectado { get => _lectorHuellaConectado; set => SetProperty(ref _lectorHuellaConectado, value); }
    public string EstadoLector { get => _estadoLector; set => SetProperty(ref _estadoLector, value); }

    public Bitmap? LogoPrograma { get => _logoPrograma; set => SetProperty(ref _logoPrograma, value); }
    public Bitmap? ImagenLugar { get => _imagenLugar; set => SetProperty(ref _imagenLugar, value); }
    public Bitmap? ImagenUniversidad { get => _imagenUniversidad; set => SetProperty(ref _imagenUniversidad, value); }
    public Bitmap? ImagenPersona { get => _imagenPersona; set => SetProperty(ref _imagenPersona, value); }
    public bool MostrarFotoPersona { get => _mostrarFotoPersona; set => SetProperty(ref _mostrarFotoPersona, value); }
    public bool HasLogoPrograma => LogoPrograma != null;
    public bool HasImagenLugar => ImagenLugar != null;
    public bool HasImagenUniversidad => ImagenUniversidad != null;

    public ICommand RegistrarCommand { get; }
    public ICommand ReconectarLectorCommand { get; }

    public ChecadorViewModel(FingerprintService fingerprintService, IServiceScopeFactory scopeFactory)
    {
        _fingerprintService = fingerprintService;
        _scopeFactory = scopeFactory;
        _imageService = new ImageService();

        RegistrarCommand = new RelayCommand(async () => await RegistrarAsync());
        ReconectarLectorCommand = new RelayCommand(async () => await InicializarLectorHuellaAsync());

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            HoraActual = DateTime.Now.ToString("HH:mm:ss");
            FechaActual = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy").ToUpper();
        };
        timer.Start();

        CargarImagenes();
        AppMessenger.ImagesChanged += OnImagesChanged;
        AppMessenger.PersonasChanged += OnPersonasChanged;
    }

    private async Task InicializarLectorHuellaAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            EstadoLector = "Inicializando lector...";
            LectorHuellaConectado = false;
        });

        await Task.Run(() => _fingerprintService.Inicializar());

        if (_fingerprintService.IsAvailable)
        {
            _fingerprintService.IniciarVerificacion(OnHuellaMatch, OnHuellaNoMatch);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EstadoLector = "Lector listo - Coloque su dedo";
                LectorHuellaConectado = true;
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EstadoLector = "Lector no disponible - Use su matricula";
                LectorHuellaConectado = false;
            });
        }
    }

    private async Task DetenerVerificacionAsync()
    {
        await Task.Run(() => _fingerprintService.DetenerVerificacion());
    }

    private void OnHuellaMatch(string matricula)
    {
        FingerprintLogger.LogMatch($"OnHuellaMatch: Matricula={matricula} - Iniciando marcado de asistencia");
        _ = MarcarAsistenciaHuellaAsync(matricula);
    }

    private void OnHuellaNoMatch()
    {
        FingerprintLogger.LogNoMatch("OnHuellaNoMatch: Huella no reconocida");
        Dispatcher.UIThread.Post(() =>
        {
            Mensaje = "Huella no reconocida, intente de nuevo";
            EsError = true;
            MostrarMensaje = true;
        });
        _ = OcultarMensajeDelayed(3000);
    }

    private async Task MarcarAsistenciaHuellaAsync(string matricula)
    {
        FingerprintLogger.LogInfo($"MarcarAsistenciaHuellaAsync: matricula={matricula}");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var checadorService = scope.ServiceProvider.GetRequiredService<ChecadorService>();
            var personasService = scope.ServiceProvider.GetRequiredService<PersonasService>();
            var (tipo, msg) = await checadorService.MarcarAsistenciaAsync(matricula);

            FingerprintLogger.LogInfo($"MarcarAsistenciaHuellaAsync: Resultado={tipo}, Mensaje={msg}");

            // Cargar foto de la persona
            try
            {
                var persona = await personasService.ObtenerPorMatriculaAsync(matricula);
                if (persona != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CargarFotoPersona(persona.Imagen));
                }
            }
            catch { }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LectorHuellaConectado = true;
                EstadoLector = $"Huella reconocida: {matricula}";
                Mensaje = msg;
                EsError = tipo == "error";
                MostrarMensaje = true;
            });

            _ = OcultarMensajeDelayed(3000);
            _ = ResetearEstadoLectorDelayed(3000);
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"MarcarAsistenciaHuellaAsync: Error - {ex}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Mensaje = $"Error: {ex.Message}";
                EsError = true;
                MostrarMensaje = true;
            });
        }
    }

    private void OnImagesChanged(object? sender, EventArgs e) => CargarImagenes();

    private void OnPersonasChanged(object? sender, EventArgs e)
    {
        try
        {
            _fingerprintService.RecargarGaleriaAsync().ConfigureAwait(false);
        }
        catch { /* ignorar */ }
    }

    private void CargarImagenes()
    {
        try
        {
            LogoPrograma = _imageService.LoadImage(_imageService.LogoChecadorPath);
            OnPropertyChanged(nameof(HasLogoPrograma));
            ImagenLugar = _imageService.LoadImage(_imageService.ImagenLugarPath);
            OnPropertyChanged(nameof(HasImagenLugar));
            ImagenUniversidad = _imageService.LoadImage(_imageService.ImagenUniversidadPath);
            OnPropertyChanged(nameof(HasImagenUniversidad));
        }
        catch { }
    }

    private async Task RegistrarAsync()
    {
        if (string.IsNullOrWhiteSpace(Matricula))
        {
            Mensaje = "Introduce una matricula.";
            EsError = true;
            MostrarMensaje = true;
            return;
        }

        FingerprintLogger.LogInfo($"Marcado MANUAL iniciado: Matricula={Matricula.Trim()}");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var checadorService = scope.ServiceProvider.GetRequiredService<ChecadorService>();
            var personasService = scope.ServiceProvider.GetRequiredService<PersonasService>();
            var (tipo, msg) = await checadorService.MarcarAsistenciaAsync(Matricula.Trim());
            FingerprintLogger.LogInfo($"Marcado MANUAL resultado: Tipo={tipo}, Mensaje={msg}");

            // Cargar foto de la persona
            try
            {
                var persona = await personasService.ObtenerPorMatriculaAsync(Matricula.Trim());
                if (persona != null)
                {
                    CargarFotoPersona(persona.Imagen);
                }
            }
            catch { }

            Mensaje = msg;
            EsError = tipo == "error";
            MostrarMensaje = true;
            Matricula = string.Empty;
            _ = OcultarMensajeDelayed(3000);
            _ = ResetearEstadoLectorDelayed(3000);
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Marcado MANUAL error: {ex}");
            Mensaje = $"Error: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
            _ = OcultarMensajeDelayed(3000);
        }
    }

    private async Task OcultarMensajeDelayed(int ms)
    {
        await Task.Delay(ms);
        MostrarMensaje = false;
        ImagenPersona = null;
        MostrarFotoPersona = false;
    }

    private void CargarFotoPersona(string? rutaImagen)
    {
        try
        {
            var path = rutaImagen;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                path = @"C:\FotografiasHuella\Dios.jpg";
            }

            if (File.Exists(path))
            {
                ImagenPersona = new Avalonia.Media.Imaging.Bitmap(path);
                MostrarFotoPersona = true;
            }
            else
            {
                ImagenPersona = null;
                MostrarFotoPersona = false;
            }
        }
        catch
        {
            ImagenPersona = null;
            MostrarFotoPersona = false;
        }
    }

    private async Task ResetearEstadoLectorDelayed(int ms)
    {
        await Task.Delay(ms);
        EstadoLector = "Lector listo - Coloque su dedo";
    }

    public async void Activar()
    {
        try
        {
            if (!_fingerprintService.IsAvailable)
            {
                FingerprintLogger.LogHardware("Lector no disponible, intentando inicializar...");
                _fingerprintService.Inicializar();
            }

            if (_fingerprintService.IsAvailable)
            {
                FingerprintLogger.LogInfo("Cargando galeria de huellas antes de iniciar verificacion...");
                await _fingerprintService.RecargarGaleriaAsync();
                FingerprintLogger.LogInfo("Galeria cargada exitosamente");
                _fingerprintService.IniciarVerificacion(OnHuellaMatch, OnHuellaNoMatch);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    EstadoLector = "Lector listo - Coloque su dedo";
                    LectorHuellaConectado = true;
                });
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    EstadoLector = "Lector no disponible";
                    LectorHuellaConectado = false;
                });
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[ChecadorViewModel] Activar error: {ex}");
            EstadoLector = "Error al activar lector";
            LectorHuellaConectado = false;
        }
    }

    public void Desactivar()
    {
        FingerprintLogger.LogInfo("=== Desactivando lector de huella ===");
        try
        {
            _fingerprintService.DetenerVerificacion();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Desactivar: Error - {ex}");
            Trace.TraceError($"[ChecadorViewModel] Desactivar error: {ex}");
        }

        EstadoLector = "Lector detenido";
        LectorHuellaConectado = false;
    }

    public void Dispose()
    {
        try
        {
            _fingerprintService.DetenerVerificacion();
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[ChecadorViewModel] Dispose error: {ex}");
        }
    }
}