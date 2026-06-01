using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
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
    public string RutaImagen { get; set; } = string.Empty;
}

public class ReporteDetalleItem
{
    public string Matricula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string HoraEntrada { get; set; } = string.Empty;
    public string HoraSalida { get; set; } = string.Empty;
    public double HorasDiarias { get; set; } = 0;
    public string ClasificacionHoras { get; set; } = string.Empty;
    public double HorasTrabajadas { get; set; } = 0;
    public string NombreCompleto => $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim();
}

public class ReportesService
{
    private readonly string _connectionString;

    public ReportesService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Obtiene una conexion abierta para usar con Dapper.
    /// </summary>
    private async Task<MySqlConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Reporte de HORAS (acumulado por persona).
    /// Basado en query del usuario:
    /// SELECT p.Matricula, p.Nombre, p.Apellido_paterno, p.Apellido_materno,
    ///        COUNT(a.id_asistencia) AS Asistencias_totales,
    ///        SUM(ROUND(TIMESTAMPDIFF(MINUTE, a.hora_entrada, a.hora_salida) / 60.0, 2)) AS Horas_totales
    /// FROM personas p JOIN asistencias_checador a ON p.id_persona = a.id_persona
    /// WHERE ... GROUP BY p.id_persona, p.Matricula, p.Nombre, p.Apellido_paterno, p.Apellido_materno
    /// </summary>
    public async Task<(List<ReporteItem> Reporte, bool SeEncontraronRegistros)> GenerarReporteAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        TipoPersona? tipoPersona = null,
        List<int>? idsPersonas = null)
    {
        var sql = @"
            SELECT 
                p.matricula,
                p.nombre,
                p.apellido_paterno,
                p.apellido_materno,
                p.tipo_persona,
                p.imagen,
                COUNT(a.id_asistencia) AS asistencias_totales,
                SUM(ROUND(TIMESTAMPDIFF(MINUTE, a.hora_entrada, a.hora_salida) / 60.0, 2)) AS horas_totales
            FROM personas p
            INNER JOIN asistencias_checador a ON p.id_persona = a.id_persona
            WHERE DATE(a.hora_entrada) >= @fechaInicio AND DATE(a.hora_entrada) <= @fechaFin
            AND a.hora_salida IS NOT NULL";

        var parameters = new DynamicParameters();
        parameters.Add("fechaInicio", fechaInicio.Date, DbType.Date);
        parameters.Add("fechaFin", fechaFin.Date, DbType.Date);

        if (tipoPersona.HasValue)
        {
            sql += " AND p.tipo_persona = @tipoPersona";
            parameters.Add("tipoPersona", tipoPersona.Value.ToString(), DbType.String);
        }

        if (idsPersonas != null && idsPersonas.Any())
        {
            sql += " AND p.id_persona IN @idsPersonas";
            parameters.Add("idsPersonas", idsPersonas);
        }

        sql += @" GROUP BY p.id_persona, p.matricula, p.nombre, p.apellido_paterno, p.apellido_materno, p.tipo_persona, p.imagen
                 ORDER BY p.matricula";

        using var connection = await GetConnectionAsync();
        var resultados = (await connection.QueryAsync<ReporteHorasDto>(sql, parameters)).ToList();

        if (resultados == null || !resultados.Any())
        {
            return (new List<ReporteItem>(), false);
        }

        var reporte = resultados.Select(r =>
        {
            // El conteo de asistencias del query ya cuenta cada par entrada/salida.
            // El usuario dice que asistencias esta en 0 porque cuenta pares.
            // En este query, asistencias_totales = COUNT(id_asistencia) que cuenta CADA registro.
            // Como cada asistencia es un par (entrada + salida), asistencias reales = asistencias_totales / 2
            var asistencias = r.asistencias_totales / 2;

            return new ReporteItem
            {
                NombreCompleto = $"{r.nombre ?? ""} {r.apellido_paterno ?? ""} {r.apellido_materno ?? ""}".Trim(),
                Matricula = r.matricula ?? string.Empty,
                Rol = r.tipo_persona ?? "Sin especificar",
                TotalAsistencias = asistencias,
                TotalEntradas = asistencias,  // O usar el conteo original si se desea
                TotalSalidas = asistencias,
                HorasAcumuladas = Math.Round(r.horas_totales ?? 0, 2),
                RutaImagen = r.imagen ?? string.Empty
            };
        }).ToList();

        return (reporte, reporte.Any());
    }

    /// <summary>
    /// Reporte de USUARIOS (detalle por dia).
    /// Basado en query del usuario:
    /// SELECT p.Matricula, p.Nombre, p.Apellido_paterno, p.Apellido_materno,
    ///        DATE(a.hora_entrada) AS Fecha,
    ///        TIME(a.hora_entrada) AS Hora_entrada,
    ///        TIME(a.hora_salida) AS Hora_salida,
    ///        ROUND(TIMESTAMPDIFF(MINUTE, a.hora_entrada, a.hora_salida) / 60.0, 2) AS Horas_trabajadas
    /// FROM personas p JOIN asistencias_checador a ON p.id_persona = a.id_persona
    /// WHERE p.tipo_persona = ... AND a.hora_entrada BETWEEN ... AND ...
    /// AND (a.hora_salida IS NOT NULL OR estatus <> 'cerrado')
    /// ORDER BY p.Matricula, a.hora_entrada
    /// </summary>
    public async Task<(List<ReporteDetalleItem> Reporte, bool SeEncontraronRegistros)> GenerarReporteUsuariosAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        TipoPersona? tipoPersona = null,
        List<int>? idsPersonas = null)
    {
        var sql = @"
            SELECT 
                p.matricula,
                p.nombre,
                p.apellido_paterno,
                p.apellido_materno,
                p.tipo_persona,
                DATE(a.hora_entrada) AS fecha,
                TIME(a.hora_entrada) AS hora_entrada,
                TIME(a.hora_salida) AS hora_salida,
                ROUND(TIMESTAMPDIFF(MINUTE, a.hora_entrada, a.hora_salida) / 60.0, 2) AS horas_trabajadas
            FROM personas p
            INNER JOIN asistencias_checador a ON p.id_persona = a.id_persona
            WHERE DATE(a.hora_entrada) >= @fechaInicio AND DATE(a.hora_entrada) <= @fechaFin
            AND a.hora_salida IS NOT NULL";

        var parameters = new DynamicParameters();
        parameters.Add("fechaInicio", fechaInicio.Date, DbType.Date);
        parameters.Add("fechaFin", fechaFin.Date, DbType.Date);

        if (tipoPersona.HasValue)
        {
            sql += " AND p.tipo_persona = @tipoPersona";
            parameters.Add("tipoPersona", tipoPersona.Value.ToString(), DbType.String);
        }

        if (idsPersonas != null && idsPersonas.Any())
        {
            sql += " AND p.id_persona IN @idsPersonas";
            parameters.Add("idsPersonas", idsPersonas);
        }

        sql += " ORDER BY p.matricula, a.hora_entrada";

        using var connection = await GetConnectionAsync();
        var resultados = (await connection.QueryAsync<ReporteUsuarioDto>(sql, parameters)).ToList();

        if (resultados == null || !resultados.Any())
        {
            return (new List<ReporteDetalleItem>(), false);
        }

        var reporte = resultados.Select(r =>
        {
            double horasDiarias = r.horas_trabajadas ?? 0;
            // Clasificacion basada en horas del dia
            string clasificacion = horasDiarias switch
            {
                < 4 => "Insuficiente",
                < 8 => "Parcial",
                < 10 => "Completa",
                _ => "Extra"
            };

            return new ReporteDetalleItem
            {
                Matricula = r.matricula ?? string.Empty,
                Nombre = r.nombre ?? string.Empty,
                ApellidoPaterno = r.apellido_paterno ?? string.Empty,
                ApellidoMaterno = r.apellido_materno ?? string.Empty,
                Rol = r.tipo_persona ?? string.Empty,
                Fecha = r.fecha,
                HoraEntrada = r.hora_entrada?.ToString() ?? string.Empty,
                HoraSalida = r.hora_salida?.ToString() ?? string.Empty,
                HorasDiarias = horasDiarias,
                ClasificacionHoras = clasificacion,
                HorasTrabajadas = horasDiarias
            };
        }).ToList();

        return (reporte, reporte.Any());
    }
}

// DTOs para el reporte de horas (acumulado)
public class ReporteHorasDto
{
    public string? matricula { get; set; }
    public string? nombre { get; set; }
    public string? apellido_paterno { get; set; }
    public string? apellido_materno { get; set; }
    public string? tipo_persona { get; set; }
    public string? imagen { get; set; }
    public int asistencias_totales { get; set; }
    public double? horas_totales { get; set; }
}

// DTO para el reporte de usuarios (detalle por dia)
public class ReporteUsuarioDto
{
    public string? matricula { get; set; }
    public string? nombre { get; set; }
    public string? apellido_paterno { get; set; }
    public string? apellido_materno { get; set; }
    public string? tipo_persona { get; set; }
    public DateTime fecha { get; set; }
    public TimeSpan? hora_entrada { get; set; }
    public TimeSpan? hora_salida { get; set; }
    public double? horas_trabajadas { get; set; }
}