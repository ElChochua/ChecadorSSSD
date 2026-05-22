using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.Views;

public partial class VerPersonaView : Window
{
    public VerPersonaView()
    {
        InitializeComponent();
    }

    public VerPersonaView(Personas persona) : this()
    {
        DataContext = persona;
        CargarFoto(persona);
    }

    private void CargarFoto(Personas persona)
    {
        try
        {
            var imageControl = this.Get<Image>("FotoImagen");
            if (imageControl == null) return;

            // Si no hay ruta de imagen, no hacer nada (dejar el placeholder vacio)
            if (string.IsNullOrEmpty(persona.Imagen))
            {
                imageControl.IsVisible = false;
                return;
            }

            // Intentar resolver la ruta completa de la imagen
            string rutaCompleta;
            if (Path.IsPathRooted(persona.Imagen))
            {
                rutaCompleta = persona.Imagen;
            }
            else
            {
                rutaCompleta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, persona.Imagen);
            }

            // Verificar si el archivo existe
            if (File.Exists(rutaCompleta))
            {
                imageControl.Source = new Bitmap(rutaCompleta);
                imageControl.IsVisible = true;
            }
            else
            {
                // Si no existe la imagen, simplemente no mostrarla
                imageControl.IsVisible = false;
            }
        }
        catch
        {
            // En caso de cualquier error (permisos, formato corrupto, etc.), no mostrar foto
            try
            {
                var imageControl = this.Get<Image>("FotoImagen");
                if (imageControl != null) imageControl.IsVisible = false;
            }
            catch { /* ignorar */ }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void CopiarMatricula_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Personas p && !string.IsNullOrEmpty(p.Matricula))
        {
            await ClipboardHelper.SetTextAsync(p.Matricula);
        }
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
