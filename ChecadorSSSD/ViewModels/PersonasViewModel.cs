using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;
using ChecadorSSSD.Views;

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

    // Ordenacion
    private string _ordenarPor = string.Empty;
    private bool _ordenAscendente = true;

    // Ventana modal nueva
    private Window? _ventanaDetalle;

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

    // Ordenacion
    public string OrdenarPor
    {
        get => _ordenarPor;
        set => SetProperty(ref _ordenarPor, value);
    }

    public bool OrdenAscendente
    {
        get => _ordenAscendente;
        set => SetProperty(ref _ordenAscendente, value);
    }

    // Comandos
    public ICommand VerCommand { get; }
    public ICommand CopiarMatriculaCommand { get; }
    public ICommand PaginaAnteriorCommand { get; }
    public ICommand PaginaSiguienteCommand { get; }
    public ICommand OrdenarHeaderCommand { get; }

    // Comandos con CanExecute dinámico
    private RelayCommand _paginaAnteriorCommand = null!;
    private RelayCommand _paginaSiguienteCommand = null!;

    public PersonasViewModel(PersonasService personasService)
    {
        _personasService = personasService;

        VerCommand = new RelayCommand<Personas>((persona) =>
        {
            if (persona != null) AbrirVentanaDetalle(persona);
        });

        CopiarMatriculaCommand = new RelayCommand<Personas>(async (persona) =>
        {
            if (persona == null || string.IsNullOrEmpty(persona.Matricula)) return;
            await CopiarMatriculaAsync(persona.Matricula);
        });

        _paginaAnteriorCommand = new RelayCommand(() =>
        {
            if (_paginaActual > 1)
            {
                _paginaActual--;
                FiltrarPersonas();
                OnPropertyChanged(nameof(RangoPaginaText));
                OnPropertyChanged(nameof(PaginaAnteriorPuedeEjecutarse));
                OnPropertyChanged(nameof(PaginaSiguientePuedeEjecutarse));
                _paginaAnteriorCommand.RaiseCanExecuteChanged();
                _paginaSiguienteCommand.RaiseCanExecuteChanged();
            }
        }, () => _paginaActual > 1);

        _paginaSiguienteCommand = new RelayCommand(() =>
        {
            if (_paginaActual < TotalPaginas)
            {
                _paginaActual++;
                FiltrarPersonas();
                OnPropertyChanged(nameof(RangoPaginaText));
                OnPropertyChanged(nameof(PaginaAnteriorPuedeEjecutarse));
                OnPropertyChanged(nameof(PaginaSiguientePuedeEjecutarse));
                _paginaAnteriorCommand.RaiseCanExecuteChanged();
                _paginaSiguienteCommand.RaiseCanExecuteChanged();
            }
        }, () => _paginaActual < TotalPaginas);

        PaginaAnteriorCommand = _paginaAnteriorCommand;
        PaginaSiguienteCommand = _paginaSiguienteCommand;

        OrdenarHeaderCommand = new RelayCommand<string>((columna) => OrdenarHeader(columna));

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

    private void AbrirVentanaDetalle(Personas persona)
    {
        try
        {
            // Cerrar ventana anterior si existe
            _ventanaDetalle?.Close();

            var ventana = new VerPersonaView(persona);
            _ventanaDetalle = ventana;

            // Buscar la ventana principal para usar como owner
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    ventana.ShowDialog(mainWindow);
                }
                else
                {
                    ventana.Show();
                }
            }
            else
            {
                ventana.Show();
            }
        }
        catch
        {
            // Si falla abrir la ventana, simplemente ignorar
        }
    }

    /// <summary>
    /// Recarga la lista de personas desde la base de datos.
    /// </summary>
    public void RecargarPersonas()
    {
        _ = CargarPersonasAsync();
    }

    private void FiltrarPersonas()
    {
        IEnumerable<Personas> query = _todasLasPersonas;

        // Aplicar busqueda
        if (!string.IsNullOrWhiteSpace(Busqueda))
        {
            var filtro = Busqueda.ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(filtro) ||
                p.ApellidoPaterno?.ToLower().Contains(filtro) == true ||
                p.ApellidoMaterno?.ToLower().Contains(filtro) == true ||
                p.Matricula.ToLower().Contains(filtro));
        }

        // Aplicar ordenacion
        query = OrdenarPor switch
        {
            "Nombre" => _ordenAscendente ? query.OrderBy(p => p.Nombre) : query.OrderByDescending(p => p.Nombre),
            "Matricula" => _ordenAscendente ? query.OrderBy(p => p.Matricula) : query.OrderByDescending(p => p.Matricula),
            "Rol" => _ordenAscendente ? query.OrderBy(p => p.TipoPersona) : query.OrderByDescending(p => p.TipoPersona),
            _ => query.OrderBy(p => p.Nombre)
        };

        var filtradas = query.ToList();
        _totalRegistros = filtradas.Count;
        Personas = new List<Personas>(filtradas.Skip((_paginaActual - 1) * RegistrosPorPagina).Take(RegistrosPorPagina));
    }

    public void OrdenarHeader(string columna)
    {
        if (OrdenarPor == columna)
        {
            OrdenAscendente = !OrdenAscendente;
        }
        else
        {
            OrdenarPor = columna;
            OrdenAscendente = true;
        }
        FiltrarPersonas();
        OnPropertyChanged(nameof(Personas));
    }

    private async Task CopiarMatriculaAsync(string matricula)
    {
        try
        {
            await ClipboardHelper.SetTextAsync(matricula);
            Mensaje = "¡Matrícula copiada exitosamente al portapapeles!";
            EsError = false;
            MostrarMensaje = true;
            _ = OcultarMensajeAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al copiar: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
            _ = OcultarMensajeAsync();
        }
    }

    private async Task OcultarMensajeAsync()
    {
        await Task.Delay(3000);
        MostrarMensaje = false;
    }
}
