using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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
    public string RutaImagen { get; set; } = string.Empty;
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
    public double HorasDiarias { get; set; } = 0;
    public string ClasificacionHoras { get; set; } = string.Empty;
    public double HorasTrabajadas { get; set; } = 0;
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
                a.hora_entrada,
                a.hora_salida
            FROM asistencias_checador a
            INNER JOIN personas p ON a.id_persona = p.id_persona
            WHERE DATE(a.hora_entrada) >= @fechaInicio AND DATE(a.hora_entrada) <= @fechaFin";

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

        List<AsistenciaReporteDto> registros;
        using (var connection = await GetConnectionAsync())
        {
            registros = (await connection.QueryAsync<AsistenciaReporteDto>(sql, parameters)).ToList();
        }

        if (registros == null || !registros.Any())
        {
            return (new List<ReporteItem>(), false);
        }

        var reporte = registros
            .GroupBy(c => c.Matricula)
            .Select(g =>
            {
                var primerRegistro = g.FirstOrDefault();
                if (primerRegistro == null) return null!;

                double horasTotales = 0;
                int parejasValidas = 0;
                int totalEntradas = g.Count();
                int totalSalidas = g.Count(x => x.HoraSalida != null || x.HoraSalida != DateTime.MinValue);

                foreach (var registro in g)
                {
                    if (registro.HoraSalida != null && registro.HoraSalida != DateTime.MinValue)
                    {
                        var dif = registro.HoraSalida.Value - registro.HoraEntrada;
                        if (dif.TotalHours > 0)
                        {
                            horasTotales += dif.TotalHours;
                            parejasValidas++;
                        }
                    }
                }

                return new ReporteItem
                {
                    NombreCompleto = $"{primerRegistro?.Nombre ?? ""} {primerRegistro?.Apellido_paterno ?? ""} {primerRegistro?.Apellido_materno ?? ""}".Trim(),
                    Matricula = g.Key,
                    Rol = primerRegistro.Tipo_persona ?? "Sin especificar",
                    TotalAsistencias = parejasValidas,
                    TotalEntradas = totalEntradas,
                    TotalSalidas = totalSalidas,
                    HorasAcumuladas = Math.Round(horasTotales, 2),
                    RutaImagen = primerRegistro.Imagen ?? string.Empty
                };
            })
            .Where(r => r != null)
            .OrderBy(r => r.NombreCompleto)
            .ToList();

        return (reporte, reporte.Any());
    }

    /// <summary>
    /// Reporte de USUARIOS (detalle por d�a).
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
                a.hora_entrada,
                a.hora_salida,
                TIMESTAMPDIFF(SECOND, a.hora_entrada, a.hora_salida) / 3600.0 as horas
            FROM asistencias_checador a
            INNER JOIN personas p ON a.id_persona = p.id_persona
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

        List<ReporteDetalleDto> resultados;
        using (var connection = await GetConnectionAsync())
        {
            resultados = (await connection.QueryAsync<ReporteDetalleDto>(sql, parameters)).ToList();
        }

        var reporte = resultados.Select(r =>
        {
            int horasDiarias = (int)Math.Floor(r.horas ?? 0);
            string clasificacion = horasDiarias switch
            {
                < 10 => "Unidades",
                < 100 => "Decenas",
                < 1000 => "Centenas",
                _ => "Mas de mil horas"
            };

            return new ReporteDetalleItem
            {
                Matricula = r.Matricula ?? string.Empty,
                Nombre = r.Nombre ?? string.Empty,
                ApellidoPaterno = r.Apellido_paterno ?? string.Empty,
                ApellidoMaterno = r.Apellido_materno ?? string.Empty,
                Fecha = r.Hora_entrada.Date,
                HoraEntrada = r.Hora_entrada.ToString("HH:mm:ss"),
                HoraSalida = r.Hora_salida?.ToString("HH:mm:ss") ?? string.Empty,
                HorasDiarias = horasDiarias,
                ClasificacionHoras = clasificacion,
                HorasTrabajadas = horasDiarias
            };
        }).ToList();

        return (reporte, reporte.Any());
    }
}

public class AsistenciaReporteDto
{
    public string? Matricula { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido_paterno { get; set; }
    public string? Apellido_materno { get; set; }
    public string? Tipo_persona { get; set; }
    public string? Imagen { get; set; }
    public DateTime HoraEntrada { get; set; }
    public DateTime? HoraSalida { get; set; }
}

public class ReporteDetalleDto
{
    public string? Matricula { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido_paterno { get; set; }
    public string? Apellido_materno { get; set; }
    public DateTime Hora_entrada { get; set; }
    public DateTime? Hora_salida { get; set; }
    public double? horas { get; set; }
}
