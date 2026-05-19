using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.ViewModels;

public class PersonasViewModel : ViewModelBase
{
    private readonly PersonasService _personasService;

    private List<Personas> _personas = new();
    private Personas? _personaSeleccionada;
    private Personas _personaEnEdicion = new();
    private bool _modoEdicion;
    private string _busqueda = string.Empty;
    private string _rutaFotoTemporal = string.Empty;
    private string _mensaje = string.Empty;
    private bool _esError;
    private bool _mostrarMensaje;
    private string _tituloFormulario = "Agregar Persona";

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
            SetProperty(ref _personaSeleccionada, value);
            if (value != null)
            {
                PersonaEnEdicion = new Personas
                {
                    IdPersonas = value.IdPersonas,
                    Nombre = value.Nombre,
                    ApellidoPaterno = value.ApellidoPaterno,
                    ApellidoMaterno = value.ApellidoMaterno,
                    Matricula = value.Matricula,
                    TipoPersona = value.TipoPersona,
                    Imagen = value.Imagen,
                    Huella = value.Huella
                };
                ModoEdicion = true;
                TituloFormulario = "Editar Persona";
            }
        }
    }

    public Personas PersonaEnEdicion
    {
        get => _personaEnEdicion;
        set => SetProperty(ref _personaEnEdicion, value);
    }

    public bool ModoEdicion
    {
        get => _modoEdicion;
        set => SetProperty(ref _modoEdicion, value);
    }

    public string Busqueda
    {
        get => _busqueda;
        set
        {
            SetProperty(ref _busqueda, value);
            FiltrarPersonas();
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

    public string TituloFormulario
    {
        get => _tituloFormulario;
        set => SetProperty(ref _tituloFormulario, value);
    }

    public List<string> TiposUsuario => new() { "Brigadista", "Asesor", "Personal Administrativo" };

    public ICommand NuevoCommand { get; }
    public ICommand GuardarCommand { get; }
    public ICommand EliminarCommand { get; }
    public ICommand EditarCommand { get; }
    public ICommand CancelarCommand { get; }
    public ICommand SeleccionarFotoCommand { get; }
    public ICommand CargarDatosCommand { get; }

    public PersonasViewModel(PersonasService personasService)
    {
        _personasService = personasService;

        NuevoCommand = new RelayCommand(() => LimpiarFormulario());
        GuardarCommand = new RelayCommand(async () => await GuardarAsync());
        EliminarCommand = new RelayCommand(async () => await EliminarAsync());
        EditarCommand = new RelayCommand(() =>
        {
            if (PersonaSeleccionada != null)
            {
                PersonaEnEdicion = new Personas
                {
                    IdPersonas = PersonaSeleccionada.IdPersonas,
                    Nombre = PersonaSeleccionada.Nombre,
                    ApellidoPaterno = PersonaSeleccionada.ApellidoPaterno,
                    ApellidoMaterno = PersonaSeleccionada.ApellidoMaterno,
                    Matricula = PersonaSeleccionada.Matricula,
                    TipoPersona = PersonaSeleccionada.TipoPersona,
                    Imagen = PersonaSeleccionada.Imagen,
                    Huella = PersonaSeleccionada.Huella
                };
                ModoEdicion = true;
                TituloFormulario = "Editar Persona";
            }
        });

        CancelarCommand = new RelayCommand(() =>
        {
            LimpiarFormulario();
            PersonaSeleccionada = null;
        });

        SeleccionarFotoCommand = new RelayCommand(SeleccionarFoto);
        CargarDatosCommand = new RelayCommand(async () => await CargarPersonasAsync());

        // Cargar datos al iniciar
        _ = CargarPersonasAsync();
    }

    private void LimpiarFormulario()
    {
        PersonaEnEdicion = new Personas();
        ModoEdicion = false;
        TituloFormulario = "Agregar Persona";
        PersonaSeleccionada = null;
        MostrarMensaje = false;
    }

    private async Task CargarPersonasAsync()
    {
        try
        {
            Personas = await _personasService.ObtenerTodasAsync();
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
            _ = CargarPersonasAsync();
            return;
        }

        var filtro = Busqueda.ToLower();
        Personas = Personas.Where(p =>
            p.Nombre.ToLower().Contains(filtro) ||
            p.ApellidoPaterno.ToLower().Contains(filtro) ||
            p.ApellidoMaterno.ToLower().Contains(filtro) ||
            p.Matricula.ToLower().Contains(filtro))
            .ToList();
    }

    private async Task GuardarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PersonaEnEdicion.Nombre) ||
                string.IsNullOrWhiteSpace(PersonaEnEdicion.ApellidoPaterno) ||
                string.IsNullOrWhiteSpace(PersonaEnEdicion.ApellidoMaterno) ||
                string.IsNullOrWhiteSpace(PersonaEnEdicion.Matricula))
            {
                Mensaje = "Por favor, complete todos los campos obligatorios.";
                EsError = true;
                MostrarMensaje = true;
                return;
            }

            // Guardar foto si se seleccionó una nueva
            if (!string.IsNullOrEmpty(_rutaFotoTemporal))
            {
                var rutaDestino = GuardarFoto(_rutaFotoTemporal, PersonaEnEdicion.IdPersonas);
                PersonaEnEdicion.Imagen = rutaDestino;
                _rutaFotoTemporal = string.Empty;
            }

            if (ModoEdicion)
            {
                await _personasService.ActualizarAsync(PersonaEnEdicion);
                Mensaje = "Persona actualizada correctamente.";
            }
            else
            {
                await _personasService.CrearAsync(PersonaEnEdicion);
                Mensaje = "Persona creada correctamente.";
            }

            EsError = false;
            MostrarMensaje = true;
            LimpiarFormulario();
            await CargarPersonasAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }

    private async Task EliminarAsync()
    {
        if (PersonaSeleccionada == null) return;

        try
        {
            await _personasService.EliminarAsync(PersonaSeleccionada.IdPersonas);
            Mensaje = "Persona eliminada correctamente.";
            EsError = false;
            MostrarMensaje = true;
            LimpiarFormulario();
            await CargarPersonasAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }

    private void SeleccionarFoto()
    {
        // Placeholder: Implementar diálogo de Avalonia para seleccionar foto
        Mensaje = "Función de selección de foto pendiente de implementación con diálogo de Avalonia.";
        EsError = true;
        MostrarMensaje = true;
    }

    private string GuardarFoto(string rutaOrigen, int personaId)
    {
        var extension = Path.GetExtension(rutaOrigen);
        var nombreArchivo = $"{personaId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var rutaDestino = Path.Combine("Assets", "Personas", nombreArchivo);
        var rutaCompleta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rutaDestino);

        Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);
        File.Copy(rutaOrigen, rutaCompleta, true);

        return rutaDestino;
    }
}
