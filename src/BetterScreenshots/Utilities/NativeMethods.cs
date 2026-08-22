using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BetterScreenshots.Utilities;

internal static class NativeMethods
{
    public const int WmHotkey = 0x0312;
    public const int GwlExStyle = -20;
    public const int WsExToolWindow = 0x00000080;
    public const int WsExNoActivate = 0x08000000;
    public const int SwRestore = 9;
    public const uint MonitorDefaultToNearest = 2;
    public const int DwmwaExtendedFrameBounds = 9;

    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool UnregisterHotKey(nint hWnd, int id);
    [DllImport("user32.dll")] public static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(nint hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool IsIconic(nint hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(nint hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(nint hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(nint hWnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] public static extern nint MonitorFromWindow(nint hwnd, uint flags);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool GetMonitorInfo(nint monitor, ref MONITORINFO info);
    [DllImport("user32.dll", SetLastError = true)] public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint value);
    [DllImport("user32.dll", SetLastError = true)] public static extern nint GetWindowLongPtr(nint hWnd, int nIndex);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool SetWindowPos(nint hWnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("dwmapi.dll", PreserveSig = true)] public static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out RECT value, int size);

    public static RECT VirtualScreen => new(GetSystemMetrics(76), GetSystemMetrics(77), GetSystemMetrics(76) + GetSystemMetrics(78), GetSystemMetrics(77) + GetSystemMetrics(79));
    public static string GetTitle(nint hwnd)
    {
        var buffer = new StringBuilder(512); GetWindowText(hwnd, buffer, buffer.Capacity); return buffer.ToString();
    }
    public static string GetApplicationName(nint hwnd)
    {
        try { GetWindowThreadProcessId(hwnd, out var id); return Process.GetProcessById((int)id).ProcessName; } catch { return ""; }
    }
    public static RECT GetExtendedFrameBounds(nint hwnd)
    {
        return DwmGetWindowAttribute(hwnd, DwmwaExtendedFrameBounds, out var rect, Marshal.SizeOf<RECT>()) == 0 ? rect : GetWindowRect(hwnd, out rect) ? rect : default;
    }
}

[StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X; public int Y; public POINT(int x, int y) { X = x; Y = y; } }
[StructLayout(LayoutKind.Sequential)] public struct RECT
{
    public int Left; public int Top; public int Right; public int Bottom;
    public RECT(int left, int top, int right, int bottom) { Left = left; Top = top; Right = right; Bottom = bottom; }
    public int Width => Right - Left; public int Height => Bottom - Top;
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public RECT Intersect(RECT other) => new(Math.Max(Left, other.Left), Math.Max(Top, other.Top), Math.Min(Right, other.Right), Math.Min(Bottom, other.Bottom));
}
[StructLayout(LayoutKind.Sequential)] internal struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
