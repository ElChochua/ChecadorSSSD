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
    [Column("Nombre")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Column("Contrasenia")]
    [MaxLength(200)]
    public string Contrasenia { get; set; } = string.Empty;
}
