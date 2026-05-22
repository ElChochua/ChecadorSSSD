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
    [Column("id_personas")]
    public int IdPersonas { get; set; }

    [Required]
    [Column("Nombre")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Column("Apellido_paterno")]
    [MaxLength(100)]
    public string ApellidoPaterno { get; set; } = string.Empty;

    [Required]
    [Column("Apellido_materno")]
    [MaxLength(100)]
    public string ApellidoMaterno { get; set; } = string.Empty;

    [Required]
    [Column("Matricula")]
    [MaxLength(50)]
    public string Matricula { get; set; } = string.Empty;

    [Column("Tipo_persona")]
    [MaxLength(50)]
    public string? TipoPersona { get; set; }  // "Brigadista", "Asesor", etc.

    [Column("Imagen")]
    [MaxLength(500)]
    public string? Imagen { get; set; }

    /// <summary>
    /// Columna de tipo LONGBLOB para almacenar la huella dactilar.
    /// Se mantiene en standby hasta que se integre un escáner de huellas.
    /// </summary>
    [Column("Huella")]
    [MaxLength(65535)]
    public byte[]? Huella { get; set; }

    [NotMapped]
    public string NombreCompleto => $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim();

    public override string ToString() => NombreCompleto;

    // NOTA: No se define relación de navegación hacia Checadores porque la tabla checador
    // se almacenan directamente en cada registro del checador.
}
