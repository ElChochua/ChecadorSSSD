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
    /// Marca la entrada o salida de una persona bas�ndose en su n�mero de matr�cula.
    /// Implementa debouncing y l�gica de turnos de 16 horas.
    /// </summary>
    public async Task<(string tipo, string mensaje)> MarcarAsistenciaAsync(string matricula)
    {
        // 1. Validar que la matr�cula no est� vac�a
        if (string.IsNullOrWhiteSpace(matricula))
        {
            return ("error", "La matr�cula no puede estar vac�a.");
        }

        // 2. Buscar la persona por matr�cula
        var persona = await _context.Personas
            .FirstOrDefaultAsync(p => p.Matricula == matricula.Trim());

        if (persona == null)
        {
            return ("error", $"Persona con matr�cula {matricula.Trim()} no encontrada.");
        }

        var ahora = DateTime.Now;

        // 3. Buscar asistencia abierta m�s reciente (sin hora_salida)
        var asistenciaAbierta = await _context.AsistenciasChecador
            .Where(a => a.IdPersona == persona.IdPersona && a.HoraSalida == null)
            .OrderByDescending(a => a.HoraEntrada)
            .FirstOrDefaultAsync();

        // 4. Si existe una asistencia abierta
        if (asistenciaAbierta != null)
        {
            var tiempoTranscurrido = ahora - asistenciaAbierta.HoraEntrada;

            // 5. Si el turno est� abierto desde hace m�s de 16 horas
            if (tiempoTranscurrido.TotalHours > 16)
            {
                // Cerrar la asistencia anterior con estatus 'cerrado'
                asistenciaAbierta.Estatus = "cerrado";
                _context.AsistenciasChecador.Update(asistenciaAbierta);
                await _context.SaveChangesAsync();

                // Crear nueva entrada
                var nuevaAsistencia = new AsistenciaChecador
                {
                    IdPersona = persona.IdPersona,
                    HoraEntrada = ahora,
                    HoraSalida = null,
                    Estatus = null
                };
                _context.AsistenciasChecador.Add(nuevaAsistencia);
                await _context.SaveChangesAsync();

                return ("nueva", $"Entrada anterior no cerrada. Entrada nueva agregada para {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}.");
            }

            // 6. Verificar debouncing (m�s de 10 minutos desde la entrada)
            if (tiempoTranscurrido.TotalMinutes < 10)
            {
                return ("info", "Entrada ya registrada. Espere a marcar la salida.");
            }

            // 7. Registrar salida
            asistenciaAbierta.HoraSalida = ahora;
            asistenciaAbierta.Estatus = "cerrado";
            _context.AsistenciasChecador.Update(asistenciaAbierta);
            await _context.SaveChangesAsync();

            return ("salida", $"�Hasta luego {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}! Salida registrada.");
        }

        // 8. No existe asistencia abierta - crear nueva entrada
        var asistenciaEntrada = new AsistenciaChecador
        {
            IdPersona = persona.IdPersona,
            HoraEntrada = ahora,
            HoraSalida = null,
            Estatus = null
        };

        _context.AsistenciasChecador.Add(asistenciaEntrada);
        await _context.SaveChangesAsync();

        return ("entrada", $"�Bienvenido/a {persona.Nombre} {persona.ApellidoPaterno} {persona.ApellidoMaterno}! Entrada registrada.");
    }
}
