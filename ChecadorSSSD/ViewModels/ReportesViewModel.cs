using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.ViewModels;

public class ReportesViewModel : ViewModelBase
{
    private readonly ReportesService _reportesService;

    private DateTimeOffset _fechaInicio = DateTimeOffset.Now.AddDays(-30);
    private DateTimeOffset _fechaFin = DateTimeOffset.Now;
    private string? _TipoPersonaSeleccionado;
    private List<ReporteItem> _resultados = new();
    private bool _mostrarResultados;
    private string _mensaje = string.Empty;
    private bool _esError;
    private bool _mostrarMensaje;

    public DateTimeOffset FechaInicio
    {
        get => _fechaInicio;
        set => SetProperty(ref _fechaInicio, value);
    }

    public DateTimeOffset FechaFin
    {
        get => _fechaFin;
        set => SetProperty(ref _fechaFin, value);
    }

    public string? TipoPersonaSeleccionado
    {
        get => _TipoPersonaSeleccionado;
        set => SetProperty(ref _TipoPersonaSeleccionado, value);
    }

    public List<string> TiposUsuario => new() { "Todos", "Brigadista", "Asesor", "Personal Administrativo" };

    public List<ReporteItem> Resultados
    {
        get => _resultados;
        set => SetProperty(ref _resultados, value);
    }

    public bool MostrarResultados
    {
        get => _mostrarResultados;
        set => SetProperty(ref _mostrarResultados, value);
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

    public ICommand GenerarCommand { get; }
    public ICommand LimpiarCommand { get; }

    public ReportesViewModel(ReportesService reportesService)
    {
        _reportesService = reportesService;
        GenerarCommand = new RelayCommand(async () => await GenerarAsync());
        LimpiarCommand = new RelayCommand(() =>
        {
            Resultados = new List<ReporteItem>();
            MostrarResultados = false;
        });
    }

    private async Task GenerarAsync()
    {
        try
        {
            if (FechaInicio > FechaFin)
            {
                Mensaje = "La fecha de inicio no puede ser mayor que la fecha de fin.";
                EsError = true;
                MostrarMensaje = true;
                return;
            }

            TipoPersona? tipo = null;
            if (!string.IsNullOrEmpty(TipoPersonaSeleccionado) && TipoPersonaSeleccionado != "Todos")
            {
                tipo = Enum.Parse<TipoPersona>(TipoPersonaSeleccionado.Replace(" ", ""));
            }

            var fechaInicio = FechaInicio.Date;
            var fechaFin = FechaFin.Date;

            Resultados = await _reportesService.GenerarReporteAsync(fechaInicio, fechaFin, tipo);
            MostrarResultados = true;

            if (Resultados.Count == 0)
            {
                Mensaje = "No se encontraron registros para el período seleccionado.";
                EsError = false;
                MostrarMensaje = true;
            }
            else
            {
                MostrarMensaje = false;
            }
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al generar el reporte: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }
}
