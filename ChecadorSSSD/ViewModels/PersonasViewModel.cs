using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
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
                FiltrarPersonas();
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

    // Comandos
    public ICommand VerCommand { get; }
    public ICommand CopiarMatriculaCommand { get; }

    public PersonasViewModel(PersonasService personasService)
    {
        _personasService = personasService;

        VerCommand = new RelayCommand<Personas>(async (persona) =>
        {
            if (persona == null) return;
            await VerPersonaAsync(persona);
        });

        CopiarMatriculaCommand = new RelayCommand<Personas>(async (persona) =>
        {
            if (persona == null || string.IsNullOrEmpty(persona.Matricula)) return;
            await CopiarMatriculaAsync(persona.Matricula);
        });

        _ = CargarPersonasAsync();
    }

    private async Task CargarPersonasAsync()
    {
        try
        {
            _todasLasPersonas = await _personasService.ObtenerTodasAsync();
            Personas = new List<Personas>(_todasLasPersonas);
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar datos: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }

    private void FiltrarPersonas()
    {
        if (string.IsNullOrWhiteSpace(Busqueda))
        {
            Personas = new List<Personas>(_todasLasPersonas);
            return;
        }

        var filtro = Busqueda.ToLower();
        Personas = _todasLasPersonas
            .Where(p => 
                p.Nombre.ToLower().Contains(filtro) ||
                p.ApellidoPaterno.ToLower().Contains(filtro) ||
                p.ApellidoMaterno.ToLower().Contains(filtro) ||
                p.Matricula.ToLower().Contains(filtro))
            .ToList();
    }

    private async Task VerPersonaAsync(Personas persona)
    {
        try
        {
            var dialog = new Views.VerPersonaView(persona);
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                await dialog.ShowDialog(desktop.MainWindow);
            }
            else
            {
                dialog.Show();
            }
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al abrir detalle: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
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
