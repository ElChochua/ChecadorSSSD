using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ChecadorSSSD.ViewModels;

namespace ChecadorSSSD;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
