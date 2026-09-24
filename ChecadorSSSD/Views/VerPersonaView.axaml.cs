using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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

            // Intentar resolver la ruta completa de la imagen
            string rutaCompleta = persona.Imagen ?? string.Empty;
            if (!string.IsNullOrEmpty(rutaCompleta) && !Path.IsPathRooted(rutaCompleta))
            {
                rutaCompleta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rutaCompleta);
            }

            // Si no hay imagen asignada o no existe, usar la foto por defecto
            if (string.IsNullOrEmpty(rutaCompleta) || !File.Exists(rutaCompleta))
            {
                rutaCompleta = @"C:\FotografiasHuella\Dios.jpg";
            }

            if (File.Exists(rutaCompleta))
            {
                imageControl.Source = new Bitmap(rutaCompleta);
                imageControl.IsVisible = true;
            }
            else
            {
                imageControl.IsVisible = false;
            }
        }
        catch
        {
            // En caso de cualquier error, intentar ocultar la foto
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
            try
            {
                await ClipboardHelper.SetTextAsync(p.Matricula);
                MostrarCopiadoFeedback();
            }
            catch
            {
                if (CopiarBtnTexto != null) CopiarBtnTexto.Text = "Copiar Matricula";
            }
        }
    }

    private void MostrarCopiadoFeedback()
    {
        if (CopiarBtnTexto != null)
        {
            CopiarBtnTexto.Text = "Copiado!";
            CopiarBtnTexto.Foreground = new SolidColorBrush(Colors.DarkGreen);
        }
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(2000);
            if (CopiarBtnTexto != null)
            {
                CopiarBtnTexto.Text = "Copiar Matricula";
                CopiarBtnTexto.Foreground = null;
            }
        });
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
