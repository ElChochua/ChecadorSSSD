using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ChecadorSSSD.Data;
using DPUruNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChecadorSSSD.Services;

public class EnrollmentProgress
{
    public int CaptureCount { get; set; }
    public int TotalCaptures { get; set; } = 4;
    public Bitmap? FingerprintImage { get; set; }
    public string Status { get; set; } = "";
    public bool IsComplete { get; set; }
    public bool IsError { get; set; }
}

public sealed class FingerprintService : IDisposable
{
    private Reader? _reader;
    private bool _readerOpened;
    private bool _disposed;
    private readonly object _lock = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public const int PROBABILITY_ONE = 0x7fffffff;
    public const int DEFAULT_THRESHOLD = PROBABILITY_ONE / 100000;

    private bool _handlerRegistered;

    // Verification state
    private volatile bool _verifyActive;
    private List<(string matricula, Fmd fmd)> _gallery = new();
    private Action<string>? _onMatch;
    private Action? _onNoMatch;
    private int _verifyProcessing;
    private CancellationTokenSource? _verifyCts;

    // Enrollment state
    private TaskCompletionSource<byte[]?>? _enrollmentTcs;
    private List<Fmd> _enrollmentFmds = new();
    private int _enrollmentCount;
    private volatile bool _enrollmentActive;
    private IProgress<EnrollmentProgress>? _enrollmentProgress;
    private CancellationTokenSource? _enrollmentCts;

    public bool IsAvailable
    {
        get { lock (_lock) return _reader != null && _readerOpened; }
    }

    public FingerprintService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Inicializar()
    {
        FingerprintLogger.LogHardware("Inicializando lector");
        if (OpenReader())
        {
            FingerprintLogger.LogHardware("Lector abierto correctamente");
            Trace.WriteLine("[FingerprintService] Lector abierto correctamente.");
            return "Lector listo.";
        }

        lock (_lock)
        {
            if (_reader != null) { try { _reader.Dispose(); } catch { } _reader = null; _readerOpened = false; }
        }

        Thread.Sleep(300);
        if (OpenReader())
        {
            FingerprintLogger.LogHardware("Lector abierto en reintento");
            Trace.WriteLine("[FingerprintService] Lector abierto (reintento).");
            return "Lector listo (reconectado).";
        }
        FingerprintLogger.LogError("No se pudo abrir el lector despues de reintentos");
        Trace.WriteLine("[FingerprintService] No se pudo abrir el lector.");
        return "No se pudo abrir el lector.";
    }

    public async Task RecargarGaleriaAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var personas = await ctx.Personas
                .AsNoTracking()
                .Where(p => p.Huella != null && p.Huella.Length > 0)
                .ToListAsync();

            FingerprintLogger.LogDb($"RecargarGaleria: {personas.Count} personas con huella en BD");

