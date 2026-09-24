using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;
using ChecadorSSSD.Views;

namespace ChecadorSSSD.ViewModels;

/// <summary>
/// ViewModel para la vista de Administracion (CRUD, reportes, imagenes, login).
/// </summary>
public class AdministracionViewModel : ViewModelBase
{
    private readonly PersonasService _personasService;
    private readonly ReportesService _reportesService;
    private readonly AuthService _authService;
    private readonly ImageService _imageService;
    private readonly FingerprintService _fingerprintService;

    private bool _autenticado = false;
    private string _usuarioLogin = string.Empty;
    private string _passLogin = string.Empty;
    private string _mensajeLogin = string.Empty;

    // CRUD personas
    private List<Personas> _personas = new();
    private List<Personas> _todasLasPersonas = new();
    private Personas? _personaSeleccionada;
    private Personas _personaEnEdicion = new();
    private string _busquedaAdmin = string.Empty;
    private bool _modoEdicionAdmin = false;
    private bool _panelEdicionVisible = false;

    // Reportes individuales
    private string _matriculaReporte = string.Empty;
    private Personas? _personaReporteSeleccionada;
    private DateTimeOffset? _fechaInicioReporte = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, DateTimeOffset.Now.Offset);
    private DateTimeOffset? _fechaFinReporte = DateTimeOffset.Now;
    private string _tipoReporteSeleccionado = "Usuarios Mensual";
    private string _busquedaReporte = string.Empty;
    private List<ReporteItem> _reporteHoras = new();
    private List<ReporteDetalleItem> _reporteUsuarios = new();
    private int _paginaActual = 1;
    private readonly int _registrosPorPagina = 50;

    // Seleccion multiple para reportes
    private string _busquedaPersonasReporte = string.Empty;
    private List<Personas> _personasFiltradasReporte = new();
    private List<Personas> _personasSeleccionadasReporte = new();

    private List<ReporteAdminItem> _resultadoReporte = new();

    // Imagenes
    private string _rutaLogo = string.Empty;
    private string _rutaLugar = string.Empty;
    private string _rutaUniversidad = string.Empty;
    private string _nombreImagenCarrusel = string.Empty;
    private string _rutaImagenCarrusel = string.Empty;
    private ObservableCollection<ImagenCarrusel> _imagenesCarrusel = new();

    // Imagenes - vistas previas de logos
    private Bitmap? _vistaPreviaLogo = null;
    private Bitmap? _vistaPreviaLugar = null;
    private Bitmap? _vistaPreviaLogoUniversidad = null;
    private Bitmap? _vistaPreviaFotoPersona = null;

    private string _mensaje = string.Empty;
    private bool _esError;
    private bool _mostrarMensaje;
    private string _estadoHuellaText = "Sin capturar";


    public bool Autenticado
    {
        get => _autenticado;
        set => SetProperty(ref _autenticado, value);
    }

    public string UsuarioLogin
    {
        get => _usuarioLogin;
        set => SetProperty(ref _usuarioLogin, value);
    }

    public string PassLogin
    {
        get => _passLogin;
        set => SetProperty(ref _passLogin, value);
    }

    public string MensajeLogin
    {
        get => _mensajeLogin;
        set
        {
            if (SetProperty(ref _mensajeLogin, value))
                OnPropertyChanged(nameof(MostrarMensajeLogin));
        }
    }

    public bool MostrarMensajeLogin => !string.IsNullOrEmpty(MensajeLogin);

    public string TituloFormulario => "Informacion de Persona";

    public List<string> TiposUsuario => new() { "Brigadista", "Asesor", "Empleado" };

    // Personas
    public List<Personas> Personas
    {
        get => _personas;
        set => SetProperty(ref _personas, value);
    }

    public Personas? PersonaSeleccionada
    {
        get => _personaSeleccionada;
        set
        {
            if (!SetProperty(ref _personaSeleccionada, value)) return;
            if (value == null)
            {
                ResetearFormularioPersona();
                return;
            }
            CargarFormularioPersona(value);
        }
    }

    private void CargarFormularioPersona(Personas value)
    {
        PersonaEnEdicion = new Personas
        {
            IdPersona = value.IdPersona,
            Nombre = value.Nombre,
            ApellidoPaterno = value.ApellidoPaterno,
            ApellidoMaterno = value.ApellidoMaterno,
            Matricula = value.Matricula,
            TipoPersona = value.TipoPersona,
            Imagen = value.Imagen,
            Huella = value.Huella
        };
        // Cargar vista previa de la foto
        if (!string.IsNullOrEmpty(value.Imagen) && System.IO.File.Exists(value.Imagen))
        {
            try
            {
                VistaPreviaFotoPersona = new Avalonia.Media.Imaging.Bitmap(value.Imagen);
            }
            catch
            {
                VistaPreviaFotoPersona = null;
            }
        }
        else
        {
            VistaPreviaFotoPersona = null;
        }
        var tieneHuella = value.Huella != null && value.Huella.Length > 0;
        EstadoHuellaText = tieneHuella ? "Huella Registrada" : "Huella Sin Capturar";
        ModoEdicionAdmin = true;
        PanelEdicionVisible = true;
    }

    private void ResetearFormularioPersona()
    {
        PersonaEnEdicion = new Personas();
        VistaPreviaFotoPersona = null;
        EstadoHuellaText = "Sin Capturar";
        ModoEdicionAdmin = false;
        PanelEdicionVisible = false;
    }

    public Personas PersonaEnEdicion
    {
        get => _personaEnEdicion;
        set => SetProperty(ref _personaEnEdicion, value);
    }

    public string BusquedaAdmin
    {
        get => _busquedaAdmin;
        set
        {
            if (SetProperty(ref _busquedaAdmin, value))
                FiltrarPersonasAdmin();
        }
    }

    public bool ModoEdicionAdmin
    {
        get => _modoEdicionAdmin;
        set => SetProperty(ref _modoEdicionAdmin, value);
    }

    public bool PanelEdicionVisible
    {
        get => _panelEdicionVisible;
        set => SetProperty(ref _panelEdicionVisible, value);
    }

    // Reportes
    public string MatriculaReporte
    {
        get => _matriculaReporte;
        set => SetProperty(ref _matriculaReporte, value);
    }

    public Personas? PersonaReporteSeleccionada
    {
        get => _personaReporteSeleccionada;
        set
        {
            if (SetProperty(ref _personaReporteSeleccionada, value))
            {
                MatriculaReporte = value?.Matricula ?? string.Empty;
            }
        }
    }

    // Propiedades para seleccion multiple de personas en reportes
    public string BusquedaPersonasReporte
    {
        get => _busquedaPersonasReporte;
        set
        {
            if (SetProperty(ref _busquedaPersonasReporte, value))
            {
                FiltrarPersonasReporte();
            }
        }
    }

    public List<Personas> PersonasFiltradasReporte
    {
        get => _personasFiltradasReporte;
        set => SetProperty(ref _personasFiltradasReporte, value);
    }

    public List<Personas> PersonasSeleccionadasReporte
    {
        get => _personasSeleccionadasReporte;
        set => SetProperty(ref _personasSeleccionadasReporte, value);
    }

    public DateTimeOffset? FechaInicioReporte
    {
        get => _fechaInicioReporte;
        set => SetProperty(ref _fechaInicioReporte, value);
    }

    public DateTimeOffset? FechaFinReporte
    {
        get => _fechaFinReporte;
        set => SetProperty(ref _fechaFinReporte, value);
    }

    public List<string> TiposReporte => new() { "Horas Mensual", "Usuarios Mensual" };

    public string TipoReporteSeleccionado
    {
        get => _tipoReporteSeleccionado;
        set
        {
            if (SetProperty(ref _tipoReporteSeleccionado, value))
            {
                OnPropertyChanged(nameof(EsReporteHoras));
                OnPropertyChanged(nameof(MostrarResultadosHoras));
                OnPropertyChanged(nameof(MostrarResultadosUsuarios));
                OnPropertyChanged(nameof(ResultadosPaginadosHoras));
                OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
                OnPropertyChanged(nameof(TotalPaginas));
                OnPropertyChanged(nameof(RangoPaginaText));
            }
        }
    }

    public bool EsReporteHoras => TipoReporteSeleccionado == "Horas Mensual";

    public string BusquedaReporte
    {
        get => _busquedaReporte;
        set
        {
            if (SetProperty(ref _busquedaReporte, value))
            {
                _paginaActual = 1;
                OnPropertyChanged(nameof(ResultadosPaginadosHoras));
                OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
                OnPropertyChanged(nameof(TotalPaginas));
                OnPropertyChanged(nameof(RangoPaginaText));
            }
        }
    }

    public int PaginaActual
    {
        get => _paginaActual;
        set
        {
            if (SetProperty(ref _paginaActual, Math.Clamp(value, 1, Math.Max(1, TotalPaginas))))
            {
                OnPropertyChanged(nameof(ResultadosPaginadosHoras));
                OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
                OnPropertyChanged(nameof(RangoPaginaText));
                OnPropertyChanged(nameof(PaginaAnteriorPuedeEjecutarse));
                OnPropertyChanged(nameof(PaginaSiguientePuedeEjecutarse));
                OnPropertyChanged(nameof(TotalPaginas));
                _paginaAnteriorCommand?.RaiseCanExecuteChanged();
                _paginaSiguienteCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public int RegistrosPorPagina => _registrosPorPagina;

    public int TotalPaginas => EsReporteHoras
        ? Math.Max(1, (int)Math.Ceiling((double)Filtrar(_reporteHoras).Count / _registrosPorPagina))
        : Math.Max(1, (int)Math.Ceiling((double)Filtrar(_reporteUsuarios).Count / _registrosPorPagina));

    public string RangoPaginaText => $"Pagina {PaginaActual} de {TotalPaginas}";

    public bool PaginaAnteriorPuedeEjecutarse => PaginaActual > 1;
    public bool PaginaSiguientePuedeEjecutarse => PaginaActual < TotalPaginas;

    public bool MostrarResultadosHoras => EsReporteHoras && ResultadosPaginadosHoras.Any();
    public bool MostrarResultadosUsuarios => !EsReporteHoras && ResultadosPaginadosUsuarios.Any();

    public List<ReporteItem> ResultadosPaginadosHoras => AplicarPaginacion(Filtrar(_reporteHoras));
    public List<ReporteDetalleItem> ResultadosPaginadosUsuarios => AplicarPaginacion(Filtrar(_reporteUsuarios));

    public List<ReporteAdminItem> ResultadoReporte
    {
        get => _resultadoReporte;
        set => SetProperty(ref _resultadoReporte, value);
    }

    // Imagenes
    public string RutaLogo
    {
        get => _rutaLogo;
        set
        {
            if (SetProperty(ref _rutaLogo, value))
            {
                VistaPreviaLogo = _imageService.LoadImage(value);
                OnPropertyChanged(nameof(HasVistaPreviaLogo));
            }
        }
    }

    public string RutaLugar
    {
        get => _rutaLugar;
        set
        {
            if (SetProperty(ref _rutaLugar, value))
            {
                VistaPreviaLugar = _imageService.LoadImage(value);
                OnPropertyChanged(nameof(HasVistaPreviaLugar));
            }
        }
    }

    public string RutaUniversidad
    {
        get => _rutaUniversidad;
        set
        {
            if (SetProperty(ref _rutaUniversidad, value))
            {
                VistaPreviaLogoUniversidad = _imageService.LoadImage(value);
                OnPropertyChanged(nameof(HasVistaPreviaLogoUniversidad));
            }
        }
    }

    // Vistas previas de logos
    public Bitmap? VistaPreviaLogo
    {
        get => _vistaPreviaLogo;
        set => SetProperty(ref _vistaPreviaLogo, value);
    }

    public bool HasVistaPreviaLogo => VistaPreviaLogo != null;

    public Bitmap? VistaPreviaLugar
    {
        get => _vistaPreviaLugar;
        set => SetProperty(ref _vistaPreviaLugar, value);
    }

    public bool HasVistaPreviaLugar => VistaPreviaLugar != null;

    public Bitmap? VistaPreviaLogoUniversidad
    {
        get => _vistaPreviaLogoUniversidad;
        set => SetProperty(ref _vistaPreviaLogoUniversidad, value);
    }

    public bool HasVistaPreviaLogoUniversidad => VistaPreviaLogoUniversidad != null;

    public Bitmap? VistaPreviaFotoPersona
    {
        get => _vistaPreviaFotoPersona;
        set
        {
            if (SetProperty(ref _vistaPreviaFotoPersona, value))
            {
                OnPropertyChanged(nameof(HasVistaPreviaFotoPersona));
            }
        }
    }

    public bool HasVistaPreviaFotoPersona => VistaPreviaFotoPersona != null;

    public void GuardarRutaLogo(string path)
    {
        try
        {
            RutaLogo = path;
            _imageService.SetLogoChecador(path);
            VistaPreviaLogo = _imageService.LoadImage(path);
            OnPropertyChanged(nameof(VistaPreviaLogo));
            OnPropertyChanged(nameof(HasVistaPreviaLogo));
            AppMessenger.NotifyImagesChanged();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error al guardar el logo: {ex}");
            MostrarError($"No se pudo guardar el logo: {ex.Message}");
        }
    }

    public void GuardarRutaLugar(string path)
    {
        try
        {
            RutaLugar = path;
            _imageService.SetImagenLugar(path);
            VistaPreviaLugar = _imageService.LoadImage(path);
            OnPropertyChanged(nameof(VistaPreviaLugar));
            OnPropertyChanged(nameof(HasVistaPreviaLugar));
            AppMessenger.NotifyImagesChanged();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error al guardar la imagen del lugar: {ex}");
            MostrarError($"No se pudo guardar la imagen del lugar: {ex.Message}");
        }
    }

    public void GuardarRutaUniversidad(string path)
    {
        try
        {
            RutaUniversidad = path;
            _imageService.SetImagenUniversidad(path);
            VistaPreviaLogoUniversidad = _imageService.LoadImage(path);
            OnPropertyChanged(nameof(VistaPreviaLogoUniversidad));
            OnPropertyChanged(nameof(HasVistaPreviaLogoUniversidad));
            AppMessenger.NotifyImagesChanged();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error al guardar la imagen de la universidad: {ex}");
            MostrarError($"No se pudo guardar la imagen de la universidad: {ex.Message}");
        }
    }

    public string NombreImagenCarrusel
    {
        get => _nombreImagenCarrusel;
        set => SetProperty(ref _nombreImagenCarrusel, value);
    }

    public string RutaImagenCarrusel
    {
        get => _rutaImagenCarrusel;
        set => SetProperty(ref _rutaImagenCarrusel, value);
    }

    public ObservableCollection<ImagenCarrusel> ImagenesCarrusel
    {
        get => _imagenesCarrusel;
        set => SetProperty(ref _imagenesCarrusel, value);
    }

    // Administradores
    private List<Administrador> _administradores = new();
    private Administrador? _adminSeleccionado;
    private Administrador _adminEnEdicion = new();
    private bool _modoEdicionAdminCrud = false;

    public List<Administrador> Administradores
    {
        get => _administradores;
        set => SetProperty(ref _administradores, value);
    }

    public Administrador? AdminSeleccionado
    {
        get => _adminSeleccionado;
        set
        {
            if (SetProperty(ref _adminSeleccionado, value) && value != null)
            {
                AdminEnEdicion = new Administrador
                {
                    IdAdmin = value.IdAdmin,
                    IdPersona = value.IdPersona,
                    Usuario = value.Usuario,
                    Contrasenia = string.Empty
                };
                PersonaSeleccionadaAdmin = PersonasDisponiblesAdmin.FirstOrDefault(p => p.IdPersona == value.IdPersona);
                ModoEdicionAdminCrud = true;
            }
        }
    }

    public Administrador AdminEnEdicion
    {
        get => _adminEnEdicion;
        set => SetProperty(ref _adminEnEdicion, value);
    }

    public bool ModoEdicionAdminCrud
    {
        get => _modoEdicionAdminCrud;
        set => SetProperty(ref _modoEdicionAdminCrud, value);
    }

    public string TituloFormularioAdmin => ModoEdicionAdminCrud ? "Editar Administrador" : "Agregar Administrador";

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

    // Comandos
    // Comandos Admin (aliases para XAML)
    public ICommand EditarCommand { get; }
    public ICommand EliminarCommand { get; }
    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }
    public ICommand LoginCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand NuevoPersonaCommand { get; }
    public ICommand GuardarPersonaCommand { get; }
    public ICommand EliminarPersonaCommand { get; }
    public ICommand BuscarPersonaCommand { get; }
    public ICommand GenerarReporteIndividuaCommand { get; }
    public ICommand ExportarReporteCommand { get; }
    public ICommand AgregarPersonaReporteCommand { get; }
    public ICommand QuitarPersonaReporteCommand { get; }
    public ICommand GuardarImagenLogoCommand { get; }
    public ICommand GuardarImagenLugarCommand { get; }
    public ICommand GuardarImagenUniversidadCommand { get; }
    public ICommand AgregarImagenCarruselCommand { get; }
    public ICommand EliminarImagenCarruselCommand { get; }
    public ICommand PaginaAnteriorCommand { get; }
    public ICommand PaginaSiguienteCommand { get; }
    public ICommand NuevoAdminCommand { get; }
    public ICommand GuardarAdminCommand { get; }
    public ICommand EliminarAdminCommand { get; }
    public ICommand EditarAdminCommand { get; }
    public ICommand CancelarAdminCommand { get; }

    private RelayCommand _paginaAnteriorCommand = null!;
    private RelayCommand _paginaSiguienteCommand = null!;

    private readonly AdministradorService _adminService;

    public string EstadoHuellaText
    {
        get => _estadoHuellaText;
        set => SetProperty(ref _estadoHuellaText, value);
    }

    public ICommand CapturarHuellaCommand { get; }
    public ICommand EliminarHuellaCommand { get; }

    public AdministracionViewModel(
        PersonasService personasService,
        ReportesService reportesService,
        AdministradorService adminService,
        AuthService authService,
        FingerprintService fingerprintService)
    {
        _personasService = personasService;
        _reportesService = reportesService;
        _adminService = adminService;
        _authService = authService;
        _fingerprintService = fingerprintService;
        _imageService = new ImageService();

        PersonasFiltradasReporte = new List<Personas>();
        PersonasSeleccionadasReporte = new List<Personas>();

        LoginCommand = new RelayCommand(async () => await LoginAsync());
        LogoutCommand = new RelayCommand(() => { Autenticado = false; MensajeLogin = string.Empty; UsuarioLogin = string.Empty; PassLogin = string.Empty; });
        
        // Aliases para XAML
        EditarCommand = new RelayCommand<Personas>(p => { if (p != null) PersonaSeleccionada = p; });
        EliminarCommand = new RelayCommand<Personas>(async p =>
        {
            if (p == null) return;
            PersonaSeleccionada = p;
            await EliminarPersonaAdminAsync(p);
        });
        GuardarCommand = new RelayCommand(async () => await GuardarPersonaAdminAsync());
        
        CancelarCommand = new RelayCommand(() =>
        {
            PersonaSeleccionada = null; // dispara ResetearFormularioPersona()
            ResetearFormularioPersona();
        });
        NuevoPersonaCommand = new RelayCommand(async () => await MostrarDialogoAgregarPersonaAsync());
        
        GuardarPersonaCommand = new RelayCommand(async () => await GuardarPersonaAdminAsync());
        EliminarPersonaCommand = new RelayCommand(async () => await EliminarPersonaAdminAsync(PersonaSeleccionada));
        BuscarPersonaCommand = new RelayCommand(async () => await BuscarPersonaAdminAsync());
        GenerarReporteIndividuaCommand = new RelayCommand(async () => await GenerarReporteIndividualAsync());
        ExportarReporteCommand = new RelayCommand(async () => await ExportarReporteAsync());
        AgregarPersonaReporteCommand = new RelayCommand<Personas>(p => { if (p != null) AgregarPersonaReporte(p); });
        QuitarPersonaReporteCommand = new RelayCommand<Personas>(p => { if (p != null) QuitarPersonaReporte(p); });
        GuardarImagenLogoCommand = new RelayCommand(() => GuardarImagenConfig(nameof(RutaLogo), RutaLogo));
        GuardarImagenLugarCommand = new RelayCommand(() => GuardarImagenConfig(nameof(RutaLugar), RutaLugar));
        GuardarImagenUniversidadCommand = new RelayCommand(() => GuardarImagenConfig(nameof(RutaUniversidad), RutaUniversidad));
        AgregarImagenCarruselCommand = new RelayCommand(() => AgregarImagenCarrusel());
        EliminarImagenCarruselCommand = new RelayCommand<ImagenCarrusel>(async item => await EliminarImagenCarruselAsync(item));
        _paginaAnteriorCommand = new RelayCommand(() => PaginaActual--, () => PaginaActual > 1);
        _paginaSiguienteCommand = new RelayCommand(() => PaginaActual++, () => PaginaActual < TotalPaginas);
        PaginaAnteriorCommand = _paginaAnteriorCommand;
        PaginaSiguienteCommand = _paginaSiguienteCommand;

        NuevoAdminCommand = new RelayCommand(() => { AdminEnEdicion = new Administrador(); ModoEdicionAdminCrud = false; });
        GuardarAdminCommand = new RelayCommand(async () => await GuardarAdminAsync());
        EliminarAdminCommand = new RelayCommand<Administrador>(async a =>
        {
            if (a != null) { AdminSeleccionado = a; await EliminarAdminAsync(); }
        });
        EditarAdminCommand = new RelayCommand<Administrador>(a =>
        {
            if (a != null) AdminSeleccionado = a;
        });
        CancelarAdminCommand = new RelayCommand(() => { AdminEnEdicion = new Administrador(); ModoEdicionAdminCrud = false; });
        CapturarHuellaCommand = new RelayCommand(async () => await CapturarHuellaAsync());
        EliminarHuellaCommand = new RelayCommand(async () => await EliminarHuellaAsync());

        // Obtenemos las administradores guardados
        RutaLogo = _imageService.LogoChecadorPath;
        RutaLugar = _imageService.ImagenLugarPath;
        RutaUniversidad = _imageService.ImagenUniversidadPath;
        SincronizarImagenesCarruselDesde(_imageService.ImagenesCarrusel);
    }

    private async Task LoginAsync()
    {
        try
        {
            if (await _authService.Login(UsuarioLogin, PassLogin))
            {
                Autenticado = true;
                MensajeLogin = string.Empty;
                await CargarPersonasAdminAsync();
                await CargarAdministradoresAsync();
                PersonasDisponiblesAdmin = new List<Personas>(_todasLasPersonas);

                PersonasSeleccionadasReporte = new List<Personas>();
                PersonasFiltradasReporte = new List<Personas>(_todasLasPersonas);
            }
            else
            {
                MensajeLogin = "Usuario o contrasena incorrectos.";
                Autenticado = false;
            }
        }
        catch (Exception ex)
        {
            MensajeLogin = $"No se pudo conectar con MySQL: {ex.Message}";
            Autenticado = false;
        }
    }

    /// <summary>
    /// Recarga la lista de personas desde la base de datos.
    /// </summary>
    public void RecargarPersonasAdmin()
    {
        _ = CargarPersonasAdminAsync();
    }

    private async Task CargarPersonasAdminAsync()
    {
        try
        {
            _todasLasPersonas = await _personasService.ObtenerTodasAsync();
            Personas = new List<Personas>(_todasLasPersonas);
        }
        catch (Exception ex)
        {
            MostrarError($"Error al cargar personas: {ex.Message}");
        }
    }

    private void FiltrarPersonasAdmin()
    {
        if (_todasLasPersonas == null)
        {
            Personas = new List<Personas>();
            return;
        }
        if (string.IsNullOrWhiteSpace(BusquedaAdmin))
        {
            Personas = new List<Personas>(_todasLasPersonas);
            return;
        }
        var f = BusquedaAdmin.ToLower();
        Personas = _todasLasPersonas.Where(p =>
            (p.Nombre ?? string.Empty).ToLower().Contains(f) ||
            (p.ApellidoPaterno ?? string.Empty).ToLower().Contains(f) ||
            (p.ApellidoMaterno ?? string.Empty).ToLower().Contains(f) ||
            (p.Matricula ?? string.Empty).ToLower().Contains(f))
            .ToList();
    }

    private async Task GuardarPersonaAdminAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PersonaEnEdicion.Nombre) ||
                string.IsNullOrWhiteSpace(PersonaEnEdicion.Matricula))
            {
                MostrarError("Complete los campos obligatorios.");
                return;
            }
            // Si no se proporcion� foto, usar la foto por defecto
            if (string.IsNullOrWhiteSpace(PersonaEnEdicion.Imagen))
            {
                PersonaEnEdicion.Imagen = @"C:\FotografiasHuella\Dios.jpg";
            }
            if (ModoEdicionAdmin)
            {
                await _personasService.ActualizarAsync(PersonaEnEdicion);
                BusquedaAdmin = "";
                await CargarPersonasAdminAsync();
                AppMessenger.NotifyPersonasChanged();
                MostrarExito("Persona actualizada.");
            }
            else
            {
                await _personasService.CrearAsync(PersonaEnEdicion);
                _todasLasPersonas.Add(PersonaEnEdicion);
                Personas = new List<Personas>(_todasLasPersonas);
                FiltrarPersonasAdmin();
                AppMessenger.NotifyPersonasChanged();
                MostrarExito("Persona creada.");
            }
            PersonaEnEdicion = new Personas();
            ModoEdicionAdmin = false;
            PanelEdicionVisible = false;
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private async Task EliminarPersonaAdminAsync(Personas? target)
    {
        if (target == null) return;
        try
        {
            bool confirmado = await ConfirmarAsync(
                "Confirmar eliminacion",
                $"Esta seguro de que desea eliminar a {target.NombreCompleto}? Esta accion no se puede deshacer.");
            if (!confirmado) return;

            var idPersona = target.IdPersona;
            await _personasService.EliminarAsync(idPersona);
            AppMessenger.NotifyPersonasChanged();
            MostrarExito("Persona eliminada.");

            // Limpiar seleccion, formulario y buscador antes de recargar la lista
            // para evitar que los bindings apunten a la persona eliminada.
            PersonaSeleccionada = null; // el setter invoca ResetearFormularioPersona()
            ResetearFormularioPersona();
            BusquedaAdmin = "";

            await CargarPersonasAdminAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private async Task<bool> ConfirmarAsync(string titulo, string mensaje)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new ConfirmacionDialog();
            return await dialog.ShowConfirm(titulo, mensaje, desktop.MainWindow);
        }
        return true;
    }

    private async Task BuscarPersonaAdminAsync()
    {
        await CargarPersonasAdminAsync();
        FiltrarPersonasAdmin();
    }

    private void FiltrarPersonasReporte()
    {
        var seleccionadosIds = PersonasSeleccionadasReporte.Select(p => p.IdPersona).ToList();
        var baseList = string.IsNullOrWhiteSpace(BusquedaPersonasReporte)
            ? _todasLasPersonas
            : _todasLasPersonas.Where(p =>
                p.Nombre.ToLower().Contains(BusquedaPersonasReporte!.ToLower()) ||
                p.Matricula.ToLower().Contains(BusquedaPersonasReporte!.ToLower()));

        PersonasFiltradasReporte = baseList.Where(p => !seleccionadosIds.Contains(p.IdPersona)).ToList();
    }

    public void AgregarPersonaReporte(Personas persona)
    {
        if (!PersonasSeleccionadasReporte.Any(p => p.IdPersona == persona.IdPersona))
        {
            PersonasSeleccionadasReporte = new List<Personas>(PersonasSeleccionadasReporte) { persona };
            OnPropertyChanged(nameof(PersonasSeleccionadasReporte));
            FiltrarPersonasReporte();
        }
    }

    public void QuitarPersonaReporte(Personas persona)
    {
        PersonasSeleccionadasReporte = PersonasSeleccionadasReporte.Where(p => p.IdPersona != persona.IdPersona).ToList();
        OnPropertyChanged(nameof(PersonasSeleccionadasReporte));
        FiltrarPersonasReporte();
    }

    private async Task GenerarReporteIndividualAsync()
    {
        try
        {
            if (PersonasSeleccionadasReporte.Count == 0)
            {
                MostrarError("Seleccione al menos una persona para el reporte.");
                return;
            }

            if (!FechaInicioReporte.HasValue || !FechaFinReporte.HasValue)
            {
                MostrarError("Seleccione las fechas de inicio y fin.");
                return;
            }

            var fechaInicio = FechaInicioReporte.Value.DateTime;
            var fechaFin = FechaFinReporte.Value.DateTime;
            var idsPersonas = PersonasSeleccionadasReporte.Select(p => p.IdPersona).ToList();

            if (EsReporteHoras)
            {
                var (reporte, ok) = await _reportesService.GenerarReporteAsync(fechaInicio, fechaFin, null, idsPersonas);
                _reporteHoras = reporte;
                _reporteUsuarios = new List<ReporteDetalleItem>();

                if (!ok || _reporteHoras.Count == 0)
                {
                    MostrarExito("No se encontraron registros.");
                    return;
                }
            }
            else
            {
                var (reporte, ok) = await _reportesService.GenerarReporteUsuariosAsync(fechaInicio, fechaFin, null, idsPersonas);
                _reporteUsuarios = reporte;
                _reporteHoras = new List<ReporteItem>();

                if (!ok || _reporteUsuarios.Count == 0)
                {
                    MostrarExito("No se encontraron registros.");
                    return;
                }
            }

            PaginaActual = 1;
            OnPropertyChanged(nameof(ResultadosPaginadosHoras));
            OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(RangoPaginaText));
            OnPropertyChanged(nameof(MostrarResultadosHoras));
            OnPropertyChanged(nameof(MostrarResultadosUsuarios));
            MostrarExito("Reporte generado correctamente.");
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error al generar reporte individual: {ex}");
            MostrarError($"No se pudo generar el reporte: {ex.Message}");
        }
    }

    /// <summary>
    /// Exporta el reporte individual actual (Horas o Usuarios) a un CSV elegido
    /// por el usuario via el dialogo SaveFilePicker de Avalonia. Incluye TODOS
    /// los registros filtrados (no solo la pagina visible) y formatea las horas
    /// en formato "Xh Ym" / "Ym".
    /// </summary>
    private async Task ExportarReporteAsync()
    {
        // Validar que existan datos para exportar (todo el conjunto filtrado).
        var datosHoras = EsReporteHoras ? Filtrar(_reporteHoras) : new List<ReporteItem>();
        var datosUsuarios = !EsReporteHoras ? Filtrar(_reporteUsuarios) : new List<ReporteDetalleItem>();

        if ((EsReporteHoras && datosHoras.Count == 0)
            || (!EsReporteHoras && datosUsuarios.Count == 0))
        {
            MostrarError("No hay resultados para exportar. Genere un reporte primero.");
            return;
        }

        if (!FechaInicioReporte.HasValue || !FechaFinReporte.HasValue)
        {
            MostrarError("Seleccione las fechas de inicio y fin.");
            return;
        }

        var topLevel = ObtenerTopLevel();
        if (topLevel == null || topLevel.StorageProvider == null)
        {
            MostrarError("No se pudo abrir el dialogo de guardado.");
            return;
        }

        try
        {
            // Nombre del archivo: <tipo>_yyyy-MM-dd_yyyy-MM-dd.csv
            var fechaInicioStr = FechaInicioReporte.Value.ToString("yyyy-MM-dd");
            var fechaFinStr = FechaFinReporte.Value.ToString("yyyy-MM-dd");
            var prefijo = EsReporteHoras ? "reporte_horas" : "reporte_mensual";
            var nombreSugerido = $"{prefijo}_{fechaInicioStr}_{fechaFinStr}.csv";

            var opciones = new FilePickerSaveOptions
            {
                Title = "Guardar reporte CSV",
                SuggestedFileName = Path.GetFileNameWithoutExtension(nombreSugerido),
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Archivo CSV") { Patterns = new[] { "*.csv" } },
                    new FilePickerFileType("Todos los archivos") { Patterns = new[] { "*.*" } }
                }
            };

            var archivo = await topLevel.StorageProvider.SaveFilePickerAsync(opciones);
            if (archivo == null)
            {
                // El usuario cancelo el dialogo: no mostrar nada.
                return;
            }

            var sb = new StringBuilder();
            if (EsReporteHoras)
            {
                sb.AppendLine("Nombre Completo,Matricula,Rol,Total Asistencias,Total Entradas,Total Salidas,Horas Acumuladas");
                foreach (var item in datosHoras)
                {
                    sb.AppendLine($"\"{item.NombreCompleto}\",\"{item.Matricula}\",\"{item.Rol}\",{item.TotalAsistencias},{item.TotalEntradas},{item.TotalSalidas},{item.HorasAcumuladasTexto}");
                }
            }
            else
            {
                sb.AppendLine("Matricula,Nombre,Apellido Paterno,Apellido Materno,Fecha,Hora Entrada,Hora Salida,Horas Diarias,Clasificacion,Horas Trabajadas");
                foreach (var item in datosUsuarios)
                {
                    sb.AppendLine($"\"{item.Matricula}\",\"{item.Nombre}\",\"{item.ApellidoPaterno}\",\"{item.ApellidoMaterno}\",{item.Fecha:yyyy-MM-dd},\"{item.HoraEntrada}\",\"{item.HoraSalida}\",{item.HorasDiarias},\"{item.ClasificacionHoras}\",{item.HorasTrabajadasTexto}");
                }
            }

            // Escribir en el archivo seleccionado por el usuario
            await using var stream = await archivo.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(sb.ToString());
            await writer.FlushAsync();

            MostrarExito($"Reporte exportado exitosamente a: {archivo.TryGetLocalPath() ?? archivo.Path.ToString()}");
        }
        catch (Exception ex)
        {
            MostrarError($"Error al exportar a CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene el TopLevel de la ventana principal (necesario para StorageProvider).
    /// </summary>
    private static TopLevel? ObtenerTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow != null)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow) ?? desktop.MainWindow;
        }
        return null;
    }

    private void GuardarImagenConfig(string propiedad, string ruta)
    {
        try
        {
            if (ruta.StartsWith("Logo")) _imageService.SetLogoChecador(ruta);
            else if (ruta.StartsWith("Lugar")) _imageService.SetImagenLugar(ruta);
            else if (ruta.StartsWith("Universidad")) _imageService.SetImagenUniversidad(ruta);
            MostrarExito("Imagen guardada.");
        }
        catch (Exception ex)
        {
            MostrarError($"Error al guardar imagen: {ex.Message}");
        }
    }

    public void AgregarImagenCarrusel()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(RutaImagenCarrusel) || string.IsNullOrWhiteSpace(NombreImagenCarrusel))
            {
                MostrarError("Proporcione ruta y nombre de la imagen.");
                return;
            }
            _imageService.AgregarImagenCarrusel(RutaImagenCarrusel, NombreImagenCarrusel);
            SincronizarImagenesCarruselDesde(_imageService.ImagenesCarrusel);
            RutaImagenCarrusel = string.Empty;
            NombreImagenCarrusel = string.Empty;
            // Postergar al proximo ciclo del dispatcher para que el ItemsControl del
            // panel admin termine de procesar los CollectionChanged antes de que el
            // sidebar actualice su imagen (evita deadlock de render).
            Dispatcher.UIThread.Post(() => AppMessenger.NotifyImagesChanged(), DispatcherPriority.Background);
            MostrarExito("Imagen de carrusel agregada.");
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error al agregar una imagen al carrusel: {ex}");
            MostrarError($"No se pudo agregar la imagen al carrusel: {ex.Message}");
        }
    }

    /// <summary>
    /// Reemplaza el contenido de la ObservableCollection del carrusel manteniendo
    /// la misma instancia (evita el rebindeo masivo del ItemsControl que provocaba
    /// congelamiento al eliminar la imagen que se mostraba en el sidebar).
    /// </summary>
    private void SincronizarImagenesCarruselDesde(List<ImagenCarrusel> origen)
    {
        ImagenesCarrusel.Clear();
        foreach (var img in origen) ImagenesCarrusel.Add(img);
    }

    public async Task EliminarImagenCarruselAsync(ImagenCarrusel item)
    {
        if (item == null) return;
        try
        {
            bool confirmado = await ConfirmarAsync(
                "Confirmar eliminacion",
                $"Esta seguro de que desea eliminar la imagen \"{item.Nombre}\" del carrusel?");
            if (!confirmado) return;

            // Eliminar del almacenamiento persistente (JSON) por indice estable
            var lista = _imageService.ObtenerImagenesCarrusel();
            var indice = lista.FindIndex(i => i.Ruta == item.Ruta && i.Nombre == item.Nombre);
            if (indice >= 0) _imageService.EliminarImagenCarrusel(indice);

            // Quitar el item de la ObservableCollection en el sitio (notifica
            // CollectionChanged individual al ItemsControl -> sin rebindeo masivo).
            if (ImagenesCarrusel.Contains(item))
                ImagenesCarrusel.Remove(item);

            // Postergar al proximo ciclo del dispatcher para que el ItemsControl
            // del panel admin termine de procesar el CollectionChanged antes de
            // que el sidebar actualice su imagen (evita deadlock de render entre
            // el item eliminado del admin y el bitmap del sidebar del MainWindow).
            Dispatcher.UIThread.Post(() => AppMessenger.NotifyImagesChanged(), DispatcherPriority.Background);

            MostrarExito("Imagen de carrusel eliminada.");
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private List<ReporteItem> Filtrar(List<ReporteItem> items)
    {
        if (string.IsNullOrWhiteSpace(BusquedaReporte)) return items;
        var s = BusquedaReporte.ToLower();
        return items.Where(r => r.NombreCompleto.ToLower().Contains(s) || r.Matricula.ToLower().Contains(s)).ToList();
    }

    private List<ReporteDetalleItem> Filtrar(List<ReporteDetalleItem> items)
    {
        if (string.IsNullOrWhiteSpace(BusquedaReporte)) return items;
        var s = BusquedaReporte.ToLower();
        return items.Where(r => r.Nombre.ToLower().Contains(s) || r.Matricula.ToLower().Contains(s)).ToList();
    }

    private List<ReporteItem> AplicarPaginacion(List<ReporteItem> items)
    {
        return items.Skip((PaginaActual - 1) * _registrosPorPagina).Take(_registrosPorPagina).ToList();
    }

    private List<ReporteDetalleItem> AplicarPaginacion(List<ReporteDetalleItem> items)
    {
        return items.Skip((PaginaActual - 1) * _registrosPorPagina).Take(_registrosPorPagina).ToList();
    }

    private void MostrarError(string mensaje)
    {
        Mensaje = mensaje;
        EsError = true;
        MostrarMensaje = true;
        _ = OcultarMensajeAsync();
    }

    private void MostrarExito(string mensaje)
    {
        Mensaje = mensaje;
        EsError = false;
        MostrarMensaje = true;
        _ = OcultarMensajeAsync();
    }

    private async Task OcultarMensajeAsync()
    {
        await Task.Delay(3000);
        MostrarMensaje = false;
    }

    // Personas disponibles para vincular con administradores
    private List<Personas> _personasDisponiblesAdmin = new();
    private Personas? _personaSeleccionadaAdmin;

    public List<Personas> PersonasDisponiblesAdmin
    {
        get => _personasDisponiblesAdmin;
        set => SetProperty(ref _personasDisponiblesAdmin, value);
    }

    public Personas? PersonaSeleccionadaAdmin
    {
        get => _personaSeleccionadaAdmin;
        set => SetProperty(ref _personaSeleccionadaAdmin, value);
    }

    // Administradores CRUD
    public async Task CargarAdministradoresAsync()
    {
        try
        {
            Administradores = await _adminService.ObtenerTodosAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error al cargar administradores: {ex.Message}");
        }
    }

    private async Task GuardarAdminAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AdminEnEdicion.Usuario))
            {
                MostrarError("El usuario es obligatorio.");
                return;
            }
            if (PersonaSeleccionadaAdmin == null)
            {
                MostrarError("Debe seleccionar una persona.");
                return;
            }
            AdminEnEdicion.IdPersona = PersonaSeleccionadaAdmin.IdPersona;
            if (ModoEdicionAdminCrud)
            {
                await _adminService.ActualizarAsync(AdminEnEdicion);
                MostrarExito("Administrador actualizado.");
            }
            else
            {
                await _adminService.CrearAsync(AdminEnEdicion);
                MostrarExito("Administrador creado.");
            }
            AdminEnEdicion = new Administrador();
            PersonaSeleccionadaAdmin = null;
            ModoEdicionAdminCrud = false;
            await CargarAdministradoresAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private async Task EliminarAdminAsync()
    {
        if (AdminSeleccionado == null) return;
        try
        {
            if (AdminSeleccionado.IdAdmin <= 0)
            {
                MostrarError("No puede eliminar el administrador por defecto.");
                return;
            }
            bool confirmado = await ConfirmarAsync(
                "Confirmar eliminacion",
                $"Esta seguro de que desea eliminar al administrador \"{AdminSeleccionado.Usuario}\"? Esta accion no se puede deshacer.");
            if (!confirmado) return;
            await _adminService.EliminarAsync(AdminSeleccionado.IdAdmin);
            MostrarExito("Administrador eliminado.");
            await CargarAdministradoresAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private async Task CapturarHuellaAsync()
    {
        FingerprintLogger.LogEnrollment($"CapturarHuellaAsync: Iniciando captura para {PersonaEnEdicion?.Matricula}");
        try
        {
            if (PersonaEnEdicion == null)
            {
                FingerprintLogger.LogError("Capturar大屏幕HuellaAsync: PersonaEnEdicion nulo");
                MostrarError("No hay persona seleccionada para capturar la huella.");
                return;
            }

            if (!_fingerprintService.IsAvailable)
            {
                FingerprintLogger.LogHardware("Lector no disponible, intentando inicializar...");
                string resultado = _fingerprintService.Inicializar();
                if (!_fingerprintService.IsAvailable)
                {
                    FingerprintLogger.LogError($"No se pudo inicializar el lector: {resultado}");
                    MostrarError($"No se pudo inicializar el lector: {resultado}");
                    return;
                }
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var dialog = new FingerprintEnrollmentDialog();
                bool? resultadoDialog = await dialog.ShowEnrollment(_fingerprintService, desktop.MainWindow);
                var fmdBytes = dialog.CapturedFmd;

                if (resultadoDialog == true && fmdBytes != null)
                {
                    EstadoHuellaText = "Procesando huella...";
                    FingerprintLogger.LogEnrollment($"Captura completa. FMD bytes: {fmdBytes.Length} bytes");

                    var (exito, mensaje) = await _fingerprintService.GuardarHuellaAsync(PersonaEnEdicion.Matricula, fmdBytes);

                    if (!exito)
                    {
                        FingerprintLogger.LogError($"GuardarHuellaAsync fallo: {mensaje}");
                        EstadoHuellaText = $"Error al guardar: {mensaje}";
                        MostrarError(EstadoHuellaText);
                        return;
                    }

                    PersonaEnEdicion.Huella = fmdBytes;
                    EstadoHuellaText = "Huella registrada correctamente.";
                    FingerprintLogger.LogEnrollment($"Huella guardada OK para {PersonaEnEdicion.Matricula}");
                    MostrarExito(mensaje);
                }
                else
                {
                    FingerprintLogger.LogEnrollment("Captura de huella cancelada por el usuario");
                    EstadoHuellaText = "Captura cancelada.";
                }
            }
            else
            {
                FingerprintLogger.LogError("No se pudo abrir el dialogo de captura");
                EstadoHuellaText = "Error: No se pudo abrir el dialogo de captura.";
                MostrarError(EstadoHuellaText);
            }
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"CapturarHuellaAsync: Error - {ex}");
            EstadoHuellaText = "Error: " + ex.Message;
            MostrarError($"Error al capturar la huella: {ex.Message}");
        }
    }

    private async Task EliminarHuellaAsync()
    {
        FingerprintLogger.LogEnrollment($"EliminarHuellaAsync: Eliminando huella de {PersonaEnEdicion?.Matricula}");
        try
        {
            if (PersonaEnEdicion == null)
            {
                FingerprintLogger.LogError("EliminarHuellaAsync: PersonaEnEdicion nulo");
                MostrarError("No hay persona seleccionada para eliminar la huella.");
                return;
            }

            var (exito, mensaje) = await _fingerprintService.EliminarHuellaAsync(PersonaEnEdicion.Matricula);
            if (exito)
            {
                PersonaEnEdicion.Huella = null;
                EstadoHuellaText = "Huella Eliminada";
                FingerprintLogger.LogEnrollment($"Huella eliminada OK para {PersonaEnEdicion.Matricula}");
                MostrarExito(mensaje);
                
                // Limpiar formulario y buscador despues de eliminar huella
                PersonaEnEdicion = new Personas();
                PersonaSeleccionada = null;
                BusquedaAdmin = "";
                EstadoHuellaText = "Sin Capturar";
            }
            else
            {
                FingerprintLogger.LogError($"EliminarHuellaAsync fallo: {mensaje}");
                EstadoHuellaText = mensaje;
                MostrarError(mensaje);
            }
            await CargarPersonasAdminAsync();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"EliminarHuellaAsync: Error - {ex}");
            EstadoHuellaText = "Error: " + ex.Message;
            MostrarError("Error al eliminar la huella: " + ex.Message);
        }
    }

    private async Task MostrarDialogoAgregarPersonaAsync()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var dialog = new NuevoPersonaDialog();
                bool? resultado = await dialog.ShowAddPerson(_fingerprintService, desktop.MainWindow);

                if (resultado == true)
                {
                    var datos = dialog.DatosPersona;
                    
                    // Crear la nueva persona
                    var nuevaPersona = new Personas
                    {
                        Nombre = datos.Nombre,
                        ApellidoPaterno = datos.ApellidoPaterno,
                        ApellidoMaterno = datos.ApellidoMaterno,
                        Matricula = datos.Matricula,
                        TipoPersona = datos.TipoPersona,
                        Imagen = string.IsNullOrWhiteSpace(datos.Imagen) ? @"C:\FotografiasHuella\Dios.jpg" : datos.Imagen
                    };

                    await _personasService.CrearAsync(nuevaPersona);

                    // Si se capturo huella, guardarla
                    if (dialog.CapturedFmd != null && dialog.CapturedFmd.Length > 0)
                    {
                        await _fingerprintService.GuardarHuellaAsync(datos.Matricula, dialog.CapturedFmd);
                    }

                    _todasLasPersonas.Add(nuevaPersona);
                    Personas = new List<Personas>(_todasLasPersonas);
                    FiltrarPersonasAdmin();
                    AppMessenger.NotifyPersonasChanged();

                    MostrarExito($"Persona agregada correctamente.{(dialog.CapturedFmd != null ? " Huella capturada." : "")}");

                    // Limpiar formulario y buscador
                    PersonaEnEdicion = new Personas();
                    BusquedaAdmin = "";
                }
            }
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"MostrarDialogoAgregarPersonaAsync: Error - {ex}");
            MostrarError($"Error al agregar persona: {ex.Message}");
        }
    }
}
