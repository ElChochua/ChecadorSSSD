using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChecadorSSSD.Data;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Services;

public class ChecadorService
{
    private readonly AppDbContext _context;

    public ChecadorService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Marca la entrada o salida de una persona basándose en su número de cuenta.
    /// Si la última acción fue 'Entrada', marca 'Salida'.
    /// Si la última acción fue 'Salida' o no hay registros, marca 'Entrada'.
    /// </summary>
    public async Task<(TipoAccion accion, string mensaje)> MarcarEntradaSalidaAsync(string Matricula)
    {
        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Matricula == Matricula);

        if (persona == null)
        {
            return (TipoAccion.Entrada, $"Persona con matrícula {Matricula} no encontrada.");
        }

        var ahora = DateTime.Now;

        // Buscar la última acción de esta persona
        var ultimaAccion = await _context.Checadores
            .Where(c => c.PersonaId == persona.Id)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Hora)
            .FirstOrDefaultAsync();

        TipoAccion tipoAccion;
        string mensaje;

        if (ultimaAccion == null || ultimaAccion.TipoAccion == TipoAccion.Salida)
        {
            tipoAccion = TipoAccion.Entrada;
            mensaje = $"¡Bienvenido/a {persona.Nombre} {persona.Apellido}! Entrada registrada.";
        }
        else
        {
            tipoAccion = TipoAccion.Salida;
            mensaje = $"¡Hasta luego {persona.Nombre} {persona.Apellido}! Salida registrada.";
        }

        var checador = new Checadores
        {
            TipoAccion = tipoAccion,
            Fecha = ahora.Date,
            Hora = ahora.TimeOfDay,
            PersonaId = persona.Id
        };

        _context.Checadores.Add(checador);
        await _context.SaveChangesAsync();

        return (tipoAccion, mensaje);
    }

    public async Task<string?> RegistrarEntradaForzadaAsync(string Matricula)
    {
        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Matricula == Matricula);

        if (persona == null)
        {
            return null;
        }

        var ahora = DateTime.Now;

        var checador = new Checadores
        {
            TipoAccion = TipoAccion.Entrada,
            Fecha = ahora.Date,
            Hora = ahora.TimeOfDay,
            PersonaId = persona.Id
        };

        _context.Checadores.Add(checador);
        await _context.SaveChangesAsync();

        return $"Entrada registrada para {persona.Nombre} {persona.Apellido}.";
    }

    public async Task<string?> RegistrarSalidaForzadaAsync(string Matricula)
    {
        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Matricula == Matricula);

        if (persona == null)
        {
            return null;
        }

        var ahora = DateTime.Now;

        var checador = new Checadores
        {
            TipoAccion = TipoAccion.Salida,
            Fecha = ahora.Date,
            Hora = ahora.TimeOfDay,
            PersonaId = persona.Id
        };

        _context.Checadores.Add(checador);
        await _context.SaveChangesAsync();

        return $"Salida registrada para {persona.Nombre} {persona.Apellido}.";
    }
}
