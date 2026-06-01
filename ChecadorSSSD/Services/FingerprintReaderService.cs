using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DPUruNet;

namespace ChecadorSSSD.Services;

public class CapturedFingerprint
{
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public uint Width { get; init; }
    public uint Height { get; init; }
    public uint DPI { get; init; } = 500;
}

public class FingerprintCapturedEventArgs : EventArgs
{
    public CapturedFingerprint Fingerprint { get; }

    public FingerprintCapturedEventArgs(CapturedFingerprint fingerprint)
    {
        Fingerprint = fingerprint;
    }
}

public class FingerprintReaderService
{
    private Reader? _reader;
    private Reader.CaptureCallback? _captureHandler;
    private CancellationTokenSource? _cts;
    private bool _isCapturing = false;

    private const int DPFJ_FMD_DP_VER_FEATURES = 2;
    private const uint DPFPDD_SUCCESS = 0;
    private const int DPFPDD_IMG_FMT_ANSI381 = 0x001B0401;

    public bool IsAvailable => _reader != null;

    public event EventHandler<FingerprintCapturedEventArgs>? FingerprintCaptured;

    /// <summary>
    /// Obtiene el primer lector disponible sin abrirlo.
    /// </summary>
    private Reader? GetReader()
    {
        try
        {
            if (_reader != null) return _reader;

            var readers = ReaderCollection.GetReaders();
            if (readers.Count > 0)
            {
                _reader = readers[0];
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader obtained: " + _reader.Description.Name);
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] No readers found.");
            }
            return _reader;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] GetReader error: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Abre el lector (replicado de Form_Main.OpenReader).
    /// </summary>
    public bool OpenReader()
    {
        try
        {
            var reader = GetReader();
            if (reader == null) return false;

            var rc = reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
            if (rc == Constants.ResultCode.DP_SUCCESS)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader opened.");
                return true;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader.Open() failed: " + rc);
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] OpenReader error: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Cierra y libera el lector (replicado de CancelCaptureAndCloseReader).
    /// </summary>
    public void CloseReader()
    {
        if (_reader != null)
        {
            try
            {
                _reader.CancelCapture();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] CancelCapture error: " + ex.Message);
            }

            try
            {
                _reader.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader.Dispose error: " + ex.Message);
            }

            _reader = null;
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader closed.");
        }
    }

    /// <summary>
    /// Verifica el estado del lector y calibra si es necesario (replicado de Form_Main.GetStatus).
    /// </summary>
    private bool GetStatus()
    {
        if (_reader == null) return false;

        try
        {
            var rc = _reader.GetStatus();
            if (rc != Constants.ResultCode.DP_SUCCESS)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] GetStatus failed: " + rc);
                return false;
            }

            // Esperar si está ocupado
            while (_reader.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader busy, waiting...");
                Thread.Sleep(50);
            }

