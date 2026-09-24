using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System.IO;

namespace ChecadorSSSD.Views;

public partial class ImagePreviewDialog : Window
{
    public ImagePreviewDialog()
    {
        InitializeComponent();
    }

    public void SetImagePath(string path)
    {
        var previewImage = this.FindControl<Image>("PreviewImage");
        if (previewImage != null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    previewImage.Source = null;
                    return;
                }
                if (!File.Exists(path))
                {
                    previewImage.Source = null;
                    return;
                }
                previewImage.Source = new Bitmap(path);
            }
            catch
            {
                previewImage.Source = null;
            }
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
