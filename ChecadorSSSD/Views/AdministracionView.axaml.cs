using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using System.Linq;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;
using ChecadorSSSD.ViewModels;
using System.Threading.Tasks;

namespace ChecadorSSSD.Views;

public partial class AdministracionView : UserControl
{
    public AdministracionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void SeleccionarLogo_Click(object? sender, RoutedEventArgs e)
    {
        var path = await SeleccionarImagenAsync();
        if (path != null && DataContext is AdministracionViewModel vm)
        {
            vm.GuardarRutaLogo(path);
        }
    }

    private async void SeleccionarLugar_Click(object? sender, RoutedEventArgs e)
    {
        var path = await SeleccionarImagenAsync();
        if (path != null && DataContext is AdministracionViewModel vm)
        {
            vm.GuardarRutaLugar(path);
        }
    }

    private async void SeleccionarUniversidad_Click(object? sender, RoutedEventArgs e)
    {
        var path = await SeleccionarImagenAsync();
        if (path != null && DataContext is AdministracionViewModel vm)
        {
            vm.GuardarRutaUniversidad(path);
        }
    }

    private async void SeleccionarImagenCarrusel_Click(object? sender, RoutedEventArgs e)
    {
        var path = await SeleccionarImagenAsync();
        if (path != null && DataContext is AdministracionViewModel vm)
        {
            vm.RutaImagenCarrusel = path;
            vm.NombreImagenCarrusel = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }

    private void CarruselItem_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ImagenCarrusel img &&
            !string.IsNullOrWhiteSpace(img.Ruta) && System.IO.File.Exists(img.Ruta))
        {
            try
            {
                var window = new Window
                {
                    Title = img.Nombre,
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new Avalonia.Controls.ScrollViewer
                    {
                        Content = new Avalonia.Controls.Image
                        {
                            Source = new Avalonia.Media.Imaging.Bitmap(img.Ruta),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                        }
                    }
                };
                var topLevel = TopLevel.GetTopLevel(this) as Window;
                if (topLevel != null)
                    window.ShowDialog(topLevel);
            }
            catch
            {
                // Si falla, intenta abrir con el visor predeterminado del sistema
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(img.Ruta) { UseShellExecute = true });
            }
        }
    }

    private async Task<string?> SeleccionarImagenAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return null;

            var options = new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Imagenes") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif"] },
                    new FilePickerFileType("Todos los archivos") { Patterns = ["*.*"] }
                ]
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            var file = files.FirstOrDefault();
            if (file != null)
            {
                return file.TryGetLocalPath() ?? file.Path.LocalPath;
            }
        }
        catch { /* Si no funciona, simplemente no pasa nada */ }
        return null;
    }
}
