using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using ChecadorSSSD.Data;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Services;

public class ReporteItem
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int TotalAsistencias { get; set; }
    public int TotalEntradas { get; set; }
    public int TotalSalidas { get; set; }
    public double HorasAcumuladas { get; set; }
}

public class ReporteDetalleItem
{
    public string Matricula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string HoraEntrada { get; set; } = string.Empty;
    public string HoraSalida { get; set; } = string.Empty;
    public int HorasDiarias { get; set; } = 0;
    public string ClasificacionHoras { get; set; } = string.Empty;
    public int HorasTrabajadas { get; set; } = 0;
}

public class ReportesService
{
    private readonly AppDbContext _context;

    public ReportesService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene la cadena de conexion configurada en el AppDbContext.
    /// </summary>
    public string GetConnectionString()
    {
        // El _context.Database.GetDbConnection().ConnectionString obtiene la cadena de conexion 
        // que EF Core utiliza.
        return _context.Database.GetDbConnection().ConnectionString;
    }

    /// <summary>
    /// Parsea un string de hora (VARCHAR) a TimeSpan de forma robusta.
    /// </summary>
    public static TimeSpan ParseHora(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return TimeSpan.Zero;
        var cleaned = raw.Trim().Trim('"').Trim('\'').Replace(';', ':').Replace('.', ':');
        if (TimeSpan.TryParseExact(cleaned, @"hh\:mm\:ss", null, out var t1)) return t1;
        if (TimeSpan.TryParseExact(cleaned, @"h\:mm\:ss", null, out var t2)) return t2;
        if (TimeSpan.TryParse(cleaned, out var t3)) return t3;
        return TimeSpan.Zero;
    }

    /// <summary>
    /// Reporte de HORAS (acumulado por persona):
    /// Nombre Completo, Matricula, Rol, Total Asistencias, Total Entradas, Total Salidas, Horas Acumuladas.
    /// </summary>
    public async Task<(List<ReporteItem> Reporte, bool SeEncontraronRegistros)> GenerarReporteAsync(DateTime fechaInicio, DateTime fechaFin, TipoPersona? tipoPersona = null)
    {
        var connection = _context.Database.GetDbConnection();
        
        var sql = @"
            SELECT 
                Matricula,
                Nombre,
                Apellido_paterno,
                Apellido_materno,
                Tipo_persona,
                Fecha,
                Hora,
                Tipo_accion
            FROM checador
            WHERE Fecha >= @fechaInicio AND Fecha <= @fechaFin";

        var parameters = new DynamicParameters();
        parameters.Add("fechaInicio", fechaInicio.Date, DbType.Date);
        parameters.Add("fechaFin", fechaFin.Date, DbType.Date);

        // Si se selecciono un tipo de persona especifico, agregar filtro
        if (tipoPersona.HasValue)
        {
            sql += " AND Tipo_persona = @tipoPersona";
            parameters.Add("tipoPersona", tipoPersona.Value.ToString(), DbType.String);
        }

        sql += " ORDER BY Matricula, Fecha, Hora";

        // Usar Dapper para obtener los registros sin materializar la entidad completa (evita DBNull)
        var registros = new List<ChecadorDto>();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        
        var results = await connection.QueryAsync<ChecadorDto>(sql, parameters);
        registros = results.ToList();

        if (registros == null || !registros.Any())
        {
            return (new List<ReporteItem>(), false);
        }

        // Agrupar por Matricula y calcular metricas
        var reporte = registros
            .GroupBy(c => c.Matricula)
            .Select(g =>
            {
                var primerRegistro = g.FirstOrDefault();
                if (primerRegistro == null) return null!;

                var registrados = g.ToList();
                var registrosOrdenados = registrados.OrderBy(c => c.Fecha).ThenBy(c => ParseHora(c.Hora)).ToList();

                double horasTotales = 0;
                int parejasValidas = 0;
                for (int i = 0; i < registrosOrdenados.Count; i++)
                {
                    var registro = registrosOrdenados[i];
                    var isEntrada = string.Equals(registro.Tipo_accion, "Entrada", StringComparison.OrdinalIgnoreCase);
                    
                    if (isEntrada && i + 1 < registrosOrdenados.Count)
                    {
                        var siguiente = registrosOrdenados[i + 1];
                        var isSalida = string.Equals(siguiente.Tipo_accion, "Salida", StringComparison.OrdinalIgnoreCase);
                        
                        if (isSalida)
                        {
                            var entradaHora = registro.Fecha.Date + ParseHora(registro.Hora);
                            var salidaHora = siguiente.Fecha.Date + ParseHora(siguiente.Hora);

                            if (salidaHora >= entradaHora)
                            {
                                horasTotales += (salidaHora - entradaHora).TotalHours;
                                parejasValidas++;
                            }
                            i++;
                        }
                    }
                }

                var totalEntradas = g.Count(c => string.Equals(c.Tipo_accion, "Entrada", StringComparison.OrdinalIgnoreCase));
                var totalSalidas = g.Count(c => string.Equals(c.Tipo_accion, "Salida", StringComparison.OrdinalIgnoreCase));
                var tipoPersonaStr = string.IsNullOrWhiteSpace(primerRegistro.Tipo_persona)
                    ? "Sin especificar"
                    : primerRegistro.Tipo_persona;

                return new ReporteItem
                {
                    NombreCompleto = $"{primerRegistro.Nombre} {primerRegistro.Apellido_paterno} {primerRegistro.Apellido_materno}".Trim(),
                    Matricula = g.Key,
                    Rol = tipoPersonaStr ?? "Sin especificar",
                    TotalAsistencias = parejasValidas,
                    TotalEntradas = totalEntradas,
                    TotalSalidas = totalSalidas,
                    HorasAcumuladas = Math.Round(horasTotales, 2)
                };
            })
            .Where(r => r != null)
            .OrderBy(r => r.NombreCompleto)
            .ToList();

        return (reporte, reporte.Any());
    }

