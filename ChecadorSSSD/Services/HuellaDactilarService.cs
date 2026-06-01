using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ChecadorSSSD.Data;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Services;

/// <summary>
/// Servicio para manejo de huellas dactilares.
/// </summary>
public class HuellaDactilarService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public HuellaDactilarService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Obtiene la huella dactilar de una persona.
    /// </summary>
    public async Task<byte[]?> ObtenerHuellaAsync(string matricula)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persona = await context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Matricula == matricula);
        return persona?.Huella;
    }

    /// <summary>
    /// Verifica si una persona tiene huella dactilar registrada.
    /// </summary>
    public async Task<bool> TieneHuellaAsync(string matricula)
    {
        var huella = await ObtenerHuellaAsync(matricula);
        return huella != null && huella.Length > 0;
    }

    /// <summary>
    /// Guarda una huella dactilar para una persona. Si la persona no existe, devuelve error.
    /// </summary>
    public async Task<(bool exito, string mensaje)> GuardarHuellaAsync(string matricula, byte[] huella)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persona = await context.Personas
            .FirstOrDefaultAsync(p => p.Matricula == matricula);

        if (persona == null)
        {
            return (false, $"Persona con matrícula {matricula} no encontrada.");
        }

        if (huella == null || huella.Length == 0)
        {
            return (false, "Los datos de la huella están vacíos.");
        }

        try
        {
            persona.Huella = huella;
            context.Personas.Update(persona);
            await context.SaveChangesAsync();
            return (true, $"Huella dactilar guardada correctamente para {persona.NombreCompleto}.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al guardar la huella: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca las huellas de todas las personas para comparación.
    /// </summary>
    public async Task<List<(string matricula, byte[] huella)>> ObtenerTodasLasHuellasAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var personasConHuella = await context.Personas
            .AsNoTracking()
            .Where(p => p.Huella != null && p.Huella.Length > 0)
            .ToListAsync();

        return personasConHuella
            .Select(p => (p.Matricula, p.Huella!))
            .ToList();
    }
}
