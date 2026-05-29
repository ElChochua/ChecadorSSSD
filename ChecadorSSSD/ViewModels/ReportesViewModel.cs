using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.ViewModels;

public class ReportesViewModel : ViewModelBase
{
    private readonly ReportesService _reportesService;

    private DateTimeOffset _fechaInicio = DateTimeOffset.Now.AddDays(-30);
    private DateTimeOffset _fechaFin = DateTimeOffset.Now;
    private string? _tipoPersonaSeleccionado;
    private string? _tipoReporteSeleccionado = "Horas Mensual";
    private string _busqueda = string.Empty;
    private string _mensaje = string.Empty;
    private bool _esError;
    private bool _mostrarMensaje;
    private bool _mostrarResultados;

    private int _paginaActual = 1;
    private readonly int _registrosPorPagina = 50;

    private string _ordenarPor = string.Empty;
    private bool _ordenAscendente = true;

    private List<ReporteItem> _reporteHoras = new();
    private List<ReporteDetalleItem> _reporteUsuarios = new();

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
        get => _tipoPersonaSeleccionado;
        set => SetProperty(ref _tipoPersonaSeleccionado, value);
    }

    public string? TipoReporteSeleccionado
    {
        get => _tipoReporteSeleccionado;
        set
        {
            if (SetProperty(ref _tipoReporteSeleccionado, value))
            {
                // Notificar que las propiedades de visibilidad de reportes han cambiado
                OnPropertyChanged(nameof(EsReporteHoras));
                OnPropertyChanged(nameof(MostrarResultadosHoras));
                OnPropertyChanged(nameof(MostrarResultadosUsuarios));
            }
        }
    }

    public List<string> TiposUsuario => new() { "Todos", "Brigadista", "Asesor", "Personal Administrativo", "Empleado" };
    public List<string> TiposReporte => new() { "Horas Mensual", "Usuarios Mensual" };

    public string Busqueda
    {
        get => _busqueda;
        set
        {
            if (SetProperty(ref _busqueda, value))
            {
                _paginaActual = 1;
                OnPropertyChanged(nameof(ResultadosPaginadosHoras));
                OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
                OnPropertyChanged(nameof(TotalPaginas));
                OnPropertyChanged(nameof(RangoPaginaText));
            }
        }
    }

    public bool MostrarResultados
    {
        get => _mostrarResultados;
        set
        {
            if (SetProperty(ref _mostrarResultados, value))
            {
                OnPropertyChanged(nameof(MostrarResultadosHoras));
                OnPropertyChanged(nameof(MostrarResultadosUsuarios));
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
        ? Math.Max(1, (int)Math.Ceiling((double)(Filtrar(_reporteHoras).Count) / _registrosPorPagina))
        : Math.Max(1, (int)Math.Ceiling((double)(Filtrar(_reporteUsuarios).Count) / _registrosPorPagina));

    public string RangoPaginaText => $"Pagina {PaginaActual} de {TotalPaginas}";

    public bool PaginaAnteriorPuedeEjecutarse => PaginaActual > 1;
    public bool PaginaSiguientePuedeEjecutarse => PaginaActual < TotalPaginas;

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

    public bool EsReporteHoras => TipoReporteSeleccionado == "Horas Mensual";
    public bool EsReporteUsuarios => !EsReporteHoras;

    public List<ReporteItem> ResultadosPaginadosHoras => AplicarPaginacion(Filtrar(Ordenar(_reporteHoras).ToList()));
    public List<ReporteDetalleItem> ResultadosPaginadosUsuarios => AplicarPaginacion(Filtrar(Ordenar(_reporteUsuarios).ToList()));

    public ICommand GenerarCommand { get; }
    public ICommand LimpiarCommand { get; }
    public ICommand ExportarExcelCommand { get; }
    public ICommand PaginaAnteriorCommand { get; }
    public ICommand PaginaSiguienteCommand { get; }
    public ICommand OrdenarHeaderCommand { get; }

    // Comandos con CanExecute dinamico
    private RelayCommand _paginaAnteriorCommand = null!;
    private RelayCommand _paginaSiguienteCommand = null!;

    public bool MostrarResultadosHoras => MostrarResultados && EsReporteHoras;
    public bool MostrarResultadosUsuarios => MostrarResultados && !EsReporteHoras;

    public string CantidadRegistrosText => EsReporteHoras
        ? $"Mostrando {ResultadosPaginadosHoras.Count} de {_reporteHoras.Count} registros"
        : $"Mostrando {ResultadosPaginadosUsuarios.Count} de {_reporteUsuarios.Count} registros";

    public ReportesViewModel(ReportesService reportesService)
    {
        _reportesService = reportesService;
        GenerarCommand = new RelayCommand(async () => await GenerarAsync());
        LimpiarCommand = new RelayCommand(() =>
        {
            _reporteHoras = new List<ReporteItem>();
            _reporteUsuarios = new List<ReporteDetalleItem>();
            MostrarResultados = false;
            PaginaActual = 1;
            Busqueda = string.Empty;
            OnPropertyChanged(nameof(ResultadosPaginadosHoras));
            OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
        });
        ExportarExcelCommand = new RelayCommand(async () => await ExportarExcelAsync());
        _paginaAnteriorCommand = new RelayCommand(() => PaginaActual--, () => PaginaActual > 1);
        _paginaSiguienteCommand = new RelayCommand(() => PaginaActual++, () => PaginaActual < TotalPaginas);
        PaginaAnteriorCommand = _paginaAnteriorCommand;
        PaginaSiguienteCommand = _paginaSiguienteCommand;
        OrdenarHeaderCommand = new RelayCommand<string>((col) => OrdenarHeader(col));
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

            if (EsReporteHoras)
            {
                var (reporte, ok) = await _reportesService.GenerarReporteAsync(fechaInicio, fechaFin, tipo);
                _reporteHoras = reporte;
                _reporteUsuarios = new();

                if (!ok || _reporteHoras.Count == 0)
                {
                    Mensaje = "No se encontraron registros para el periodo seleccionado.";
                    EsError = false;
                    MostrarMensaje = true;
                    MostrarResultados = false;
                    return;
                }
            }
            else
            {
                var (reporte, ok) = await _reportesService.GenerarReporteUsuariosAsync(fechaInicio, fechaFin, tipo);
                _reporteUsuarios = reporte;
                _reporteHoras = new();

                if (!ok || _reporteUsuarios.Count == 0)
                {
                    Mensaje = "No se encontraron registros para el periodo seleccionado.";
                    EsError = false;
                    MostrarMensaje = true;
                    MostrarResultados = false;
                    return;
                }
            }

            MostrarResultados = true;
            MostrarMensaje = false;
            PaginaActual = 1;
            OnPropertyChanged(nameof(ResultadosPaginadosHoras));
            OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(RangoPaginaText));
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al generar el reporte: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }

    private List<ReporteItem> Filtrar(List<ReporteItem> items)
    {
        if (string.IsNullOrWhiteSpace(Busqueda)) return items;
        var s = Busqueda.ToLower();
        return items.Where(r =>
            r.NombreCompleto.ToLower().Contains(s) ||
            r.Matricula.ToLower().Contains(s)).ToList();
    }

    private List<ReporteDetalleItem> Filtrar(List<ReporteDetalleItem> items)
    {
        if (string.IsNullOrWhiteSpace(Busqueda)) return items;
        var s = Busqueda.ToLower();
        return items.Where(r =>
            r.Nombre.ToLower().Contains(s) ||
            r.ApellidoPaterno.ToLower().Contains(s) ||
            r.Matricula.ToLower().Contains(s)).ToList();
    }

    private IOrderedEnumerable<ReporteItem> Ordenar(List<ReporteItem> items)
    {
        var sorted = items.AsEnumerable();
        if (string.IsNullOrEmpty(OrdenarPor)) return items.OrderBy(r => r.NombreCompleto);

        return OrdenarPor switch
        {
            "NombreCompleto" => _ordenAscendente ? items.OrderBy(r => r.NombreCompleto) : items.OrderByDescending(r => r.NombreCompleto),
            "Matricula" => _ordenAscendente ? items.OrderBy(r => r.Matricula) : items.OrderByDescending(r => r.Matricula),
            "HorasAcumuladas" => _ordenAscendente ? items.OrderBy(r => r.HorasAcumuladas) : items.OrderByDescending(r => r.HorasAcumuladas),
            "TotalAsistencias" => _ordenAscendente ? items.OrderBy(r => r.TotalAsistencias) : items.OrderByDescending(r => r.TotalAsistencias),
            _ => items.OrderBy(r => r.NombreCompleto)
        };
    }

    private IOrderedEnumerable<ReporteDetalleItem> Ordenar(List<ReporteDetalleItem> items)
    {
        var sorted = items.AsEnumerable();
        if (string.IsNullOrEmpty(OrdenarPor)) return items.OrderBy(r => r.Nombre);

        return OrdenarPor switch
        {
            "Nombre" => _ordenAscendente ? items.OrderBy(r => r.Nombre) : items.OrderByDescending(r => r.Nombre),
            "Matricula" => _ordenAscendente ? items.OrderBy(r => r.Matricula) : items.OrderByDescending(r => r.Matricula),
            "Fecha" => _ordenAscendente ? items.OrderBy(r => r.Fecha) : items.OrderByDescending(r => r.Fecha),
            "HorasDiarias" => _ordenAscendente ? items.OrderBy(r => r.HorasDiarias) : items.OrderByDescending(r => r.HorasDiarias),
            _ => items.OrderBy(r => r.Nombre)
        };
    }

    private List<ReporteItem> AplicarPaginacion(List<ReporteItem> items)
    {
        return items.Skip((PaginaActual - 1) * _registrosPorPagina).Take(_registrosPorPagina).ToList();
    }

    private List<ReporteDetalleItem> AplicarPaginacion(List<ReporteDetalleItem> items)
    {
        return items.Skip((PaginaActual - 1) * _registrosPorPagina).Take(_registrosPorPagina).ToList();
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
        OnPropertyChanged(nameof(ResultadosPaginadosHoras));
        OnPropertyChanged(nameof(ResultadosPaginadosUsuarios));
    }

    private async Task ExportarExcelAsync()
    {
        if ((EsReporteHoras && (ResultadosPaginadosHoras == null || ResultadosPaginadosHoras.Count == 0))
            || (!EsReporteHoras && (ResultadosPaginadosUsuarios == null || ResultadosPaginadosUsuarios.Count == 0)))
        {
            Mensaje = "No hay resultados para exportar. Genere un reporte primero.";
            EsError = true;
            MostrarMensaje = true;
            return;
        }

        try
        {
            var fechaInicioStr = FechaInicio.ToString("yyyyMMdd");
            var fechaFinStr = FechaFin.ToString("yyyyMMdd");
            var nombreArchivo = $"Reporte_{fechaInicioStr}_{fechaFinStr}.csv";
            var rutaDescargas = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var rutaCompleta = Path.Combine(rutaDescargas, nombreArchivo);

            var sb = new StringBuilder();
            if (EsReporteHoras)
            {
                sb.AppendLine("Nombre Completo,Matricula,Rol,Total Asistencias,Total Entradas,Total Salidas,Horas Acumuladas");
                foreach (var item in ResultadosPaginadosHoras)
                {
                    sb.AppendLine($"\"{item.NombreCompleto}\",\"{item.Matricula}\",\"{item.Rol}\",{item.TotalAsistencias},{item.TotalEntradas},{item.TotalSalidas},{item.HorasAcumuladas}");
                }
            }
            else
            {
                sb.AppendLine("Matricula,Nombre,Apellido Paterno,Apellido Materno,Fecha,Hora Entrada,Hora Salida,Horas Diarias,Clasificacion,Horas Trabajadas");
                foreach (var item in ResultadosPaginadosUsuarios)
                {
                    sb.AppendLine($"\"{item.Matricula}\",\"{item.Nombre}\",\"{item.ApellidoPaterno}\",\"{item.ApellidoMaterno}\",{item.Fecha:yyyy-MM-dd},\"{item.HoraEntrada}\",\"{item.HoraSalida}\",{item.HorasDiarias},\"{item.ClasificacionHoras}\",{item.HorasTrabajadas}");
                }
            }

            await File.WriteAllTextAsync(rutaCompleta, sb.ToString(), Encoding.UTF8);

            Mensaje = $"Reporte exportado exitosamente a: {rutaCompleta}";
            EsError = false;
            MostrarMensaje = true;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al exportar a Excel: {ex.Message}";
            EsError = true;
            MostrarMensaje = true;
        }
    }
}
