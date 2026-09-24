using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Services;

/// <summary>
/// Servicio para exportar listado de personas a CSV.
/// </summary>
public class ExportService
{
    /// <summary>
    /// Exporta una lista de personas a un archivo CSV y devuelve la ruta del archivo generado.
    /// </summary>
    public Task<string> ExportarPersonasCsvAsync(List<Personas> personas, string? rutaDestino = null)
    {
        return Task.Run(() =>
        {
            // Si no se proporciona ruta, generar una por defecto en Downloads o directorio de la app
            if (string.IsNullOrEmpty(rutaDestino))
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var fileName = $"Personas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                rutaDestino = Path.Combine(baseDir, fileName);
            }

            var sb = new StringBuilder();
            // BOM para que Excel detecte correctamente la codificación UTF-8
            sb.Append("\uFEFF");
            // Encabezados
            sb.AppendLine("Nombre,Apellido Paterno,Apellido Materno,Matricula,Tipo de Persona");
            foreach (var p in personas.OrderBy(x => x.ApellidoPaterno).ThenBy(x => x.Nombre))
            {
                sb.AppendLine($"\"{Escape(p.Nombre)}\",\"{Escape(p.ApellidoPaterno)}\",\"{Escape(p.ApellidoMaterno)}\",\"{Escape(p.Matricula)}\",\"{Escape(p.TipoPersona)}\"");
            }

            var dir = Path.GetDirectoryName(rutaDestino);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(rutaDestino, sb.ToString(), System.Text.Encoding.UTF8);
            return rutaDestino;
        });
    }

    private static string Escape(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        // Reemplazar comillas dobles por dos comillas dobles (estándar CSV)
        return input.Replace("\"", "\"\"").Replace("\r", "").Replace("\n", "");
    }
}