            // Calibrar si es necesario
            if (_reader.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader needs calibration.");
                var calRC = _reader.Calibrate();
                if (calRC != Constants.ResultCode.DP_SUCCESS)
                {
                    System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader.Calibrate() failed: " + calRC);
                    return false;
                }
            }
            else if (_reader.Status.Status != Constants.ReaderStatuses.DP_STATUS_READY)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Reader not ready: " + _reader.Status.Status);
                // No siempre es un error fatal; algunos lectores reportan READY después de un momento
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] GetStatus error: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Inicia una captura única (replicado de Form_Main.CaptureFingerAsync).
    /// </summary>
    private bool StartCapture()
    {
        if (_reader == null) return false;

        try
        {
            // Verificar estado antes de capturar
            if (!GetStatus()) return false;

            var rc = _reader.CaptureAsync(
                Constants.Formats.Fid.ANSI,
                Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT,
                _reader.Capabilities.Resolutions[0] // Usar resolución del lector, no hardcodeada
            );

            if (rc != Constants.ResultCode.DP_SUCCESS)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] CaptureAsync failed: " + rc);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] StartCapture error: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Inicia la captura continua de huellas.
    /// Abre el lector, suscribe el evento y lanza la primera captura.
    /// </summary>
    /// <summary>
    /// Inicializa el lector (stub para compatibilidad con ViewModels).
    /// </summary>
    public string Initialize()
    {
        return OpenReader() ? "Lector listo." : "No se pudo abrir el lector.";
    }

    /// <summary>
    /// Libera el lector (stub para compatibilidad con ViewModels).
    /// </summary>
    public void Dispose()
    {
        StopContinuousCapture();
    }

    /// <summary>
    /// Captura bloqueante de una sola huella (administración).
    /// Abre el lector, espera captura, y cierra.
    /// </summary>
    public CapturedFingerprint? CaptureFingerprint()
    {
        if (!OpenReader()) return null;

        var tcs = new TaskCompletionSource<CapturedFingerprint?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Reader.CaptureCallback? handler = null;

        handler = (CaptureResult result) =>
        {
            CapturedFingerprint? fingerprint = null;

            if (result.ResultCode == Constants.ResultCode.DP_SUCCESS && result.Data != null)
            {
                fingerprint = new CapturedFingerprint
                {
                    ImageData = result.Data.Bytes,
                    DPI = 500
                };
            }

            if (_reader != null && handler != null)
            {
                try { _reader.On_Captured -= handler; } catch { }
            }

            tcs.TrySetResult(fingerprint);
        };

        try
        {
            _reader!.On_Captured += handler;

            if (!StartCapture())
            {
                try { _reader.On_Captured -= handler; } catch { }
                CloseReader();
                return null;
            }

            if (!tcs.Task.Wait(TimeSpan.FromSeconds(30)))
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] CaptureFingerprint timeout.");
                try { _reader.On_Captured -= handler; } catch { }
                CloseReader();
                return null;
            }

            CloseReader();
            return tcs.Task.Result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] CaptureFingerprint error: " + ex.Message);
            try { _reader!.On_Captured -= handler; } catch { }
            CloseReader();
            return null;
        }
    }

    public void StartContinuousCapture()
    {
        if (_isCapturing) return;

        // Abrir el lector para esta sesión
        if (!OpenReader()) return;

        _cts = new CancellationTokenSource();
        _isCapturing = true;

        _captureHandler = (CaptureResult result) =>
        {
            if (_cts?.IsCancellationRequested == true) return;

            if (result.ResultCode == Constants.ResultCode.DP_SUCCESS && result.Data != null)
            {
                var fingerprint = new CapturedFingerprint
                {
                    ImageData = result.Data.Bytes,
                    DPI = 500
                };
                FingerprintCaptured?.Invoke(this, new FingerprintCapturedEventArgs(fingerprint));
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] Capture result: " + result.ResultCode);
            }

            // reiniciar captura si aún está activo
            if (_cts?.IsCancellationRequested == false)
            {
                Task.Run(() => StartCapture());
            }
        };

        try
        {
            _reader!.On_Captured += _captureHandler;

            if (!StartCapture())
            {
                _reader.On_Captured -= _captureHandler;
                _captureHandler = null;
                _isCapturing = false;
                CloseReader();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] StartContinuousCapture error: " + ex.Message);
            _isCapturing = false;
            CloseReader();
        }
    }

    /// <summary>
    /// Detiene la captura continua y cierra el lector.
    /// </summary>
    public void StopContinuousCapture()
    {
        _cts?.Cancel();
        _cts = null;
        _isCapturing = false;

        if (_reader != null && _captureHandler != null)
        {
            try
            {
                _reader.On_Captured -= _captureHandler;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] On_Captured -= error: " + ex.Message);
            }
            _captureHandler = null;
        }

        CloseReader();
    }

    // P/Invoke to dpfj.dll
    [DllImport("dpfj.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int dpfj_create_fmd_from_fid(
        int fid_type,
        byte[] fid,
        uint fid_size,
        int fmd_type,
        [In, Out] byte[] fmd,
        ref uint fmd_size);

    [DllImport("dpfj.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int dpfj_compare(
        int fmd1_type,
        byte[] fmd1,
        uint fmd1_size,
        uint fmd1_view_idx,
        int fmd2_type,
        byte[] fmd2,
        uint fmd2_size,
        uint fmd2_view_idx,
        out uint score);

    public byte[]? CreateFmd(byte[] fidData)
    {
        if (fidData == null || fidData.Length == 0) return null;

        try
        {
            byte[] fmdBuffer = new byte[65536];
            uint fmdSize = (uint)fmdBuffer.Length;

            int status = dpfj_create_fmd_from_fid(
                DPFPDD_IMG_FMT_ANSI381,
                fidData,
                (uint)fidData.Length,
                DPFJ_FMD_DP_VER_FEATURES,
                fmdBuffer,
                ref fmdSize);

            System.Diagnostics.Trace.WriteLine($"[FingerprintReaderService] dpfj_create_fmd_from_fid status={status}, fmdSize={fmdSize}");

            if (status != DPFPDD_SUCCESS || fmdSize == 0) return null;

            byte[] fmd = new byte[fmdSize];
            Array.Copy(fmdBuffer, fmd, (int)fmdSize);
            return fmd;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] CreateFmd exception: " + ex.GetType().Name + ": " + ex.Message);
            return null;
        }
    }

    public uint CompareFmds(byte[] fmd1, byte[] fmd2)
    {
        if (fmd1 == null || fmd2 == null || fmd1.Length == 0 || fmd2.Length == 0) return 0;

        try
        {
            uint score;
            int status = dpfj_compare(
                DPFJ_FMD_DP_VER_FEATURES,
                fmd1,
                (uint)fmd1.Length,
                0,
                DPFJ_FMD_DP_VER_FEATURES,
                fmd2,
                (uint)fmd2.Length,
                0,
                out score);

            System.Diagnostics.Trace.WriteLine($"[FingerprintReaderService] dpfj_compare status={status}, score={score}");

            if (status != DPFPDD_SUCCESS) return 0;
            return score;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("[FingerprintReaderService] CompareFmds exception: " + ex.GetType().Name + ": " + ex.Message);
            return 0;
        }
    }
}
