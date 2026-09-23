using System.Runtime.InteropServices;

namespace EyeReminder;

/// <summary>
/// Interop needed to turn a regular WPF window into a click-through, never-focused overlay.
/// </summary>
internal static class Win32
{
    public const int GWL_EXSTYLE = -20;

    public const int WS_EX_TRANSPARENT = 0x00000020; // clicks pass through to whatever is underneath
    public const int WS_EX_TOOLWINDOW  = 0x00000080; // hidden from Alt+Tab and the taskbar
    public const int WS_EX_LAYERED     = 0x00080000; // required for per-window alpha
    public const int WS_EX_NOACTIVATE  = 0x08000000; // never takes focus, even when shown

    public static readonly IntPtr HWND_TOPMOST = new(-1);

    public const uint SWP_NOSIZE       = 0x0001;
    public const uint SWP_NOMOVE       = 0x0002;
    public const uint SWP_NOACTIVATE   = 0x0010;
    public const uint SWP_SHOWWINDOW   = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Adds the extended styles that make the window invisible to the mouse and to the focus system.
    /// </summary>
    public static void MakeClickThroughOverlay(IntPtr hWnd)
    {
        var current = (long)GetWindowLongPtr(hWnd, GWL_EXSTYLE);
        var updated = current | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
        SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(updated));
    }

    /// <summary>
    /// Paints the standard title bar dark so the settings dialog does not get a white bar
    /// on top of dark content. Silently ignored on builds that do not support the attribute.
    /// </summary>
    public static void UseDarkTitleBar(IntPtr hWnd)
    {
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        var enabled = 1;

        try
        {
            DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // Older Windows without dwmapi: the light title bar is cosmetic, not fatal.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);

    /// <summary>
    /// Moves the window to an absolute position in physical pixels without ever activating it.
    /// Going through SetWindowPos instead of Window.Left/Top keeps multi-monitor mixed-DPI setups honest.
    /// </summary>
    public static void PlaceTopMost(IntPtr hWnd, int x, int y)
    {
        SetWindowPos(hWnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    /// <summary>
    /// Time since the last keyboard or mouse input anywhere in the session.
    /// Note this is input-based: watching a video without touching anything counts as idle.
    /// </summary>
    public static TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };

        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        // Both are GetTickCount values, so unchecked subtraction handles the ~49-day wraparound.
        var elapsed = unchecked((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(elapsed);
    }
}
