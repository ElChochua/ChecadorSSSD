using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChecadorSSSD.Views;

public partial class PersonasView : UserControl
{
    public PersonasView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
