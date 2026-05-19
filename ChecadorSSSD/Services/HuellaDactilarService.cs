using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChecadorSSSD.Data;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Services;

/// <summary>
/// Servicio para manejo de huella dactilar.
/// Se encuentra en STANDBY hasta que se integre un escáner de huellas.
/// </summary>
public class HuellaDactilarService
{
    private readonly AppDbContext _context;

    public HuellaDactilarService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Guarda los datos de una huella dactilar en formato de bytes.
    /// Este método queda en standby hasta que se integre un escáner de huellas.
    /// </summary>
    public async Task<string?> GuardarHuellaAsync(string matricula, byte[] huella)
    {
        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Matricula == matricula);

        if (persona == null)
        {
            return $"Persona con matrícula {matricula} no encontrada.";
        }

        if (huella == null || huella.Length == 0)
        {
            return "Los datos de la huella están vacíos.";
        }

        persona.Huella = huella;
        _context.Personas.Update(persona);
        await _context.SaveChangesAsync();

        return $"Huella dactilar guardada para {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}.";
    }

    /// <summary>
    /// Obtiene los datos de la huella dactilar de una persona.
    /// Este método queda en standby hasta que se integre un escáner de huellas.
    /// </summary>
    public async Task<byte[]?> ObtenerHuellaAsync(string matricula)
    {
        var persona = await _context.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Matricula == matricula);

        return persona?.Huella;
    }

    /// <summary>
    /// Verifica si una persona tiene huella dactilar registrada.
    /// Este método queda en standby hasta que se integre un escáner de huellas.
    /// </summary>
    public async Task<bool> TieneHuellaAsync(string matricula)
    {
        var huella = await ObtenerHuellaAsync(matricula);
        return huella != null && huella.Length > 0;
    }

    /// <summary>
    /// Placeholder para validación de huella dactilar.
    /// Este método queda en standby hasta que se integre un escáner de huellas.
    /// </summary>
    public async Task<(bool valida, string mensaje)> ValidarHuellaAsync(string matricula, byte[] huellaEscaneada)
    {
        // En un futuro, aquí se compararán las huellas biométricas
        // Por ahora, siempre retorna false indicando que la validación por huella no está disponible
        return await Task.FromResult((false, "Validación por huella dactilar no disponible. Utilice la matrícula para marcar entrada/salida."));
    }

    #region Standby — Handler de Huella Dactilar

    /// <summary>
    /// Handler en standby para procesar lectura de huella dactilar.
    /// Este método permanece inactive hasta que se integre un escáner de huellas.
    /// </summary>
    public async Task<string?> ProcesarHuellaAsync(byte[] datosHuella)
    {
        // STANDBY: Integración con escáner de huella pendiente
        // 1. Recibir datos del escáner
        // 2. Comparar con plantillas almacenadas
        // 3. Retornar resultado de la verificación
        return await Task.FromResult("Huella dactilar en standby - No hay escáner integrado");
    }

    /// <summary>
    /// Handler en standby para escuchar eventos del escáner de huella.
    /// Este método permanece inactive hasta que se integre el hardware.
    /// </summary>
    public void EscucharHuellaDactilar()
    {
        // STANDBY: Inicializar listener del escáner de huella
        // - Configurar puerto COM o interfaz USB
        // - Escuchar eventos de lectura de huella
        // - Procesar plantilla biométrica recibida
        // Por ahora no hace nada, está en standby
    }

    /// <summary>
    /// Verifica si el servicio de huella dactilar está disponible.
    /// </summary>
    public Task<bool> ServicioDisponibleAsync()
    {
        // STANDBY: Retorna false hasta que se integre un escáner
        return Task.FromResult(false);
    }

    #endregion
}
