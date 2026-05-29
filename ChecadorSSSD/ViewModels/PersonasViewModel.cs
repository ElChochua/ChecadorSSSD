using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.ViewModels;

/// <summary>
/// ViewModel de la vista publica de Personas (solo busqueda, ver, copiar).
/// </summary>
public class PersonasViewModel : ViewModelBase
{
    private readonly PersonasService _personasService;

    private List<Personas> _personas = new();
    private List<Personas> _todasLasPersonas = new();
    private Personas? _personaSeleccionada;
    private string _busqueda = string.Empty;
    private string _mensaje = string.Empty;
    private bool _esError;
    private bool _mostrarMensaje;

    // Paginacion
    private int _paginaActual = 1;
    private const int RegistrosPorPagina = 50;
    private int _totalRegistros = 0;

    // Modal inline
    private Personas? _personaEnModal;
    private bool _mostrarModal = false;

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
                // Cuando se selecciona, simplemente se muestra en VerPersonaView
            }
        }
    }

    public string Busqueda
    {
        get => _busqueda;
        set
        {
            if (SetProperty(ref _busqueda, value))
            {
                _paginaActual = 1;
                FiltrarPersonas();
                OnPropertyChanged(nameof(RangoPaginaText));
            }
        }
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

    // Paginacion
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling((double)_totalRegistros / RegistrosPorPagina));

    public string RangoPaginaText => $"Pagina {_paginaActual} de {TotalPaginas} (Total: {_totalRegistros})";

    public bool PaginaAnteriorPuedeEjecutarse => _paginaActual > 1;
    public bool PaginaSiguientePuedeEjecutarse => _paginaActual < TotalPaginas;

    // Modal inline
    public Personas? PersonaEnModal
    {
        get => _personaEnModal;
        set
        {
            if (SetProperty(ref _personaEnModal, value))
            {
                MostrarModal = _personaEnModal != null;
            }
        }
    }

    public bool MostrarModal
    {
        get => _mostrarModal;
        set => SetProperty(ref _mostrarModal, value);
    }

    // Comandos
    public ICommand VerCommand { get; }
    public ICommand CopiarMatriculaCommand { get; }
    public ICommand PaginaAnteriorCommand { get; }
    public ICommand PaginaSiguienteCommand { get; }
    public ICommand CerrarModalCommand { get; }

    public PersonasViewModel(PersonasService personasService)
    {
        _personasService = personasService;

        VerCommand = new RelayCommand<Personas>((persona) =>
        {
            if (persona != null) PersonaEnModal = persona;
        });

        CopiarMatriculaCommand = new RelayCommand<Personas>(async (persona) =>
        {
            if (persona == null || string.IsNullOrEmpty(persona.Matricula)) return;
            await CopiarMatriculaAsync(persona.Matricula);
        });

        PaginaAnteriorCommand = new RelayCommand(() =>
        {
            if (_paginaActual > 1)
            {
                _paginaActual--;
                FiltrarPersonas();
                OnPropertyChanged(nameof(RangoPaginaText));
            }
        }, () => _paginaActual > 1);

        PaginaSiguienteCommand = new RelayCommand(() =>
        {
            if (_paginaActual < TotalPaginas)
            {
                _paginaActual++;
                FiltrarPersonas();
                OnPropertyChanged(nameof(RangoPaginaText));
            }
        }, () => _paginaActual < TotalPaginas);

        CerrarModalCommand = new RelayCommand(() =>
        {
            PersonaEnModal = null;
        });

        _ = CargarPersonasAsync();
    }

    private async Task CargarPersonasAsync()
    {
        try
        {
            _todasLasPersonas = await _personasService.ObtenerTodasAsync();
            Personas = new List<Personas>(_todasLasPersonas);
            _totalRegistros = Personas.Count;
            OnPropertyChanged(nameof(RangoPaginaText));
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar datos: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }

    /// <summary>
    /// Actualiza la lista si se agrega un usuario desde administracion.
    /// </summary>
    public void RecargarPersonas()
    {
        _ = CargarPersonasAsync();
    }

    private void FiltrarPersonas()
    {
        if (string.IsNullOrWhiteSpace(Busqueda))
        {
            Personas = new List<Personas>(_todasLasPersonas.Skip((_paginaActual - 1) * RegistrosPorPagina).Take(RegistrosPorPagina));
            _totalRegistros = _todasLasPersonas.Count;
            return;
        }

        var filtro = Busqueda.ToLower();
        var filtradas = _todasLasPersonas
            .Where(p =>
                p.Nombre.ToLower().Contains(filtro) ||
                p.ApellidoPaterno?.ToLower().Contains(filtro) == true ||
                p.ApellidoMaterno?.ToLower().Contains(filtro) == true ||
                p.Matricula.ToLower().Contains(filtro))
            .ToList();

        _totalRegistros = filtradas.Count;
        Personas = new List<Personas>(filtradas.Skip((_paginaActual - 1) * RegistrosPorPagina).Take(RegistrosPorPagina));
    }

    private async Task CopiarMatriculaAsync(string matricula)
    {
        try
        {
            await ClipboardHelper.SetTextAsync(matricula);
            Mensaje = "Matricula copiada al portapapeles.";
            EsError = false;
            MostrarMensaje = true;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al copiar: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }
}
