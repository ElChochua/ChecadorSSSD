using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class ReportesService
{
    private readonly AppDbContext _context;

    public ReportesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReporteItem>> GenerarReporteAsync(DateTime fechaInicio, DateTime fechaFin, TipoPersona? TipoPersona = null)
    {
        var query = _context.Checadores
            .Include(c => c.Persona)
            .Where(c => c.Fecha >= fechaInicio.Date && c.Fecha <= fechaFin.Date)
            .AsQueryable();

        if (TipoPersona.HasValue)
        {
            query = query.Where(c => c.Persona.TipoPersona == TipoPersona.Value);
        }

        var registros = await query.ToListAsync();

        var reporte = registros
            .GroupBy(c => c.PersonaId)
            .Select(g =>
            {
                var persona = g.First().Persona;
                var entradas = g.Where(c => c.TipoAccion == TipoAccion.Entrada).OrderBy(c => c.Fecha).ThenBy(c => c.Hora).ToList();
                var salidas = g.Where(c => c.TipoAccion == TipoAccion.Salida).OrderBy(c => c.Fecha).ThenBy(c => c.Hora).ToList();

                double horasTotales = 0;

                // Calcular horas acumuladas emparejando entradas con salidas
                for (int i = 0; i < Math.Min(entradas.Count, salidas.Count); i++)
                {
                    var entrada = entradas[i];
                    var salida = salidas[i];

                    var fechaHoraEntrada = entrada.Fecha.Date + entrada.Hora;
                    var fechaHoraSalida = salida.Fecha.Date + salida.Hora;

                    if (fechaHoraSalida > fechaHoraEntrada)
                    {
                        horasTotales += (fechaHoraSalida - fechaHoraEntrada).TotalHours;
                    }
                }

                return new ReporteItem
                {
                    NombreCompleto = $"{persona.Nombre} {persona.Apellido}",
                    Matricula = persona.Matricula,
                    Rol = persona.TipoPersona.ToString(),
                    TotalAsistencias = entradas.Count + salidas.Count,
                    TotalEntradas = entradas.Count,
                    TotalSalidas = salidas.Count,
                    HorasAcumuladas = Math.Round(horasTotales, 2)
                };
            })
            .OrderBy(r => r.NombreCompleto)
            .ToList();

        return reporte;
    }
}
