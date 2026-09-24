using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ChecadorSSSD.Services;
using Avalonia.Threading;

namespace ChecadorSSSD.Views;

public partial class NuevoPersonaDialog : Window
{
    private FingerprintService? _fingerprintService;
    private CancellationTokenSource? _cts;
    private byte[]? _capturedFmd;
    private bool _completed;
    private bool _isClosed;
    private string? _rutaFotoSeleccionada;

    public NuevoPersonaDialog()
    {
        InitializeComponent();
    }

    public byte[]? CapturedFmd => _capturedFmd;

    public PersonaDatos DatosPersona => new PersonaDatos
    {
        Nombre = NombreTextBox?.Text?.Trim() ?? "",
        ApellidoPaterno = ApellidoPaternoTextBox?.Text?.Trim() ?? "",
        ApellidoMaterno = ApellidoMaternoTextBox?.Text?.Trim() ?? "",
        Matricula = MatriculaTextBox?.Text?.Trim() ?? "",
        TipoPersona = (TipoComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Alumno",
        Imagen = _rutaFotoSeleccionada
    };

    public async Task<bool?> ShowAddPerson(FingerprintService fingerService, Window owner)
    {
        _fingerprintService = fingerService;
        _cts = new CancellationTokenSource();
        _capturedFmd = null;
        _completed = false;
        EstadoHuellaText.Text = "Sin CaptWorkbench";

        var result = await ShowDialog<bool?>(owner);

        if (!_completed)
        {
            _cts.Cancel();
        }

        return result;
    }

    private async void CapturarHuellaBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_fingerprintService == null) return;

        try
        {
            var dialog = new FingerprintEnrollmentDialog();
            bool? resultado = await dialog.ShowEnrollment(_fingerprintService, this);

            if (resultado == true && dialog.CapturedFmd != null)
            {
                _capturedFmd = dialog.CapturedFmd;
                EstadoHuellaText.Text = "Huella Capturada";
                EstadoHuellaText.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                EstadoHuellaText.Text = "Captura Cancelada";
                EstadoHuellaText.Foreground = new SolidColorBrush(Colors.Orange);
            }
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error al abrir o completar la captura de huella: {ex}");
            EstadoHuellaText.Text = $"Error de captura: {ex.Message}";
            EstadoHuellaText.Foreground = new SolidColorBrush(Colors.Red);
        }
    }

    private async void SeleccionarFotoBtn_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var options = new FilePickerOpenOptions
            {
                Title = "Seleccionar foto de persona",
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
                var path = file.TryGetLocalPath() ?? file.Path.LocalPath;
                var matricula = MatriculaTextBox?.Text?.Trim() ?? "sin_matricula";
                var ext = System.IO.Path.GetExtension(path);
                var destDir = @"C:\FotografiasHuella";
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                var destPath = System.IO.Path.Combine(destDir, $"{matricula}{ext}");
                int counter = 1;
                while (System.IO.File.Exists(destPath))
                {
                    destPath = System.IO.Path.Combine(destDir, $"{matricula}_{counter}{ext}");
                    counter++;
                }
                System.IO.File.Copy(path, destPath, true);
                _rutaFotoSeleccionada = destPath;
                try
                {
                    FotoPreview.Source = new Bitmap(destPath);
                    FotoPreview.IsVisible = true;
                }
                catch
                {
                    FotoPreview.IsVisible = false;
                }
            }
        }
        catch { /* ignorar */ }
    }

    private void GuardarBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
        {
            MostrarError("El campo Nombre es obligatorio.");
            return;
        }
        if (string.IsNullOrWhiteSpace(ApellidoPaternoTextBox.Text))
        {
            MostrarError("El campo Apellido Paterno es obligatorio.");
            return;
        }
        if (string.IsNullOrWhiteSpace(MatriculaTextBox.Text))
        {
            MostrarError("El campo Matricula es obligatorio.");
            return;
        }
        OcultarError();
        _completed = true;
        Close(true);
    }

    private void MostrarError(string mensaje)
    {
        if (ErrorText != null) ErrorText.Text = mensaje;
        if (ErrorBanner != null) ErrorBanner.IsVisible = true;
    }

    private void OcultarError()
    {
        if (ErrorText != null) ErrorText.Text = "";
        if (ErrorBanner != null) ErrorBanner.IsVisible = false;
    }

    private void CancelBtn_Click(object? sender, RoutedEventArgs e)
    {
        _completed = false;
        Close(null);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _isClosed = true;
        _cts?.Cancel();
        base.OnClosing(e);
    }
}

public class PersonaDatos
{
    public string Nombre { get; set; } = "";
    public string ApellidoPaterno { get; set; } = "";
    public string ApellidoMaterno { get; set; } = "";
    public string Matricula { get; set; } = "";
    public string TipoPersona { get; set; } = "Alumno";
    public string? Imagen { get; set; } = string.Empty;
}
