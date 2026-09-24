using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChecadorSSSD.Models;

public enum TipoAccion
{
    Entrada,
    Salida
}

// Modelo crudo exacto a la tabla checador de la BD.
// TipoPersona y TipoAccion son strings porque en la BD son VARCHAR y pueden tener NULL.
// Hora es string porque en la BD es VARCHAR.
[Table("checador")]
public class Checadores
{
    [Key]
    [Column("id_checador")]
    public int IdChecador { get; set; }

    [Column("Tipo_accion")]
    public string? TipoAccion { get; set; }  // "Entrada", "Salida", o NULL

    public bool EsEntrada => string.Equals(TipoAccion, "Entrada", StringComparison.OrdinalIgnoreCase);
    public bool EsSalida => string.Equals(TipoAccion, "Salida", StringComparison.OrdinalIgnoreCase);

    [Required]
    [Column("Fecha")]
    public DateTime Fecha { get; set; }

    [Required]
    [Column("Hora")]
    public string? HoraRaw { get; set; }  // Almacena el valor VARCHAR de la BD

    // Getter/Setter para convertir entre string y TimeSpan
    [NotMapped]
    public TimeSpan Hora
    {
        get => ParseHora(HoraRaw);
        set => HoraRaw = FormatHora(value);
    }

    [Required]
    [Column("Matricula")]
    [MaxLength(50)]
    public string Matricula { get; set; } = string.Empty;

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

    [Column("Tipo_persona")]
    public string? TipoPersona { get; set; }  // "Brigadista", "Asesor", etc., o NULL

    // Helpers estáticos para parsear la hora de forma robusta
    private static TimeSpan ParseHora(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return TimeSpan.Zero;
        var cleaned = raw.Trim().Trim('"').Trim('\'').Replace(';', ':').Replace('.', ':');
        if (TimeSpan.TryParseExact(cleaned, @"hh\:mm\:ss", null, out var t1)) return t1;
        if (TimeSpan.TryParseExact(cleaned, @"h\:mm\:ss", null, out var t2)) return t2;
        if (TimeSpan.TryParse(cleaned, out var t3)) return t3;
        return TimeSpan.Zero;
    }

    private static string FormatHora(TimeSpan ts)
    {
        return ts.ToString(@"hh\:mm\:ss");
    }
}
