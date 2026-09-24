using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ChecadorSSSD.Services;

/// <summary>
/// Logger thread-safe para depuracion del lector de huellas.
/// Escribe en %LOCALAPPDATA%/ChecadorSSSD/logs/huellas.log
/// </summary>
public static class FingerprintLogger
{
    private static readonly object _lock = new();
    private static readonly string _logPath;
    private static string AppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    static FingerprintLogger()
    {
        var dir = Path.Combine(AppDataPath, "ChecadorSSSD", "logs");
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch { }

        _logPath = Path.Combine(dir, "huellas.log");
    }

    public static void Log(string action, string result)
    {
        var timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
        var line = $"[{timestamp}] [{action}] {result}";

        // Traceline para consola/debug
        Trace.WriteLine(line);

        // Escribir a archivo con lock para thread-safety
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[FingerprintLogger] Error escribiendo log: {ex.Message}");
            }
        }
    }

    public static void LogInfo(string message)
    {
        Log("INFO", message);
    }

    public static void LogError(string message)
    {
        Log("ERROR", message);
    }

    public static void LogVerify(string message)
    {
        Log("VERIFY", message);
    }

    public static void LogEnrollment(string message)
    {
        Log("ENROLL", message);
    }

    public static void LogHardware(string message)
    {
        Log("HARDWARE", message);
    }

    public static void LogDb(string message)
    {
        Log("DB", message);
    }

    public static void LogMatch(string message)
    {
        Log("MATCH", message);
    }

    public static void LogNoMatch(string message)
    {
        Log("NOMATCH", message);
    }
}