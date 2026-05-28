using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChecadorSSSD.Models;

[Table("asistencias_checador")]
public class AsistenciaChecador
{
    [Key]
    [Column("id_asistencia")]
    public int IdAsistencia { get; set; }

    [Required]
    [Column("id_persona")]
    public int IdPersona { get; set; }

    [Required]
    [Column("hora_entrada")]
    public DateTime HoraEntrada { get; set; } = DateTime.Now;

    [Column("hora_salida")]
    public DateTime? HoraSalida { get; set; }

    [Column("estatus")]
    [MaxLength(30)]
    public string? Estatus { get; set; }

    // Navegaci�n
    [ForeignKey("IdPersona")]
    public Personas? Persona { get; set; }
}
