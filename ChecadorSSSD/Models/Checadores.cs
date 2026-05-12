using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChecadorSSSD.Models;

public enum TipoAccion
{
    Entrada,
    Salida
}

public class Checadores
{
    [Key]
    public int Id { get; set; }

    [Required]
    public TipoAccion TipoAccion { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    public TimeSpan Hora { get; set; }

    [Required]
    public int PersonaId { get; set; }

    [ForeignKey("PersonaId")]
    public Personas Persona { get; set; } = null!;
}
