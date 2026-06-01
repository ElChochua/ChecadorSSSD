using System;
using System.Threading.Tasks;
using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.ViewModels;

public class FingerprintCaptureViewModel : ViewModelBase
{
    private readonly FingerprintReaderService _fingerprintReader;

    private string _statusText = "Coloque su dedo en el lector...";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private bool _isCapturing = true;
    public bool IsCapturing
    {
        get => _isCapturing;
        set => SetProperty(ref _isCapturing, value);
    }

    private bool _isSuccess = false;
    public bool IsSuccess
    {
        get => _isSuccess;
        set => SetProperty(ref _isSuccess, value);
    }

    private bool _isError = false;
    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    public byte[]? CapturedFmd { get; private set; }

    public event EventHandler? CaptureCompleted;
    public event EventHandler? CaptureCancelled;

    public FingerprintCaptureViewModel(FingerprintReaderService fingerprintReader)
    {
        _fingerprintReader = fingerprintReader;
        _fingerprintReader.FingerprintCaptured += OnFingerprintCaptured;
    }

    public void StartCapture()
    {
        if (!_fingerprintReader.IsAvailable)
        {
            StatusText = "Error: El lector no está disponible.";
            IsError = true;
            IsCapturing = false;
            return;
        }

        StatusText = "Coloque su dedo en el lector...";
        IsCapturing = true;
        IsError = false;
        IsSuccess = false;

        // Ensure any previous continuous capture is stopped before starting our own
        _fingerprintReader.StopContinuousCapture();
        _fingerprintReader.StartContinuousCapture();
    }

    private async void OnFingerprintCaptured(object? sender, FingerprintCapturedEventArgs e)
    {
        // Only take the first capture if we're still in capturing state
        if (!IsCapturing) return;

        IsCapturing = false;
        StatusText = "Procesando huella...";

        try
        {
            byte[]? fmd = await Task.Run(() => _fingerprintReader.CreateFmd(e.Fingerprint.ImageData));

            if (fmd == null)
            {
                StatusText = "Error al procesar la huella.";
                IsError = true;
            }
            else
            {
                CapturedFmd = fmd;
                StatusText = "Huella capturada correctamente.";
                IsSuccess = true;
                CaptureCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            IsError = true;
        }

        _fingerprintReader.StopContinuousCapture();
    }

    public void CancelCapture()
    {
        _fingerprintReader.FingerprintCaptured -= OnFingerprintCaptured;
        _fingerprintReader.StopContinuousCapture();
        CaptureCancelled?.Invoke(this, EventArgs.Empty);
    }
}