    /// <summary>
    /// Reporte de USUARIOS (detalle por dia):
    /// Matricula, Nombre, Apellido_paterno, Apellido_materno, Fecha, hora_entrada, hora_salida, horas_diarias, clasificacion_horas, horas_trabajadas.
    /// Basado directamente en la query SQL proporcionada.
    /// </summary>
    public async Task<(List<ReporteDetalleItem> Reporte, bool SeEncontraronRegistros)> GenerarReporteUsuariosAsync(DateTime fechaInicio, DateTime fechaFin, TipoPersona? tipoPersona = null)
    {
        var connection = _context.Database.GetDbConnection();

        var sql = @"
            SELECT 
                a.Matricula,
                a.Nombre,
                a.Apellido_paterno,
                a.Apellido_materno,
                a.Fecha,
                MIN(a.Hora) AS hora_entrada,
                MAX(b.Hora) AS hora_salida,
                FLOOR(SUM(TIME_TO_SEC(TIMEDIFF(b.Hora, a.Hora))) / 3600) AS horas_diarias,
                CASE
                    WHEN FLOOR(SUM(TIME_TO_SEC(TIMEDIFF(b.Hora, a.Hora))) / 3600) < 10 THEN 'Unidades'
                    WHEN FLOOR(SUM(TIME_TO_SEC(TIMEDIFF(b.Hora, a.Hora))) / 3600) BETWEEN 10 AND 99 THEN 'Decenas'
                    WHEN FLOOR(SUM(TIME_TO_SEC(TIMEDIFF(b.Hora, a.Hora))) / 3600) BETWEEN 100 AND 999 THEN 'Centenas'
                    ELSE 'Mas de mil horas'
                END AS clasificacion_horas,
                FLOOR(SUM(TIME_TO_SEC(TIMEDIFF(b.Hora, a.Hora))) / 3600) AS horas_trabajadas
            FROM 
                checador a
            JOIN 
                checador b ON a.Matricula = b.Matricula 
                             AND a.Fecha = b.Fecha
                             AND a.Tipo_accion = 'Entrada'
                             AND b.Tipo_accion = 'Salida'
            WHERE 
                a.Fecha >= @fechaInicio AND a.Fecha <= @fechaFin";

        var parameters = new DynamicParameters();
        parameters.Add("fechaInicio", fechaInicio.Date, DbType.Date);
        parameters.Add("fechaFin", fechaFin.Date, DbType.Date);

        if (tipoPersona.HasValue)
        {
            sql += " AND a.Tipo_persona = @tipoPersona";
            parameters.Add("tipoPersona", tipoPersona.Value.ToString(), DbType.String);
        }

        sql += @"
            GROUP BY 
                a.Matricula, a.Nombre, a.Apellido_paterno, a.Apellido_materno, a.Fecha
            ORDER BY 
                a.Matricula, a.Fecha";

        List<ReporteDetalleDto> resultados;
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var query = await connection.QueryAsync<ReporteDetalleDto>(sql, parameters);
        resultados = query.ToList();

        // Mapear resultados de DTO a la clase del modelo de vista
        var reporte = resultados.Select(r => new ReporteDetalleItem
        {
            Matricula = r.Matricula ?? string.Empty,
            Nombre = r.Nombre ?? string.Empty,
            ApellidoPaterno = r.Apellido_paterno ?? string.Empty,
            ApellidoMaterno = r.Apellido_materno ?? string.Empty,
            Fecha = r.Fecha,
            // La hora de Dapper ya viene como string VARCHAR, la dejamos como esta o formateamos
            HoraEntrada = r.hora_entrada ?? string.Empty,
            HoraSalida = r.hora_salida ?? string.Empty,
            HorasDiarias = r.horas_diarias,
            ClasificacionHoras = r.clasificacion_horas ?? string.Empty,
            HorasTrabajadas = r.horas_trabajadas
        }).ToList();

        return (reporte, reporte.Any());
    }
}

/// <summary>
/// DTO para mapear el resultado de la query SQL de horas usando Dapper.
/// Evita usar la entidad Checadores para prevenir errores de DBNull.
/// </summary>
public class ChecadorDto
{
    public string? Matricula { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido_paterno { get; set; }
    public string? Apellido_materno { get; set; }
    public string? Tipo_persona { get; set; }
    public DateTime Fecha { get; set; }
    public string? Hora { get; set; }
    public string? Tipo_accion { get; set; }
}

/// <summary>
/// DTO para mapear el resultado del reporte de usuarios usando Dapper.
/// </summary>
public class ReporteDetalleDto
{
    public string? Matricula { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido_paterno { get; set; }
    public string? Apellido_materno { get; set; }
    public DateTime Fecha { get; set; }
    public string? hora_entrada { get; set; }
    public string? hora_salida { get; set; }
    public int horas_diarias { get; set; }
    public string? clasificacion_horas { get; set; }
    public int horas_trabajadas { get; set; }
}
