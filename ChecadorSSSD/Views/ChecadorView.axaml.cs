using Avalonia.Controls;
using Avalonia.Input;
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

    private void OnMatriculaKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is ChecadorViewModel vm)
            {
                if (vm.RegistrarCommand.CanExecute(null))
                {
                    vm.RegistrarCommand.Execute(null);
                }
            }
        }
    }
}
