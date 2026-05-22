using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace ChecadorSSSD.Services;

/// <summary>
/// Helper para operaciones de clipboard que usa reflection para ser compatible
/// con diferentes versiones de la API de Avalonia (incluyendo la 12.X
/// donde IClipboard.SetTextAsync puede no estar disponible).
/// </summary>
public static class ClipboardHelper
{
    /// <summary>
    /// Intenta copiar el texto al portapapeles del sistema.
    /// Es safe para usar desde cualquier lugar (ViewModels, code-behind, etc.).
    /// </summary>
    public static async Task SetTextAsync(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            // Obtener la ventana principal a través de ApplicationLifetime
            var lifetime = Application.Current?.ApplicationLifetime;
            if (lifetime == null) return;

            // Leer propiedad MainWindow vía reflection para evitar dependencia de tipo exacto
            var mainWindowProperty = lifetime.GetType().GetProperty("MainWindow");
            var mainWindow = mainWindowProperty?.GetValue(lifetime);
            if (mainWindow == null) return;

            // Leer propiedad Clipboard vía reflection
            var clipboardProperty = mainWindow.GetType().GetProperty("Clipboard");
            var clipboard = clipboardProperty?.GetValue(mainWindow);
            if (clipboard == null) return;

            // Buscar método SetTextAsync o SetText (soporta ambos)
            var method = clipboard.GetType().GetMethod("SetTextAsync", new[] { typeof(string) })
                         ?? clipboard.GetType().GetMethod("SetText", new[] { typeof(string) });

            if (method != null)
            {
                var result = method.Invoke(clipboard, new object[] { text });
                if (result is Task task)
                {
                    await task;
                }
            }
            else
            {
                // Fallback: usar IDataObject / SetDataObjectAsync si existe
                var dataObjectType = clipboard.GetType().Assembly.GetType("Avalonia.Input.DataObject");
                var dataObject = dataObjectType != null 
                    ? Activator.CreateInstance(dataObjectType) 
                    : null;
                
                if (dataObject != null)
                {
                    dataObjectType!.GetMethod("Set", new[] { typeof(string), typeof(object) })?.Invoke(dataObject, new object[] { "text", text });
                    var setDataMethod = clipboard.GetType().GetMethod("SetDataObjectAsync", new[] { dataObjectType });
                    if (setDataMethod != null)
                    {
                        var result = setDataMethod.Invoke(clipboard, new object[] { dataObject });
                        if (result is Task task) await task;
                    }
                }
            }
        }
        catch
        {
            // Ignorar errores silenciosamente (no es crítico)
        }
    }
}
