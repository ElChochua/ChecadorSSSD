using System;

namespace ChecadorSSSD.ViewModels;

/// <summary>
/// DTO simple para mostrar items de reporte en la vista de Administracion.
/// </summary>
public class ReporteAdminItem
{
    public string Fecha { get; set; } = string.Empty;
    public string HoraEntrada { get; set; } = string.Empty;
    public string HoraSalida { get; set; } = string.Empty;
    public string HorasTrabajadas { get; set; } = string.Empty;
}
