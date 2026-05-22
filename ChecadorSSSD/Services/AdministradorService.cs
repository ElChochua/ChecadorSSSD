using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ChecadorSSSD.Data;
using ChecadorSSSD.Models;
using Microsoft.EntityFrameworkCore;

namespace ChecadorSSSD.Services;

public class AdministradorService
{
    private readonly AppDbContext _context;

    public AdministradorService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Genera el hash SHA256 de una contraseña.
    /// </summary>
    public static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    public async Task<List<Administrador>> ObtenerTodosAsync()
    {
        return await _context.Administradores.ToListAsync();
    }

    public async Task<Administrador?> ObtenerPorIdAsync(int id)
    {
        return await _context.Administradores.FindAsync(id);
    }

    public async Task<Administrador?> ObtenerPorNombreAsync(string nombre)
    {
        return await _context.Administradores
            .FirstOrDefaultAsync(a => a.Nombre.ToLower() == nombre.ToLower());
    }

    public async Task<Administrador?> ValidarCredencialesAsync(string nombre, string contrasenia)
    {
        var admin = await _context.Administradores
            .FirstOrDefaultAsync(a => a.Nombre.ToLower() == nombre.ToLower());

        if (admin == null) return null;

        var hashIngresado = HashPassword(contrasenia);

        // Si coincide el hash ya hasheado (login normal)
        if (admin.Contrasenia == hashIngresado)
        {
            return admin;
        }

        // Si no coincide, y la contraseña almacenada está en texto plano (migración legada):
        // Intentamos comparar en texto plano y si coincide, actualizamos a hash automáticamente
        if (admin.Contrasenia == contrasenia)
        {
            admin.Contrasenia = hashIngresado;
            _context.Administradores.Update(admin);
            await _context.SaveChangesAsync();
            return admin;
        }

        return null;
    }

    public async Task CrearAsync(Administrador admin)
    {
        admin.Contrasenia = HashPassword(admin.Contrasenia);
        await _context.Administradores.AddAsync(admin);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Administrador admin)
    {
        var existing = await _context.Administradores.FindAsync(admin.IdAdmin);
        if (existing == null) throw new Exception("Administrador no encontrado.");

        existing.Nombre = admin.Nombre;
        // Solo actualizar la contraseña si se proporcionó una nueva
        if (!string.IsNullOrEmpty(admin.Contrasenia))
        {
            existing.Contrasenia = HashPassword(admin.Contrasenia);
        }
        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        var admin = await _context.Administradores.FindAsync(id);
        if (admin != null)
        {
            _context.Administradores.Remove(admin);
            await _context.SaveChangesAsync();
        }
    }
}
