using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChecadorSSSD.Models;

public enum TipoPersona
{
    Brigadista,
    Asesor,
    PersonalAdministrativo
}

public class Personas
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Matricula { get; set; } = string.Empty;

    [Required]
    public TipoPersona TipoPersona { get; set; }

    [MaxLength(500)]
    public string? RutaFoto { get; set; }

    public ICollection<Checadores> Registros { get; set; } = new List<Checadores>();
}
