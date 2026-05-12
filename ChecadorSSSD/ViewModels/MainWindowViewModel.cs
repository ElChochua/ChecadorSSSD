using System;
using System.Windows.Input;

namespace ChecadorSSSD.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase? _selectedViewModel;
    private int _selectedIndex = 0;
    private readonly ChecadorViewModel _checadorViewModel;
    private readonly PersonasViewModel _personasViewModel;
    private readonly ReportesViewModel _reportesViewModel;

    public ViewModelBase? SelectedViewModel
    {
        get => _selectedViewModel;
        set => SetProperty(ref _selectedViewModel, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (SetProperty(ref _selectedIndex, value))
            {
                OnSelectedIndexChanged(value);
            }
        }
    }

    public ICommand NavigateToChecadorCommand { get; }
    public ICommand NavigateToPersonasCommand { get; }
    public ICommand NavigateToReportesCommand { get; }

    public MainWindowViewModel(ChecadorViewModel checadorViewModel, PersonasViewModel personasViewModel, ReportesViewModel reportesViewModel)
    {
        _checadorViewModel = checadorViewModel;
        _personasViewModel = personasViewModel;
        _reportesViewModel = reportesViewModel;
        
        SelectedViewModel = checadorViewModel;

        NavigateToChecadorCommand = new RelayCommand(() => SelectedIndex = 0);
        NavigateToPersonasCommand = new RelayCommand(() => SelectedIndex = 1);
        NavigateToReportesCommand = new RelayCommand(() => SelectedIndex = 2);
    }

    private void OnSelectedIndexChanged(int index)
    {
        SelectedViewModel = index switch
        {
            0 => _checadorViewModel,
            1 => _personasViewModel,
            2 => _reportesViewModel,
            _ => _checadorViewModel
        };
    }
}
