using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChecadorSSSD.ViewModels;

namespace ChecadorSSSD.Views;

public partial class FingerprintCaptureDialog : Window
{
    private FingerprintCaptureViewModel? _viewModel;

    public FingerprintCaptureDialog()
    {
        InitializeComponent();
    }

    public void Initialize(FingerprintCaptureViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.CaptureCompleted += OnCaptureCompleted;
        viewModel.CaptureCancelled += OnCaptureCancelled;

        viewModel.StartCapture();
    }

    private void OnCaptureCompleted(object? sender, EventArgs e)
    {
        // Auto-close on success after a short delay so the user sees the success message
        DispatcherTimer.RunOnce(() =>
        {
            Close(true);
        }, TimeSpan.FromSeconds(1.5));
    }

    private void OnCaptureCancelled(object? sender, EventArgs e)
    {
        Close(false);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.CancelCapture();
    }

    private void AcceptButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void RetryButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.StartCapture();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CaptureCompleted -= OnCaptureCompleted;
            _viewModel.CaptureCancelled -= OnCaptureCancelled;
            _viewModel.CancelCapture();
        }
        base.OnClosing(e);
    }
}
