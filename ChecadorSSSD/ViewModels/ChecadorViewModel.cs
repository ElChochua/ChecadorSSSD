using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChecadorSSSD.Services;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.ViewModels;

public class ChecadorViewModel : ViewModelBase, IDisposable
{
    private readonly ChecadorService _checadorService;
    private readonly ImageService _imageService;
    private readonly FingerprintReaderService _fingerprintReader;
    private readonly PersonasService _personasService;

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

    public string HoraActual
    {
        get => _horaActual;
        set => SetProperty(ref _horaActual, value);
    }

    public string FechaActual
    {
        get => _fechaActual;
        set => SetProperty(ref _fechaActual, value);
    }

    public string Matricula
    {
        get => _matricula;
        set => SetProperty(ref _matricula, value);
    }

    public string Mensaje
    {
        get => _mensaje;
        set => SetProperty(ref _mensaje, value);
    }

    public bool EsError
    {
        get => _esError;
        set => SetProperty(ref _esError, value);
    }

    public bool MostrarMensaje
    {
        get => _mostrarMensaje;
        set => SetProperty(ref _mostrarMensaje, value);
    }

    public bool LectorHuellaConectado
    {
        get => _lectorHuellaConectado;
        set => SetProperty(ref _lectorHuellaConectado, value);
    }

    public string EstadoLector
    {
        get => _estadoLector;
        set => SetProperty(ref _estadoLector, value);
    }

    private string _nombrePersona = string.Empty;
    private Bitmap? _imagenPersona;
    private bool _mostrarDatosPersona;

    public string NombrePersona
    {
        get => _nombrePersona;
        set => SetProperty(ref _nombrePersona, value);
    }

    public Bitmap? ImagenPersona
    {
        get => _imagenPersona;
        set => SetProperty(ref _imagenPersona, value);
    }

    public bool MostrarDatosPersona
    {
        get => _mostrarDatosPersona;
        set => SetProperty(ref _mostrarDatosPersona, value);
    }

    public bool HasImagenPersona => ImagenPersona != null;

    public Bitmap? LogoPrograma
    {
        get => _logoPrograma;
        set => SetProperty(ref _logoPrograma, value);
    }

    public bool HasLogoPrograma => LogoPrograma != null;

    public Bitmap? ImagenLugar
    {
        get => _imagenLugar;
        set => SetProperty(ref _imagenLugar, value);
    }

    public bool HasImagenLugar => ImagenLugar != null;

    public Bitmap? ImagenUniversidad
    {
        get => _imagenUniversidad;
        set => SetProperty(ref _imagenUniversidad, value);
    }

    public bool HasImagenUniversidad => ImagenUniversidad != null;

        public ICommand RegistrarCommand { get; }
        public ICommand ReconectarLectorCommand { get; }

    public ChecadorViewModel(ChecadorService checadorService, FingerprintReaderService fingerprintReader, PersonasService personasService)
    {
        _checadorService = checadorService;
        _fingerprintReader = fingerprintReader;
        _personasService = personasService;
        _imageService = new ImageService();
        RegistrarCommand = new RelayCommand(async () => await RegistrarAsync());
        ReconectarLectorCommand = new RelayCommand(async () => await InicializarLectorHuellaAsync());
        
        // Iniciar timer para actualizar el reloj cada segundo
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (s, e) => 
        {
            HoraActual = DateTime.Now.ToString("HH:mm:ss");
            FechaActual = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy").ToUpper();
        };
        timer.Start();

        // Cargar imagenes configurables
        CargarImagenes();

        AppMessenger.ImagesChanged += OnImagesChanged;

        // Inicializar lector de huella automaticamente al cargar la vista
        Task.Run(async () => await InicializarLectorHuellaAsync());
    }

    private async System.Threading.Tasks.Task InicializarLectorHuellaAsync()
    {
        try
        {
            EstadoLector = "Inicializando lector...";
            string resultado = _fingerprintReader.Initialize();
            if (_fingerprintReader.IsAvailable)
            {
                EstadoLector = "Lector listo - Coloque su dedo";
                LectorHuellaConectado = true;
                
                // Escuchar eventos de captura de huella
                _fingerprintReader.FingerprintCaptured += OnHuellaCapturada;
                _fingerprintReader.StartContinuousCapture();
            }
            else
            {
                EstadoLector = "Lector no disponible - Use su matricula";
                LectorHuellaConectado = false;
            }
        }
        catch (Exception ex)
        {
            EstadoLector = $"Error del lector: {ex.Message}";
            LectorHuellaConectado = false;
        }
    }

