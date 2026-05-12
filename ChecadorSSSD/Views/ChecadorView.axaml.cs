using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ChecadorSSSD.ViewModels;

namespace ChecadorSSSD.Views;

public partial class ChecadorView : UserControl
{
    public ChecadorView()
    {
        InitializeComponent();
    }

    public ChecadorView(ChecadorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
