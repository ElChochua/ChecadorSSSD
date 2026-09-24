using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChecadorSSSD.Models;

public enum TipoPersona
{
    Brigadista,
    Asesor,
    PersonalAdministrativo,
    Empleado
}

[Table("personas")]
public class Personas
{
    [Key]
    [Column("id_persona")]
    public int IdPersona { get; set; }

    [Required]
    [Column("nombre")]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Column("apellido_paterno")]
    [MaxLength(200)]
    public string? ApellidoPaterno { get; set; }

    [Column("apellido_materno")]
    [MaxLength(200)]
    public string? ApellidoMaterno { get; set; }

    [Required]
    [Column("tipo_persona")]
    [MaxLength(50)]
    public string TipoPersona { get; set; } = "Brigadista";

    [Required]
    [Column("matricula")]
    [MaxLength(50)]
    public string Matricula { get; set; } = string.Empty;

    [Column("huella")]
    public byte[]? Huella { get; set; }

    [Column("imagen")]
    [MaxLength(200)]
    public string? Imagen { get; set; }

    [NotMapped]
    public string NombreCompleto => 
        $"{Nombre} {ApellidoPaterno ?? ""} {ApellidoMaterno ?? ""}".Trim();

    public override string ToString() => NombreCompleto;
}
