using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace ChecadorSSSD.Services;

/// <summary>
/// Helper para operaciones de clipboard mediante P/Invoke (Windows).
/// Garantiza compatibilidad independientemente de la versión de Avalonia.
/// </summary>
public static class ClipboardHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalUnlock(IntPtr hMem);

    const uint GMEM_MOVEABLE = 0x0002;
    const uint CF_UNICODETEXT = 13;

    /// <summary>
    /// Copia el texto al portapapeles de Windows en formato Unicode.
    /// </summary>
    public static async Task SetTextAsync(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;

        await Task.Run(() =>
        {
            if (!OpenClipboard(IntPtr.Zero)) return;
            try
            {
                if (!EmptyClipboard()) return;

                var buffer = Encoding.Unicode.GetBytes(text + '\0');
                var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)buffer.Length);
                if (hGlobal == IntPtr.Zero) return;

                var ptr = GlobalLock(hGlobal);
                if (ptr == IntPtr.Zero) return;

                try
                {
                    Marshal.Copy(buffer, 0, ptr, buffer.Length);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                SetClipboardData(CF_UNICODETEXT, hGlobal);
            }
            finally
            {
                CloseClipboard();
            }
        });
    }
}
