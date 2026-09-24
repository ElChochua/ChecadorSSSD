using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.Views;

public partial class FingerprintEnrollmentDialog : Window
{
    private FingerprintService? _fingerprintService;
    private CancellationTokenSource? _cts;
    private byte[]? _capturedFmd;
    private bool _completed;
    private int _currentStep;
    private bool _isClosed;
    private Task<byte[]?>? _currentEnrollmentTask;

    public FingerprintEnrollmentDialog()
    {
        InitializeComponent();
    }

    public byte[]? CapturedFmd => _capturedFmd;

    public async Task<bool?> ShowEnrollment(FingerprintService fingerService, Window owner)
    {
        _fingerprintService = fingerService;
        _cts = new CancellationTokenSource();

        if (!_fingerprintService.IsAvailable)
        {
            _fingerprintService.Inicializar();
            if (!_fingerprintService.IsAvailable)
            {
                StatusText.Text = "Error: No se pudo conectar al lector de huellas.";
                ErrorPanel.IsVisible = true;
                AcceptBtn.IsVisible = true;
                AcceptBtn.Content = "Cerrar";
                CancelBtn.IsVisible = false;
                return await ShowDialog<bool?>(owner);
            }
        }

        CapturingPanel.IsVisible = true;
        CancelBtn.IsVisible = true;

        IniciarEnrollment();

        var result = await ShowDialog<bool?>(owner);

        if (!_completed)
        {
            _cts.Cancel();
            _fingerprintService.CancelarEnrollment();
        }

        return result;
    }

    private void IniciarEnrollment()
    {
        _completed = false;
        _capturedFmd = null;
        _currentStep = 0;
        UpdateStepDots();
        CapturingPanel.IsVisible = true;
        SuccessPanel.IsVisible = false;
        ErrorPanel.IsVisible = false;
        AcceptBtn.IsVisible = false;
        RetryBtn.IsVisible = false;
        CancelBtn.IsVisible = true;
        FingerprintImage.IsVisible = false;
        NoImagePlaceholder.IsVisible = true;

        var progress = new Progress<EnrollmentProgress>(OnProgress);
        _currentEnrollmentTask = _fingerprintService!.EnrollAsync(progress, _cts!.Token);

        _ = _currentEnrollmentTask.ContinueWith(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_isClosed) return;

                if (t.IsCanceled)
                {
                    CapturingPanel.IsVisible = false;
                    ErrorPanel.IsVisible = true;
                    StatusText.Text = "La captura fue cancelada.";
                    RetryBtn.IsVisible = true;
                    CancelBtn.IsVisible = true;
                    return;
                }

                if (t.IsFaulted)
                {
                    var error = t.Exception?.GetBaseException();
                    FingerprintLogger.LogError($"Enrollment finalizó con error: {error}");
                    CapturingPanel.IsVisible = false;
                    ErrorPanel.IsVisible = true;
                    StatusText.Text = $"Error durante la captura: {error?.Message ?? "Error desconocido."}";
                    RetryBtn.IsVisible = true;
                    CancelBtn.IsVisible = true;
                    return;
                }

                _capturedFmd = t.Result;
                _completed = true;
                CapturingPanel.IsVisible = false;

                if (_capturedFmd != null)
                {
                    AcceptBtn.IsVisible = true;
                    CancelBtn.IsVisible = false;
                    RetryBtn.IsVisible = false;
                    SuccessPanel.IsVisible = true;
                    ErrorPanel.IsVisible = false;
                }
                else
                {
                    if (!ErrorPanel.IsVisible)
                    {
                        ErrorPanel.IsVisible = true;
                        RetryBtn.IsVisible = true;
                        CancelBtn.IsVisible = true;
                    }
                }
            });
        }, TaskScheduler.Default);
    }

    private void OnProgress(EnrollmentProgress p)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_isClosed) return;

            _currentStep = p.CaptureCount;
            StatusText.Text = p.Status;

            if (p.FingerprintImage != null)
            {
                FingerprintImage.Source = p.FingerprintImage;
                FingerprintImage.IsVisible = true;
                NoImagePlaceholder.IsVisible = false;
            }

            UpdateStepDots();

            if (p.CaptureCount == 0 && p.IsError)
            {
                CapturingPanel.IsVisible = false;
                ErrorPanel.IsVisible = true;
                RetryBtn.IsVisible = true;
                CancelBtn.IsVisible = true;
            }
        });
    }

    private void UpdateStepDots()
    {
        var green = Brush.Parse("#2ECC71");
        var gold = Brush.Parse("#C5A54A");
        var gray = Brush.Parse("#4A5568");

        Step1Dot.Fill = _currentStep >= 1 ? green : gray;
        Step2Dot.Fill = _currentStep >= 2 ? green : gray;
        Step3Dot.Fill = _currentStep >= 3 ? green : gray;
        Step4Dot.Fill = _currentStep >= 4 ? gold : gray;
    }

    private void AcceptBtn_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void CancelBtn_Click(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _fingerprintService?.CancelarEnrollment();
        Close(null);
    }

    private void RetryBtn_Click(object? sender, RoutedEventArgs e)
    {
        IniciarEnrollment();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_completed)
        {
            _cts?.Cancel();
            _fingerprintService?.CancelarEnrollment();
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosed = true;
        base.OnClosed(e);
    }
}