            var g = new List<(string, Fmd)>();
            foreach (var p in personas)
            {
                if (p.Huella == null || p.Huella.Length == 0)
                {
                    FingerprintLogger.LogDb($"RecargarGaleria: {p.Matricula} - Huella nula o vacia, ignorando");
                    continue;
                }
                var fmd = ReconstructFmd(p.Huella);
                if (fmd != null)
                {
                    g.Add((p.Matricula, fmd));
                    FingerprintLogger.LogDb($"RecargarGaleria: {p.Matricula} - FMD reconstruido OK");
                }
                else
                {
                    FingerprintLogger.LogError($"RecargarGaleria: {p.Matricula} - Fallo reconstruccion FMD!");
                }
            }
            lock (_lock) { _gallery = g; }
            FingerprintLogger.LogInfo($"Galeria recargada: {g.Count} huellas cargadas exitosamente");
            Trace.WriteLine($"[FingerprintService] Galeria: {g.Count} huellas cargadas.");
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error cargando galeria: {ex}");
            Trace.WriteLine($"[FingerprintService] Error cargando galeria: {ex.Message}");
        }
    }

    #region Verification

    public void IniciarVerificacion(Action<string> onMatch, Action onNoMatch)
    {
        lock (_lock)
        {
            if (_verifyActive) return;
            _verifyActive = true;
        }

        _onMatch = onMatch;
        _onNoMatch = onNoMatch;
        _verifyCts = new CancellationTokenSource();

        FingerprintLogger.LogVerify("=== Iniciando verificacion ===");
        Trace.WriteLine("[FingerprintService] Iniciando verificacion...");
        FingerprintLogger.LogVerify($"Galeria en memoria: {_gallery.Count} huellas");
        ArmCapture();
    }

    public void DetenerVerificacion()
    {
        bool wasActive;
        lock (_lock)
        {
            wasActive = _verifyActive;
            _verifyActive = false;
        }

        if (!wasActive) return;

        _verifyCts?.Cancel();
        _onMatch = null;
        _onNoMatch = null;

        FingerprintLogger.LogVerify("=== Deteniendo verificacion ===");
        Trace.WriteLine("[FingerprintService] Deteniendo verificacion...");
        CancelCapture();
    }

    private void OnVerificationCaptured(CaptureResult result)
    {
        FingerprintLogger.LogVerify("Captura iniciada");

        if (!_verifyActive)
        {
            FingerprintLogger.LogVerify("Captura ignorada: verificacion inactiva");
            Trace.WriteLine("[FingerprintService] Verify capturado pero inactivo.");
            return;
        }

        if (Interlocked.CompareExchange(ref _verifyProcessing, 1, 0) != 0)
        {
            FingerprintLogger.LogVerify("Captura ignorada: procesamiento en curso");
            Trace.WriteLine("[FingerprintService] Verify capturado pero procesamiento en curso.");
            return;
        }

        // Capturar las acciones localmente en caso de que sean limpiadas desde otro hilo
        var onMatchLocal = _onMatch;
        var onNoMatchLocal = _onNoMatch;

        try
        {
            if (result.Data == null || result.ResultCode != Constants.ResultCode.DP_SUCCESS)
            {
                FingerprintLogger.LogVerify($"Captura invalida: {result.ResultCode}");
                Trace.WriteLine($"[FingerprintService] Verify capture invalido: {result.ResultCode}");
                return;
            }

            var fmd = CreateFmdFromFid(result.Data);
            if (fmd == null)
            {
                FingerprintLogger.LogVerify("FALLA: No se pudo extraer FMD de la captura");
                Trace.WriteLine("[FingerprintService] Verify: no se pudo extraer FMD.");
                return;
            }
            FingerprintLogger.LogVerify("FMD extraido correctamente de la captura");

            List<(string matricula, Fmd fmd)> galleryCopy;
            lock (_lock) { galleryCopy = _gallery.ToList(); }
            FingerprintLogger.LogVerify($"Galeria tiene {galleryCopy.Count} huellas cargadas");

            if (galleryCopy.Count == 0)
            {
                FingerprintLogger.LogNoMatch("Galeria vacia - no hay huellas para comparar");
                Trace.WriteLine("[FingerprintService] Verify: galeria vacia.");
                if (onNoMatchLocal != null)
                    Dispatcher.UIThread.Post(() => onNoMatchLocal());
                return;
            }

            int bestIdx = -1;
            int bestScore = int.MaxValue;
            FingerprintLogger.LogVerify("Iniciando comparacion con galeria...");

            for (int i = 0; i < galleryCopy.Count; i++)
            {
                var r = Comparison.Compare(fmd, 0, galleryCopy[i].fmd, 0);
                if (r.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    FingerprintLogger.LogVerify($"  vs {galleryCopy[i].matricula}: Compare fallo ({r.ResultCode})");
                    continue;
                }
                FingerprintLogger.LogVerify($"  vs {galleryCopy[i].matricula}: Score = {r.Score} (umbral = {DEFAULT_THRESHOLD})");
                if (r.Score >= 0 && r.Score < bestScore && r.Score < DEFAULT_THRESHOLD)
                {
                    bestScore = r.Score;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                var matricula = galleryCopy[bestIdx].matricula;
                FingerprintLogger.LogMatch($"Huella reconocida! Matricula = {matricula}, Score = {bestScore}, Threshold = {DEFAULT_THRESHOLD}");
                Trace.WriteLine($"[FingerprintService] Verify: MATCH {matricula} (score={bestScore}).");
                if (onMatchLocal != null)
                    Dispatcher.UIThread.Post(() => onMatchLocal(matricula));
            }
            else
            {
                FingerprintLogger.LogNoMatch($"Huella NO reconocida. Mejor score = {bestScore}, Umbral = {DEFAULT_THRESHOLD}");
                Trace.WriteLine($"[FingerprintService] Verify: NO MATCH (bestScore={bestScore}, threshold={DEFAULT_THRESHOLD}).");
                if (onNoMatchLocal != null)
                    Dispatcher.UIThread.Post(() => onNoMatchLocal());
            }
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error en OnVerificationCaptured: {ex}");
            Trace.WriteLine($"[FingerprintService] Verify error: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _verifyProcessing, 0);
            if (_verifyActive) ReArmFromHandler();
        }
    }

    #endregion

    #region Enrollment

    public Task<byte[]?> EnrollAsync(IProgress<EnrollmentProgress> progress, CancellationToken ct = default)
    {
        DetenerVerificacion();

        _enrollmentTcs = new TaskCompletionSource<byte[]?>();
        _enrollmentFmds.Clear();
        _enrollmentCount = 0;
        _enrollmentActive = true;
        _enrollmentProgress = progress;
        _enrollmentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        ct.Register(() =>
        {
            if (_enrollmentActive)
            {
                _enrollmentActive = false;
                _enrollmentTcs?.TrySetResult(null);
            }
        });

        progress.Report(new EnrollmentProgress
        {
            CaptureCount = 0,
            TotalCaptures = 4,
            Status = "Coloque su dedo en el lector (Captura 1/4)"
        });

        FingerprintLogger.LogEnrollment("=== Iniciando enrollment ===");
        Trace.WriteLine("[FingerprintService] Iniciando enrollment...");
        ArmCapture();

        return _enrollmentTcs.Task;
    }

    public void CancelarEnrollment()
    {
        _enrollmentActive = false;
        _enrollmentCts?.Cancel();
        CancelCapture();
        _enrollmentTcs?.TrySetResult(null);
        Trace.WriteLine("[FingerprintService] Enrollment cancelado.");
    }

    private void OnEnrollmentCaptured(CaptureResult result)
    {
        if (!_enrollmentActive) return;

        try
        {
            if (result.Data == null || result.ResultCode != Constants.ResultCode.DP_SUCCESS)
            {
                FingerprintLogger.LogEnrollment($"Captura invalida: {result.ResultCode}");
                Trace.WriteLine($"[FingerprintService] Enroll capture invalido: {result.ResultCode}");
                ReArmFromHandler();
                return;
            }

            var fmd = CreateFmdFromFid(result.Data);
            if (fmd == null)
            {
                FingerprintLogger.LogEnrollment("FALLA: No se pudo extraer FMD de la captura");
                Trace.WriteLine("[FingerprintService] Enroll: no se pudo extraer FMD.");
                ReArmFromHandler();
                return;
            }

            _enrollmentCount++;
            _enrollmentFmds.Add(fmd);
            FingerprintLogger.LogEnrollment($"Captura de enrollment #{_enrollmentCount}/4 exitosa");

            var img = CreateFingerprintBitmap(result.Data);

            if (_enrollmentCount >= 4)
            {
                FingerprintLogger.LogEnrollment("Procesando enrollment con 4 capturas...");
                DataResult<Fmd> enrollment = DPUruNet.Enrollment.CreateEnrollmentFmd(
                    Constants.Formats.Fmd.ANSI, _enrollmentFmds);

                _enrollmentActive = false;

                if (enrollment.ResultCode == Constants.ResultCode.DP_SUCCESS && enrollment.Data != null)
                {
                    // Usar XML para persistencia compatible con el SDK
                    var fmdXml = Fmd.SerializeXml(enrollment.Data);
                    var fmdBytes = System.Text.Encoding.UTF8.GetBytes(fmdXml);
                    FingerprintLogger.LogEnrollment($"Enrollment exitoso! FMD generado correctamente (XML bytes: {fmdBytes.Length})");
                    Trace.WriteLine("[FingerprintService] Enrollment exitoso.");
                    var prog = new EnrollmentProgress
                    {
                        CaptureCount = 4,
                        TotalCaptures = 4,
                        FingerprintImage = img,
                        Status = "Huella registrada exitosamente!",
                        IsComplete = true
                    };
                    Dispatcher.UIThread.Post(() => _enrollmentProgress?.Report(prog));
                    _enrollmentTcs?.TrySetResult(fmdBytes);
                }
                else if (enrollment.ResultCode == Constants.ResultCode.DP_ENROLLMENT_INVALID_SET)
                {
                    FingerprintLogger.LogEnrollment("Enrollment fallido: Set de capturas invalido (dedos diferentes)");
                    Trace.WriteLine("[FingerprintService] Enrollment: set invalido.");
                    var prog = new EnrollmentProgress
                    {
                        CaptureCount = 0,
                        TotalCaptures = 4,
                        FingerprintImage = img,
                        Status = "Las capturas no coinciden. Coloque el MISMO dedo e intente de nuevo.",
                        IsError = true
                    };
                    Dispatcher.UIThread.Post(() => _enrollmentProgress?.Report(prog));
                    _enrollmentTcs?.TrySetResult(null);
                }
                else
                {
                    FingerprintLogger.LogEnrollment($"Enrollment fallido: {enrollment.ResultCode}");
                    Trace.WriteLine($"[FingerprintService] Enrollment fallido: {enrollment.ResultCode}");
                    var prog = new EnrollmentProgress
                    {
                        CaptureCount = 0,
                        TotalCaptures = 4,
                        FingerprintImage = img,
                        Status = "Error en el enrollment. Intente de nuevo.",
                        IsError = true
                    };
                    Dispatcher.UIThread.Post(() => _enrollmentProgress?.Report(prog));
                    _enrollmentTcs?.TrySetResult(null);
                }
            }
            else
            {
                FingerprintLogger.LogEnrollment($"Captura #{_enrollmentCount}/4 completada. Coloque el MISMO dedo de nuevo.");
                var prog = new EnrollmentProgress
                {
                    CaptureCount = _enrollmentCount,
                    TotalCaptures = 4,
                    FingerprintImage = img,
                    Status = $"Huella capturada ({_enrollmentCount}/4). Coloque el MISMO dedo de nuevo."
                };
                Dispatcher.UIThread.Post(() => _enrollmentProgress?.Report(prog));
                ReArmFromHandler();
            }
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error en OnEnrollmentCaptured: {ex}");
            Trace.WriteLine($"[FingerprintService] Enroll error: {ex}");
            _enrollmentActive = false;
            var prog = new EnrollmentProgress
            {
                CaptureCount = 0,
                TotalCaptures = 4,
                Status = $"Error: {ex.Message}",
                IsError = true
            };
            Dispatcher.UIThread.Post(() => _enrollmentProgress?.Report(prog));
            _enrollmentTcs?.TrySetResult(null);
        }
    }

    #endregion

    #region DB operations

    public async Task<(bool exito, string mensaje)> GuardarHuellaAsync(string matricula, byte[] fmdBytes)
    {
        if (fmdBytes == null || fmdBytes.Length == 0)
            return (false, "Los datos de la huella estan vacios.");

        FingerprintLogger.LogDb($"Guardando huella para matricula: {matricula}. Bytes: {fmdBytes.Length}");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persona = await ctx.Personas.FirstOrDefaultAsync(p => p.Matricula == matricula);
            if (persona == null)
            {
                FingerprintLogger.LogError($"GuardarHuella: Persona {matricula} no encontrada");
                return (false, $"Persona con matricula {matricula} no encontrada.");
            }

            persona.Huella = fmdBytes;
            ctx.Personas.Update(persona);
            await ctx.SaveChangesAsync();

            var nombre = $"{persona.Nombre} {persona.ApellidoPaterno}";
            FingerprintLogger.LogDb($"Huella guardada OK para {nombre} ({matricula})");
            Trace.WriteLine($"[FingerprintService] Huella guardada para {nombre}.");

            // Actualizar galeria en memoria
            var fmdNuevo = ReconstructFmd(fmdBytes);
            if (fmdNuevo != null)
            {
                lock (_lock)
                {
                    var g = _gallery.ToList();
                    g.RemoveAll(x => x.matricula == matricula);
                    g.Add((matricula, fmdNuevo));
                    _gallery = g;
                }
                FingerprintLogger.LogDb($"Galeria en memoria actualizada: agregado {matricula}");
            }

            return (true, $"Huella guardada correctamente para {nombre}.");
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error guardando huella para {matricula}: {ex}");
            Trace.WriteLine($"[FingerprintService] Error guardando huella: {ex.Message}");
            return (false, $"Error al guardar la huella: {ex.Message}");
        }
    }

    public async Task<(bool exito, string mensaje)> EliminarHuellaAsync(string matricula)
    {
        FingerprintLogger.LogDb($"Eliminando huella para matricula: {matricula}");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persona = await ctx.Personas.FirstOrDefaultAsync(p => p.Matricula == matricula);
            if (persona == null)
            {
                FingerprintLogger.LogError($"EliminarHuella: Persona {matricula} no encontrada");
                return (false, $"Persona con matricula {matricula} no encontrada.");
            }

            persona.Huella = null;
            ctx.Personas.Update(persona);
            await ctx.SaveChangesAsync();

            var nombre_definition = $"{persona.Nombre} {persona.ApellidoPaterno}";
            FingerprintLogger.LogDb($"Huella eliminada OK para {nombre_definition} ({matricula})");
            Trace.WriteLine($"[FingerprintService] Huella eliminada para {nombre_definition}.");

            // Actualizar galeria en memoria
            lock (_lock)
            {
                var g = _gallery.ToList();
                g.RemoveAll(x => x.matricula == matricula);
                _gallery = g;
            }
            FingerprintLogger.LogDb($"Galeria en memoria actualizada: eliminado {matricula}");

            return (true, $"Huella eliminada correctamente para {nombre_definition}.");
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error eliminando huella para {matricula}: {ex}");
            return (false, $"Error al eliminar la huella: {ex.Message}");
        }
    }

    #endregion

    #region Reader hardware

    private Reader? GetReader()
    {
        lock (_lock) { if (_reader != null) return _reader; }

        try
        {
            var readers = ReaderCollection.GetReaders();
            if (readers != null && readers.Count > 0)
            {
                var r = readers[0];
                lock (_lock) { _reader = r; }
                Trace.WriteLine($"[FingerprintService] Reader detectado: {r.Description.Name}");
            }
            else
            {
                Trace.WriteLine("[FingerprintService] No se detectaron lectores.");
            }
            return _reader;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[FingerprintService] Error detectando lector: {ex.Message}");
            return null;
        }
    }

    private bool OpenReader()
    {
        try
        {
            lock (_lock) { if (_readerOpened && _reader != null) return true; }

            var reader = GetReader();
            if (reader == null) return false;

            lock (_lock)
            {
                var rc = _reader!.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
                Trace.WriteLine($"[FingerprintService] Open result: {rc}");
                if (rc == Constants.ResultCode.DP_SUCCESS) { _readerOpened = true; return true; }
                if (rc == Constants.ResultCode.DP_DEVICE_BUSY)
                {
                    Thread.Sleep(200);
                    rc = _reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
                    Trace.WriteLine($"[FingerprintService] Open retry result: {rc}");
                    if (rc == Constants.ResultCode.DP_SUCCESS) { _readerOpened = true; return true; }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[FingerprintService] Open error: {ex}");
            return false;
        }
    }

    private bool GetStatus()
    {
        Reader r;
        lock (_lock) { r = _reader!; }

        try
        {
            var rc = r.GetStatus();
            if (rc != Constants.ResultCode.DP_SUCCESS) return false;

            int retries = 0;
            while (r.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY && retries < 60)
            { Thread.Sleep(50); retries++; }

            if (r.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION)
            { r.Calibrate(); }

            return true;
        }
        catch { return false; }
    }

    private void CaptureFinger()
    {
        Reader r;
        lock (_lock) { r = _reader!; }
        if (r == null)
        {
            FingerprintLogger.LogHardware("CaptureFinger: lector no disponible");
            return;
        }

        try
        {
            if (!GetStatus())
            {
                FingerprintLogger.LogHardware("CaptureFinger: GetStatus fallo");
                Trace.WriteLine("[FingerprintService] CaptureFinger: GetStatus fallo.");
                return;
            }

            int resolution = r.Capabilities.Resolutions?[0] ?? 500;
            FingerprintLogger.LogHardware($"CaptureFinger: iniciando captura a {resolution}dpi");

            for (int i = 0; i < 3; i++)
            {
                var rc = r.CaptureAsync(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, resolution);
                if (rc == Constants.ResultCode.DP_SUCCESS)
                {
                    FingerprintLogger.LogHardware("CaptureFinger: captura iniciada OK");
                    Trace.WriteLine("[FingerprintService] CaptureFinger: CaptureAsync OK.");
                    return;
                }
                if (rc == Constants.ResultCode.DP_DEVICE_BUSY)
                {
                    FingerprintLogger.LogHardware($"CaptureFinger: lector ocupado, reintento {i + 1}/3");
                    Trace.WriteLine($"[FingerprintService] CaptureFinger: busy, reintentando ({i + 1}/3)...");
                    Thread.Sleep(100);
                    continue;
                }
                FingerprintLogger.LogHardware($"CaptureFinger: CaptureAsync fallo con resultado {rc}");
                Trace.WriteLine($"[FingerprintService] CaptureAsync result: {rc}");
                break;
            }
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"CaptureFinger error: {ex}");
            Trace.WriteLine($"[FingerprintService] CaptureFinger error: {ex.Message}");
        }
    }

    /// <summary>Registers the single handler once and arms capture.</summary>
    private void ArmCapture()
    {
        lock (_lock)
        {
            if (_reader == null || !_readerOpened)
            {
                Trace.WriteLine("[FingerprintService] ArmCapture: lector no disponible.");
                return;
            }

            if (!_handlerRegistered)
            {
                try { _reader.On_Captured += OnCaptured; } catch { }
                _handlerRegistered = true;
                Trace.WriteLine("[FingerprintService] ArmCapture: handler registrado.");
            }
        }

        CaptureFinger();
    }

    /// <summary>Single capture handler that dispatches to the active mode.</summary>
    private void OnCaptured(CaptureResult result)
    {
        if (_disposed) return;

        if (_enrollmentActive)
        {
            OnEnrollmentCaptured(result);
        }
        else if (_verifyActive)
        {
            OnVerificationCaptured(result);
        }
        else
        {
            Trace.WriteLine("[FingerprintService] OnCaptured: modo inactivo, ignorando.");
        }
    }

    /// <summary>Re-arms the reader from within a capture handler (SDK callback thread).</summary>
    private void ReArmFromHandler()
    {
        if (_disposed) return;
        if (!_verifyActive && !_enrollmentActive)
        {
            Trace.WriteLine("[FingerprintService] ReArmFromHandler: no hay modo activo.");
            return;
        }

        int delay = _enrollmentActive ? 500 : 300;
        Task.Delay(delay).ContinueWith(_ => CaptureFinger(), TaskScheduler.Default);
    }

    /// <summary>Cancels any pending capture.</summary>
    private void CancelCapture()
    {
        lock (_lock)
        {
            if (_reader != null)
            {
                try { _reader.CancelCapture(); } catch { }
            }
        }
    }

    #endregion

    #region FMD helpers

    private Fmd? CreateFmdFromFid(Fid fid)
    {
        if (fid == null)
        {
            FingerprintLogger.LogError("CreateFmdFromFid: FID nulo");
            return null;
        }
        try
        {
            DataResult<Fmd> r = FeatureExtraction.CreateFmdFromFid(fid, Constants.Formats.Fmd.ANSI);
            if (r.ResultCode == Constants.ResultCode.DP_SUCCESS && r.Data != null)
            {
                FingerprintLogger.LogVerify("FMD extraido correctamente del FID");
                return r.Data;
            }
            FingerprintLogger.LogError($"CreateFmdFromFid: FeatureExtraction fallo con {r.ResultCode}");
            return null;
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"CreateFmdFromFid: Excepcion - {ex.Message}");
            return null;
        }
    }

    private Fmd? ReconstructFmd(byte[] fmdBytes)
    {
        if (fmdBytes == null || fmdBytes.Length == 0)
        {
            FingerprintLogger.LogError("ReconstructFmd: Bytes nulos o vacios");
            return null;
        }

        // Reconstruccion via XML (unico metodo compatible con DPUruNet 3.4.4)
        try
        {
            var xmlString = System.Text.Encoding.UTF8.GetString(fmdBytes);
            if (!string.IsNullOrWhiteSpace(xmlString) && xmlString.TrimStart().StartsWith("<"))
            {
                var fmd = Fmd.DeserializeXml(xmlString);
                if (fmd != null)
                {
                    FingerprintLogger.LogInfo("ReconstructFmd: EXITO via DeserializeXml");
                    return fmd;
                }
            }
            else
            {
                FingerprintLogger.LogError("ReconstructFmd: Los bytes no son XML valido");
            }
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"ReconstructFmd: DeserializeXml fallo -> {ex.GetType().Name}: {ex.Message}");
        }

        return null;
    }

    public static Bitmap? CreateFingerprintBitmap(Fid fid)
    {
        if (fid?.Views == null || fid.Views.Count == 0) return null;
        var fiv = fid.Views[0];
        if (fiv.RawImage == null || fiv.RawImage.Length == 0) return null;

        int w = fiv.Width;
        int h = fiv.Height;
        var raw = fiv.RawImage;

        var wb = new WriteableBitmap(
            new PixelSize(w, h),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using (var buf = wb.Lock())
        {
            int stride = buf.RowBytes;
            byte[] pixelData = new byte[stride * h];

            for (int y = 0; y < h; y++)
            {
                int srcRow = y * w;
                int dstRow = y * stride;
                for (int x = 0; x < w; x++)
                {
                    byte gray = raw[srcRow + x];
                    int dstCol = dstRow + x * 4;
                    pixelData[dstCol] = gray;
                    pixelData[dstCol + 1] = gray;
                    pixelData[dstCol + 2] = gray;
                    pixelData[dstCol + 3] = 255;
                }
            }

            Marshal.Copy(pixelData, 0, buf.Address, pixelData.Length);
        }

        return wb;
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DetenerVerificacion();
        CancelarEnrollment();

        lock (_lock)
        {
            if (_reader != null)
            {
                try { _reader.On_Captured -= OnCaptured; } catch { }
                try { _reader.CancelCapture(); } catch { }
                try { _reader.Dispose(); } catch { }
                _reader = null;
                _readerOpened = false;
                _handlerRegistered = false;
            }
        }
    }
}