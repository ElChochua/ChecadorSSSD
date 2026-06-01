using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
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
    private readonly FingerprintReaderService _fingerprintReader;
    private readonly HuellaDactilarService _huellaDactilarService;

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
    private List<ImagenCarrusel> _imagenesCarrusel = new();

    // Imagenes - vistas previas de logos
    private string? _vistaPreviaLogo = null;
    private string? _vistaPreviaLugar = null;
    private string? _vistaPreviaLogoUniversidad = null;

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

    public string TituloFormulario => ModoEdicionAdmin ? "Editar Persona" : "Agregar Persona";

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
            if (SetProperty(ref _personaSeleccionada, value) && value != null)
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
                ModoEdicionAdmin = true;
            }
        }
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
                VistaPreviaLogo = _imageService.LoadImage(value) != null ? value : null;
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
                VistaPreviaLugar = _imageService.LoadImage(value) != null ? value : null;
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
                VistaPreviaLogoUniversidad = _imageService.LoadImage(value) != null ? value : null;
            }
        }
    }

    // Vistas previas de logos
    public string? VistaPreviaLogo
    {
        get => _vistaPreviaLogo;
        set => SetProperty(ref _vistaPreviaLogo, value);
    }

    public bool HasVistaPreviaLogo => !string.IsNullOrEmpty(VistaPreviaLogo);

    public string? VistaPreviaLugar
    {
        get => _vistaPreviaLugar;
        set => SetProperty(ref _vistaPreviaLugar, value);
    }

    public bool HasVistaPreviaLugar => !string.IsNullOrEmpty(VistaPreviaLugar);

    public string? VistaPreviaLogoUniversidad
    {
        get => _vistaPreviaLogoUniversidad;
        set => SetProperty(ref _vistaPreviaLogoUniversidad, value);
    }

    public bool HasVistaPreviaLogoUniversidad => !string.IsNullOrEmpty(VistaPreviaLogoUniversidad);

    public void GuardarRutaLogo(string path)
    {
        RutaLogo = path;
        _imageService.SetLogoChecador(path);
        VistaPreviaLogo = path;
        OnPropertyChanged(nameof(VistaPreviaLogo));
        OnPropertyChanged(nameof(HasVistaPreviaLogo));
        AppMessenger.NotifyImagesChanged();
    }

    public void GuardarRutaLugar(string path)
    {
        RutaLugar = path;
        _imageService.SetImagenLugar(path);
        VistaPreviaLugar = path;
        OnPropertyChanged(nameof(VistaPreviaLugar));
        OnPropertyChanged(nameof(HasVistaPreviaLugar));
        AppMessenger.NotifyImagesChanged();
    }

    public void GuardarRutaUniversidad(string path)
    {
        RutaUniversidad = path;
        _imageService.SetImagenUniversidad(path);
        VistaPreviaLogoUniversidad = path;
        OnPropertyChanged(nameof(VistaPreviaLogoUniversidad));
        OnPropertyChanged(nameof(HasVistaPreviaLogoUniversidad));
        AppMessenger.NotifyImagesChanged();
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

    public List<ImagenCarrusel> ImagenesCarrusel
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

    public AdministracionViewModel(
        PersonasService personasService,
        ReportesService reportesService,
        AdministradorService adminService,
        AuthService authService,
        FingerprintReaderService fingerprintReader,
        HuellaDactilarService huellaDactilarService)
    {
        _personasService = personasService;
        _reportesService = reportesService;
        _adminService = adminService;
        _authService = authService;
        _fingerprintReader = fingerprintReader;
        _huellaDactilarService = huellaDactilarService;
        _imageService = new ImageService();

        PersonasFiltradasReporte = new List<Personas>();
        PersonasSeleccionadasReporte = new List<Personas>();

        LoginCommand = new RelayCommand(async () => await LoginAsync());
        LogoutCommand = new RelayCommand(() => { Autenticado = false; MensajeLogin = string.Empty; UsuarioLogin = string.Empty; PassLogin = string.Empty; });
        
        // Aliases para XAML
        EditarCommand = new RelayCommand<Personas>(p => { if (p != null) PersonaSeleccionada = p; });
        EliminarCommand = new RelayCommand<Personas>(async p => 
        { 
            if (p != null) 
            { 
                PersonaSeleccionada = p; 
                await EliminarPersonaAdminAsync(); 
            } 
        });
        GuardarCommand = new RelayCommand(async () => await GuardarPersonaAdminAsync());
        CancelarCommand = new RelayCommand(() => { PersonaEnEdicion = new Personas(); ModoEdicionAdmin = false; });
        
        NuevoPersonaCommand = new RelayCommand(() => { PersonaEnEdicion = new Personas(); ModoEdicionAdmin = false; });
        GuardarPersonaCommand = new RelayCommand(async () => await GuardarPersonaAdminAsync());
        EliminarPersonaCommand = new RelayCommand(async () => await EliminarPersonaAdminAsync());
        BuscarPersonaCommand = new RelayCommand(async () => await BuscarPersonaAdminAsync());
        GenerarReporteIndividuaCommand = new RelayCommand(async () => await GenerarReporteIndividualAsync());
        AgregarPersonaReporteCommand = new RelayCommand<Personas>(p => { if (p != null) AgregarPersonaReporte(p); });
        QuitarPersonaReporteCommand = new RelayCommand<Personas>(p => { if (p != null) QuitarPersonaReporte(p); });
        GuardarImagenLogoCommand = new RelayCommand(() => GuardarImagenConfig(nameof(RutaLogo), RutaLogo));
        GuardarImagenLugarCommand = new RelayCommand(() => GuardarImagenConfig(nameof(RutaLugar), RutaLugar));
        GuardarImagenUniversidadCommand = new RelayCommand(() => GuardarImagenConfig(nameof(RutaUniversidad), RutaUniversidad));
        AgregarImagenCarruselCommand = new RelayCommand(() => AgregarImagenCarrusel());
        EliminarImagenCarruselCommand = new RelayCommand<ImagenCarrusel>(item => EliminarImagenCarrusel(item));
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

        // Obtenemos las administradores guardados
        RutaLogo = _imageService.LogoChecadorPath;
        RutaLugar = _imageService.ImagenLugarPath;
        RutaUniversidad = _imageService.ImagenUniversidadPath;
        ImagenesCarrusel = _imageService.ImagenesCarrusel;
    }

    private async Task LoginAsync()
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
        if (string.IsNullOrWhiteSpace(BusquedaAdmin))
        {
            Personas = new List<Personas>(_todasLasPersonas);
            return;
        }
        var f = BusquedaAdmin.ToLower();
        Personas = _todasLasPersonas.Where(p =>
            p.Nombre.ToLower().Contains(f) ||
            (p.ApellidoPaterno ?? "").ToLower().Contains(f) ||
            (p.ApellidoMaterno ?? "").ToLower().Contains(f) ||
            p.Matricula.ToLower().Contains(f))
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
            if (ModoEdicionAdmin)
            {
                await _personasService.ActualizarAsync(PersonaEnEdicion);
                MostrarExito("Persona actualizada.");
            }
            else
            {
                await _personasService.CrearAsync(PersonaEnEdicion);
                MostrarExito("Persona creada.");
            }
            PersonaEnEdicion = new Personas();
            ModoEdicionAdmin = false;
            await CargarPersonasAdminAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private async Task EliminarPersonaAdminAsync()
    {
        if (PersonaSeleccionada == null) return;
        try
        {
            await _personasService.EliminarAsync(PersonaSeleccionada.IdPersona);
            MostrarExito("Persona eliminada.");
            await CargarPersonasAdminAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
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
        if (string.IsNullOrWhiteSpace(RutaImagenCarrusel) || string.IsNullOrWhiteSpace(NombreImagenCarrusel))
        {
            MostrarError("Proporcione ruta y nombre de la imagen.");
            return;
        }
        _imageService.AgregarImagenCarrusel(RutaImagenCarrusel, NombreImagenCarrusel);
        ImagenesCarrusel = _imageService.ImagenesCarrusel;
        RutaImagenCarrusel = string.Empty;
        NombreImagenCarrusel = string.Empty;
        OnPropertyChanged(nameof(ImagenesCarrusel));
        AppMessenger.NotifyImagesChanged();
        MostrarExito("Imagen de carrusel agregada.");
    }

    public void EliminarImagenCarrusel(ImagenCarrusel item)
    {
        var lista = _imageService.ObtenerImagenesCarrusel();
        var indice = lista.FindIndex(i => i.Ruta == item.Ruta && i.Nombre == item.Nombre);
        if (indice >= 0) _imageService.EliminarImagenCarrusel(indice);
        ImagenesCarrusel = _imageService.ImagenesCarrusel;
        OnPropertyChanged(nameof(ImagenesCarrusel));
        AppMessenger.NotifyImagesChanged();
        MostrarExito("Imagen de carrusel eliminada.");
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
    }

    private void MostrarExito(string mensaje)
    {
        Mensaje = mensaje;
        EsError = false;
        MostrarMensaje = true;
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
        try
        {
            if (PersonaEnEdicion == null)
            {
                MostrarError("No hay persona seleccionada para capturar la huella.");
                return;
            }

            if (!_fingerprintReader.IsAvailable)
            {
                string resultado = _fingerprintReader.Initialize();
                if (!_fingerprintReader.IsAvailable)
                {
                    MostrarError($"No se pudo inicializar el lector: {resultado}");
                    return;
                }
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var dialog = new FingerprintCaptureDialog();
                var captureVm = new FingerprintCaptureViewModel(_fingerprintReader);
                dialog.Initialize(captureVm);

                // Store the result from the dialog
                bool? resultadoDialog = await dialog.ShowDialog<bool?>(desktop.MainWindow);

                if (resultadoDialog == true && captureVm.CapturedFmd != null)
                {
                    EstadoHuellaText = "Procesando huella...";
                    byte[]? fmd = captureVm.CapturedFmd;

                    var resultadoGuardado = await _huellaDactilarService.GuardarHuellaAsync(PersonaEnEdicion.Matricula, fmd);

                    if (!resultadoGuardado.exito)
                    {
                        EstadoHuellaText = $"Error al guardar: {resultadoGuardado.mensaje}";
                        MostrarError(EstadoHuellaText);
                        return;
                    }

                    PersonaEnEdicion.Huella = fmd;
                    EstadoHuellaText = "Huella registrada correctamente.";
                    MostrarExito(resultadoGuardado.mensaje);
                }
                else
                {
                    EstadoHuellaText = "Captura cancelada.";
                }
            }
            else
            {
                EstadoHuellaText = "Error: No se pudo abrir el diálogo de captura.";
                MostrarError(EstadoHuellaText);
            }
        }
        catch (Exception ex)
        {
            EstadoHuellaText = "Error: " + ex.Message;
            MostrarError($"Error al capturar la huella: {ex.Message}");
        }
    }
}
