using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase? _selectedViewModel;
    private int _selectedIndex = 0;
    private int _previousIndex = -1;

    private readonly ChecadorViewModel _checadorViewModel;
    private readonly PersonasViewModel _personasViewModel;
    private readonly ReportesViewModel _reportesViewModel;
    private readonly AdministracionViewModel _administracionViewModel;
    private readonly ImageService _imageService;

    private Bitmap? _carruselImagenActual;
    private string _carruselTituloActual = "Bienvenido";
    private string _carruselRutaActual = string.Empty;
    private List<ImagenCarrusel> _carruselImagenes = new();
    private int _carruselIndice = 0;

    private Bitmap? _logoSidebar;

    public Bitmap? LogoSidebar
    {
        get => _logoSidebar;
        private set => SetProperty(ref _logoSidebar, value);
    }

    public void CargarLogo()
    {
        try
        {
            var logoPath = _imageService.LogoChecadorPath;
            if (!string.IsNullOrWhiteSpace(logoPath))
            {
                LogoSidebar = _imageService.LoadImage(logoPath);
            }
            else
            {
                LogoSidebar = null;
            }
        }
        catch
        {
            LogoSidebar = null;
        }
    }

    public bool HasLogoSidebar => LogoSidebar != null;

    public ViewModelBase? SelectedViewModel
    {
        get => _selectedViewModel;
        set => SetProperty(ref _selectedViewModel, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (SetProperty(ref _selectedIndex, value))
            {
                OnSelectedIndexChanged(value);
            }
        }
    }

    public Bitmap? CarruselImagenActual
    {
        get => _carruselImagenActual;
        private set => SetProperty(ref _carruselImagenActual, value);
    }

    public string CarruselRutaActual
    {
        get => _carruselRutaActual;
        private set => SetProperty(ref _carruselRutaActual, value);
    }

    public ICommand AbrirCarruselCommand { get; }

    public ICommand NavigateToChecadorCommand { get; }
    public ICommand NavigateToPersonasCommand { get; }
    public ICommand NavigateToReportesCommand { get; }
    public ICommand NavigateToAdministracionCommand { get; }
    public ICommand ExitCommand { get; }

    public MainWindowViewModel(
        ChecadorViewModel checadorViewModel,
        PersonasViewModel personasViewModel,
        ReportesViewModel reportesViewModel,
        AdministracionViewModel administracionViewModel)
    {
        _checadorViewModel = checadorViewModel;
        _personasViewModel = personasViewModel;
        _reportesViewModel = reportesViewModel;
        _administracionViewModel = administracionViewModel;
        _imageService = new ImageService();

        SelectedViewModel = checadorViewModel;
        _checadorViewModel.Activar();

        NavigateToChecadorCommand = new RelayCommand(() => SelectedIndex = 0);
        NavigateToPersonasCommand = new RelayCommand(() => SelectedIndex = 1);
        NavigateToReportesCommand = new RelayCommand(() => SelectedIndex = 2);
        NavigateToAdministracionCommand = new RelayCommand(() => SelectedIndex = 3);
        ExitCommand = new RelayCommand(() => Salir());
        AbrirCarruselCommand = new RelayCommand(AbrirCarrusel);

        AppMessenger.ImagesChanged += OnImagesChanged;

        CargarLogo();
        InicializarCarrusel();
    }

    private void OnImagesChanged(object? sender, EventArgs e)
    {
        try
        {
            CarruselImagenActual = null;
            CarruselRutaActual = string.Empty;
            _carruselImagenes = _imageService.ImagenesCarrusel;
            _carruselIndice = 0;
            ActualizarCarrusel();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"MWVM.OnImagesChanged EXCEPCION: {ex}\n{ex.StackTrace}");
        }
    }

    private void InicializarCarrusel()
    {
        _carruselImagenes = _imageService.ImagenesCarrusel;
        if (_carruselImagenes.Count == 0)
        {
            // Imagen por defecto si no hay carrusel configurado
            CarruselImagenActual = null;
            return;
        }
        ActualizarCarrusel();

        // Timer cada 5 segundos
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        timer.Tick += (_, _) => AvanzarCarrusel();
        timer.Start();
    }

    private void AvanzarCarrusel()
    {
        if (_carruselImagenes.Count == 0) return;
        _carruselIndice = (_carruselIndice + 1) % _carruselImagenes.Count;
        ActualizarCarrusel();
    }

    private void ActualizarCarrusel()
    {
        if (_carruselImagenes == null || _carruselImagenes.Count == 0)
        {
            CarruselRutaActual = string.Empty;
            CarruselImagenActual = null;
            return;
        }
        if (_carruselIndice >= 0 && _carruselIndice < _carruselImagenes.Count)
        {
            var actual = _carruselImagenes[_carruselIndice];
            CarruselRutaActual = actual.Ruta;
            CarruselImagenActual = _imageService.LoadImage(actual.Ruta);
        }
    }

    private void AbrirCarrusel()
    {
        if (_carruselImagenes.Count == 0 || string.IsNullOrEmpty(CarruselRutaActual)) return;
        try
        {
            var dialog = new Views.ImagePreviewDialog();
            dialog.SetImagePath(CarruselRutaActual);
            dialog.Show();
        }
        catch
        {
            // Ignorar errores al abrir dialogo
        }
    }

    private void OnSelectedIndexChanged(int index)
    {
        if (index == _previousIndex) return;

        // Desactivar el lector cuando se SALE de checador y esperar a que complete
        if (_previousIndex == 0)
        {
            _checadorViewModel.Desactivar();
        }

        SelectedViewModel = index switch
        {
            0 => _checadorViewModel,
            1 => _personasViewModel,
            2 => _reportesViewModel,
            3 => _administracionViewModel,
            _ => _checadorViewModel
        };

        // Activar el lector cuando se ENTRA a checador
        if (index == 0)
        {
            _checadorViewModel.Activar();
        }

        // Recargar personas al entrar a Personas o Administracion
        if (index == 1)
        {
            _personasViewModel.RecargarPersonas();
        }
        else if (index == 3)
        {
            _administracionViewModel.RecargarPersonasAdmin();
        }

        _previousIndex = index;
    }

    private void Salir()
    {
        if (System.Diagnostics.Debugger.IsAttached)
        {
            // En modo debug, cerrar la ventana principal
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                desktop.MainWindow.Close();
            }
        }
        else
        {
            // En produccion cierra la aplicacion
            System.Environment.Exit(0);
        }
    }
}
