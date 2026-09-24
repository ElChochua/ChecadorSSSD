using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using System;
using System.Linq;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;
using ChecadorSSSD.ViewModels;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Logging;
using Avalonia.Media.Imaging;

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

    private void ActualizarLogo_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdministracionViewModel vm && !string.IsNullOrWhiteSpace(vm.RutaLogo))
        {
            vm.GuardarRutaLogo(vm.RutaLogo);
        }
    }

    private void ActualizarLugar_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdministracionViewModel vm && !string.IsNullOrWhiteSpace(vm.RutaLugar))
        {
            vm.GuardarRutaLugar(vm.RutaLugar);
        }
    }

    private void ActualizarUniversidad_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdministracionViewModel vm && !string.IsNullOrWhiteSpace(vm.RutaUniversidad))
        {
            vm.GuardarRutaUniversidad(vm.RutaUniversidad);
        }
    }

    private async void SeleccionarImagenCarrusel_Click(object? sender, RoutedEventArgs e)
    {
        var path = await SeleccionarImagenAsync();
        if (path != null && DataContext is AdministracionViewModel vm)
        {
            vm.RutaImagenCarrusel = path;
            vm.NombreImagenCarrusel = System.IO.Path.GetFileNameWithoutExtension(path);
            // Agregar imagen directamente al carrusel
            vm.AgregarImagenCarrusel();
        }
    }

    private void EliminarImagenCarrusel_Click(object? sender, RoutedEventArgs e)
    {
        // Marcar el evento como manejado evita que el PointerPressed del Border
        // padre dispare ademas la apertura del preview de la imagen.
        e.Handled = true;
        if (sender is Button btn && btn.DataContext is ImagenCarrusel img && DataContext is AdministracionViewModel vm)
        {
            vm.EliminarImagenCarruselCommand.Execute(img);
        }
    }

    private void CarruselItem_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ImagenCarrusel img)
        {
            // Abrir modal con la imagen en grande
            if (DataContext is AdministracionViewModel)
            {
                var dialog = new ImagePreviewDialog();
                dialog.SetImagePath(img.Ruta);
                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner != null)
                {
                    dialog.ShowDialog(owner);
                }
                else
                {
                    dialog.Show();
                }
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

    private async void SeleccionarFotoPersona_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AdministracionViewModel vm) return;
        if (string.IsNullOrWhiteSpace(vm.PersonaEnEdicion.Matricula))
        {
            // Mostrar mensaje de error
            return;
        }

        var path = await SeleccionarImagenAsync();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string destDir = @"C:\FotografiasHuella";
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            var ext = Path.GetExtension(path);
            var destPath = Path.Combine(destDir, $"{vm.PersonaEnEdicion.Matricula}{ext}");
            int counter = 1;
            while (File.Exists(destPath))
            {
                destPath = Path.Combine(destDir, $"{vm.PersonaEnEdicion.Matricula}_{counter}{ext}");
                counter++;
            }

            File.Copy(path, destPath, true);

            vm.PersonaEnEdicion.Imagen = destPath;
            try
            {
                vm.VistaPreviaFotoPersona = new Bitmap(destPath);
            }
            catch
            {
                vm.VistaPreviaFotoPersona = null;
            }
        }
        catch (Exception ex)
        {
            // Log or show error
        }
    }

    private void EliminarFotoPersona_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AdministracionViewModel vm) return;
        vm.PersonaEnEdicion.Imagen = null;
        vm.VistaPreviaFotoPersona = null;
    }
}
