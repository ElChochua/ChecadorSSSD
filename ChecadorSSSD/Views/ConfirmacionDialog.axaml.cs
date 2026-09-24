using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ChecadorSSSD.Views;

public partial class ConfirmacionDialog : Window
{
    private TextBlock? _tituloText;
    private TextBlock? _mensajeText;
    private Button? _aceptarBtn;

    public ConfirmacionDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        // AvaloniaXamlLoader no materializa campos x:Name en este proyecto,
        // asi que los resolvemos manualmente con FindControl.
        _tituloText = this.FindControl<TextBlock>(nameof(TituloText));
        _mensajeText = this.FindControl<TextBlock>(nameof(MensajeText));
        _aceptarBtn = this.FindControl<Button>(nameof(AceptarBtn));
    }

    public async Task<bool> ShowConfirm(string titulo, string mensaje, Window owner)
    {
        if (_tituloText != null)
            _tituloText.Text = string.IsNullOrWhiteSpace(titulo) ? "Confirmar accion" : titulo;
        if (_mensajeText != null)
            _mensajeText.Text = string.IsNullOrWhiteSpace(mensaje) ? "Esta seguro?" : mensaje;
        if (_aceptarBtn != null)
            _aceptarBtn.Content = "Eliminar";
        return await ShowDialog<bool>(owner);
    }

    private void AceptarBtn_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void CancelarBtn_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
