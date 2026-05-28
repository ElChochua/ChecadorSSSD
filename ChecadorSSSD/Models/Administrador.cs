using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChecadorSSSD.Models;

[Table("administradores")]
public class Administrador
{
    [Key]
    [Column("id_admin")]
    public int IdAdmin { get; set; }

    [Required]
    [Column("id_persona")]
    public int IdPersona { get; set; }

    [Required]
    [Column("usuario")]
    [MaxLength(200)]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    [Column("contrasenia")]
    [MaxLength(500)]
    public string Contrasenia { get; set; } = string.Empty;

    // Navegacion
    [ForeignKey("IdPersona")]
    public Personas? Persona { get; set; }
}
