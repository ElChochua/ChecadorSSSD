using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChecadorSSSD.Services;

/// <summary>
/// Servicio simple para guardar configuración de la aplicación en un archivo JSON.
/// </summary>
public class SettingsService
{
    private readonly string _configPath;

    public SettingsService()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = Path.Combine(baseDir,  "appconfig.json");
    }

    /// <summary>
    /// Obtiene un valor del archivo de configuración. Si no existe, devuelve defaultValue.
    /// </summary>
    public T Get<T>(string clave, T defaultValue)
    {
        try
        {
            if (!File.Exists(_configPath))
                return defaultValue;

            var json = File.ReadAllText(_configPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null && dict.TryGetValue(clave, out var valueString))
            {
                // Para bool, parsear directamente
                if (typeof(T) == typeof(bool) && bool.TryParse(valueString, out var boolValue))
                {
                    return (T)(object)boolValue;
                }

                return (T)Convert.ChangeType(valueString, typeof(T))!;
            }
        }
        catch
        {
            // Si falla la lectura, devolver default
        }
        return defaultValue;
    }

    /// <summary>
    /// Guarda un valor en el archivo de configuración.
    /// </summary>
    public void Set<T>(string clave, T valor)
    {
        var dict = new Dictionary<string, string>();
        try
        {
            if (File.Exists(_configPath))
            {
                var existing = File.ReadAllText(_configPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, string>>(existing) ?? new Dictionary<string, string>();
            }
        }
        catch { /* ignorar si el archivo está corrupto */ }

        dict[clave] = valor?.ToString() ?? string.Empty;

        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
