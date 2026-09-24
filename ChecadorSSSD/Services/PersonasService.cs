using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChecadorSSSD.Data;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Services;

public class PersonasService
{
    private readonly AppDbContext _context;

    public PersonasService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Personas>> ObtenerTodasAsync()
    {
        return await _context.Personas.AsNoTracking().ToListAsync();
    }

    public async Task<Personas?> ObtenerPorIdAsync(int id)
    {
        return await _context.Personas.FindAsync(id);
    }

    public async Task<Personas?> ObtenerPorMatriculaAsync(string matricula)
    {
        return await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Matricula == matricula);
    }

    public async Task<Personas> CrearAsync(Personas persona)
    {
        _context.Personas.Add(persona);
        await _context.SaveChangesAsync();
        return persona;
    }

    public async Task<Personas> ActualizarAsync(Personas persona)
    {
        _context.Personas.Update(persona);
        await _context.SaveChangesAsync();
        return persona;
    }

    public async Task EliminarAsync(int id)
    {
        var persona = await _context.Personas.FindAsync(id);
        if (persona != null)
        {
            _context.Personas.Remove(persona);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Personas>> ObtenerPersonasConHuellaAsync()
    {
        return await _context.Personas
            .AsNoTracking()
            .Where(p => p.Huella != null && p.Huella.Length > 0)
            .ToListAsync();
    }
}
