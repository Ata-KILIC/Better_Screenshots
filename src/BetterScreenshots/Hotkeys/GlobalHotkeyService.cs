using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using BetterScreenshots.Utilities;

namespace BetterScreenshots.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int CaptureId = 1001, OcrId = 1002;
    private nint _handle;
    private HwndSource? _source;
    public event EventHandler? CapturePressed;
    public event EventHandler? OcrPressed;

    public void Attach(nint windowHandle)
    {
        _handle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(WndProc);
    }

    public void Register(BetterScreenshots.Utilities.Shortcut capture, BetterScreenshots.Utilities.Shortcut ocr)
    {
        UnregisterAll();
        if (!NativeMethods.RegisterHotKey(_handle, CaptureId, (uint)capture.Modifiers, capture.VirtualKey)) throw new Win32Exception(Marshal.GetLastWin32Error(), $"{capture} is already in use by another application.");
        if (!NativeMethods.RegisterHotKey(_handle, OcrId, (uint)ocr.Modifiers, ocr.VirtualKey))
        {
            NativeMethods.UnregisterHotKey(_handle, CaptureId);
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{ocr} is already in use by another application.");
        }
    }

    public void UnregisterAll()
    {
        if (_handle == 0) return;
        NativeMethods.UnregisterHotKey(_handle, CaptureId);
        NativeMethods.UnregisterHotKey(_handle, OcrId);
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != NativeMethods.WmHotkey) return 0;
        handled = true;
        if (wParam.ToInt32() == CaptureId) CapturePressed?.Invoke(this, EventArgs.Empty);
        else if (wParam.ToInt32() == OcrId) OcrPressed?.Invoke(this, EventArgs.Empty);
        return 0;
    }

    public void Dispose() { UnregisterAll(); if (_source is not null) _source.RemoveHook(WndProc); }
}
