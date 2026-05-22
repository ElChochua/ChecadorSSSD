using System;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChecadorSSSD.Services;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.ViewModels;

public class ChecadorViewModel : ViewModelBase
{
    private readonly ChecadorService _checadorService;
    private readonly ImageService _imageService;

    private string _horaActual = DateTime.Now.ToString("HH:mm:ss");
    private string _fechaActual = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy").ToUpper();
    private string _matricula = string.Empty;
    private string _mensaje = string.Empty;
    private bool _esError;
    private bool _mostrarMensaje;

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

    public Bitmap? LogoPrograma
    {
        get => _logoPrograma;
        set => SetProperty(ref _logoPrograma, value);
    }

    public Bitmap? ImagenLugar
    {
        get => _imagenLugar;
        set => SetProperty(ref _imagenLugar, value);
    }

    public Bitmap? ImagenUniversidad
    {
        get => _imagenUniversidad;
        set => SetProperty(ref _imagenUniversidad, value);
    }

    public ICommand RegistrarCommand { get; }

    public ChecadorViewModel(ChecadorService checadorService)
    {
        _checadorService = checadorService;
        _imageService = new ImageService();
        RegistrarCommand = new RelayCommand(async () => await RegistrarAsync());
        
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
            ImagenLugar = _imageService.LoadImage(_imageService.ImagenLugarPath);
            ImagenUniversidad = _imageService.LoadImage(_imageService.ImagenUniversidadPath);
        }
        catch { /* En caso de que no haya imagenes configuradas, quedan en null */ }
    }

    private async System.Threading.Tasks.Task RegistrarAsync()
    {
        if (string.IsNullOrWhiteSpace(Matricula))
        {
            Mensaje = "Por favor, introduce una matrícula.";
            EsError = true;
            MostrarMensaje = true;
            return;
        }

        try
        {
            var (accion, mensaje) = await _checadorService.MarcarEntradaSalidaAsync(Matricula.Trim());
            Mensaje = mensaje;
            EsError = accion == TipoAccion.Salida;
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
}
