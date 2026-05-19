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
    /// Marca la entrada o salida de una persona basándose en su número de matrícula.
    /// Si la última acción fue 'Entrada', marca 'Salida'.
    /// Si la última acción fue 'Salida' o no hay registros, marca 'Entrada'.
    /// Además, lanza un mensaje de error correspondiente si se detecta que la matrícula
    /// está vacía o la persona no existe en la base de datos.
    /// </summary>
    public async Task<(TipoAccion accion, string mensaje)> MarcarEntradaSalidaAsync(string matricula)
    {
        // 1. Validar que la matrícula no esté vacía
        if (string.IsNullOrWhiteSpace(matricula))
        {
            return (TipoAccion.Entrada, "La matrícula no puede estar vacía.");
        }

        // 2. Buscar la persona por matrícula
        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Matricula == matricula.Trim());

        if (persona == null)
        {
            return (TipoAccion.Entrada, $"Persona con matrícula {matricula.Trim()} no encontrada.");
        }

        var ahora = DateTime.Now;

        // 3. Determinar tipo de acción basado en el último registro de la persona
        // Traer a memoria primero para usar c.Hora (NotMapped) sin problemas de traducción SQL
        var ultimaAccion = (await _context.Checadores
            .Where(c => c.Matricula == matricula.Trim())
            .ToListAsync())
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Hora)
            .FirstOrDefault();

        TipoAccion tipoAccion;
        string mensaje;

        if (ultimaAccion == null || ultimaAccion.EsSalida)
        {
            tipoAccion = TipoAccion.Entrada;
            mensaje = $"¡Bienvenido/a {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}! Entrada registrada.";
        }
        else
        {
            tipoAccion = TipoAccion.Salida;
            mensaje = $"¡Hasta luego {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}! Salida registrada.";
        }

        // 4. Crear y guardar el registro en Checadores con los datos de la persona
        var checador = new Checadores
        {
            TipoAccion = tipoAccion.ToString(),
            Fecha = ahora.Date,
            Hora = ahora.TimeOfDay,
            Matricula = persona.Matricula,
            Nombre = persona.Nombre,
            ApellidoPaterno = persona.ApellidoPaterno,
            ApellidoMaterno = persona.ApellidoMaterno,
            TipoPersona = persona.TipoPersona
        };

        _context.Checadores.Add(checador);
        await _context.SaveChangesAsync();

        return (tipoAccion, mensaje);
    }

    public async Task<string?> RegistrarEntradaForzadaAsync(string matricula)
    {
        if (string.IsNullOrWhiteSpace(matricula))
        {
            return "La matrícula no puede estar vacía.";
        }

        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Matricula == matricula.Trim());

        if (persona == null)
        {
            return $"Persona con matrícula {matricula.Trim()} no encontrada.";
        }

        var ahora = DateTime.Now;

        var checador = new Checadores
        {
            TipoAccion = TipoAccion.Entrada.ToString(),
            Fecha = ahora.Date,
            Hora = ahora.TimeOfDay,
            Matricula = persona.Matricula,
            Nombre = persona.Nombre,
            ApellidoPaterno = persona.ApellidoPaterno,
            ApellidoMaterno = persona.ApellidoMaterno,
            TipoPersona = persona.TipoPersona
        };

        _context.Checadores.Add(checador);
        await _context.SaveChangesAsync();

        return $"Entrada registrada para {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}.";
    }

    public async Task<string?> RegistrarSalidaForzadaAsync(string matricula)
    {
        if (string.IsNullOrWhiteSpace(matricula))
        {
            return "La matrícula no puede estar vacía.";
        }

        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Matricula == matricula.Trim());

        if (persona == null)
        {
            return $"Persona con matrícula {matricula.Trim()} no encontrada.";
        }

        var ahora = DateTime.Now;

        var checador = new Checadores
        {
            TipoAccion = TipoAccion.Salida.ToString(),
            Fecha = ahora.Date,
            Hora = ahora.TimeOfDay,
            Matricula = persona.Matricula,
            Nombre = persona.Nombre,
            ApellidoPaterno = persona.ApellidoPaterno,
            ApellidoMaterno = persona.ApellidoMaterno,
            TipoPersona = persona.TipoPersona
        };

        _context.Checadores.Add(checador);
        await _context.SaveChangesAsync();

        return $"Salida registrada para {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}.";
    }
}