    private async void OnHuellaCapturada(object? sender, FingerprintCapturedEventArgs e)
    {
        try
        {
            Trace.WriteLine("[ChecadorViewModel] Huella capturada recibida.");
            
            // Usar el dispatcher de Avalonia para operaciones de UI desde otro thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                EstadoLector = "Procesando huella...";
                
                Trace.WriteLine($"[ChecadorViewModel] Dimensiones: W={e.Fingerprint.Width}, H={e.Fingerprint.Height}, DPI={e.Fingerprint.DPI}, DataLen={e.Fingerprint.ImageData.Length}");
                
                byte[]? fmdCapturado = _fingerprintReader.CreateFmd(e.Fingerprint.ImageData);
                if (fmdCapturado == null)
                {
                    EstadoLector = "Error al procesar huella. Intente de nuevo.";
                    Trace.WriteLine("[ChecadorViewModel] CreateFmd devolvio NULL.");
                    return;
                }
                Trace.WriteLine($"[ChecadorViewModel] CreateFmd OK. Tamano={fmdCapturado.Length}");

                var personas = await _personasService.ObtenerPersonasConHuellaAsync();
                Trace.WriteLine($"[ChecadorViewModel] Personas con huella en BD: {personas?.Count ?? 0}");
                if (personas == null || personas.Count == 0)
                {
                    EstadoLector = "No hay huellas registradas. Use su matricula.";
                    return;
                }

                Personas? mejorMatch = null;
                uint mejorScore = uint.MaxValue; // Lower is better (0 = perfect match)
                const uint UMBRAL_COINCIDENCIA = 100000; // Adjust threshold experimentally

                foreach (var persona in personas)
                {
                    if (persona.Huella == null || persona.Huella.Length == 0) continue;

                    uint score = _fingerprintReader.CompareFmds(fmdCapturado, persona.Huella);
                    Trace.WriteLine($"[ChecadorViewModel] Comparando con {persona.Matricula}: score={score}");
                    if (score < mejorScore)
                    {
                        mejorScore = score;
                        mejorMatch = persona;
                    }
                }

                Trace.WriteLine($"[ChecadorViewModel] Mejor match: {mejorMatch?.NombreCompleto ?? "Ninguno"} con score={mejorScore}. Umbral={UMBRAL_COINCIDENCIA}");

                if (mejorMatch != null && mejorScore < UMBRAL_COINCIDENCIA)
                {
                    EstadoLector = $"Huella reconocida: {mejorMatch.NombreCompleto}";
                    var (tipo, mensaje) = await _checadorService.MarcarAsistenciaAsync(mejorMatch.Matricula);
                    Mensaje = mensaje;
                    EsError = tipo == "error";
                    MostrarMensaje = true;
                    
                    await Task.Delay(3000);
                    EstadoLector = "Lector listo - Coloque su dedo";
                }
                else
                {
                    EstadoLector = "Huella no reconocida. Intente de nuevo.";
                    EsError = true;
                    MostrarMensaje = true;
                    Mensaje = "Huella no reconocida. Intente de nuevo o use su matricula.";
                    
                    await Task.Delay(2000);
                    EstadoLector = "Lector listo - Coloque su dedo";
                }
            });
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                EstadoLector = $"Error: {ex.Message}";
            });
        }
    }

    private void OnImagesChanged(object? sender, EventArgs e)
    {
        CargarImagenes();
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
        catch { /* En caso de que no haya imagenes configuradas, quedan en null */ }
    }

    private async System.Threading.Tasks.Task RegistrarAsync()
    {
        if (string.IsNullOrWhiteSpace(Matricula))
        {
            Mensaje = "Por favor, introduce una matricula.";
            EsError = true;
            MostrarMensaje = true;
            return;
        }

        try
        {
            var (tipo, mensaje) = await _checadorService.MarcarAsistenciaAsync(Matricula.Trim());
            Mensaje = mensaje;
            EsError = tipo == "error";
            MostrarMensaje = true;
            Matricula = string.Empty;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }

    public void Dispose()
    {
        _fingerprintReader?.Dispose();
    }

    ~ChecadorViewModel()
    {
        Dispose();
    }
}
