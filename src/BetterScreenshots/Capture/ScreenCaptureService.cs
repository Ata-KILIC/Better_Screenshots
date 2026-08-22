using System.Drawing;
using System.Drawing.Imaging;
using BetterScreenshots.Utilities;

namespace BetterScreenshots.Capture;

public sealed record CaptureContext(nint WindowHandle, string ApplicationName, string WindowTitle, RECT ActiveWindowBounds);
public sealed record ScreenSnapshot(Bitmap Bitmap, RECT VirtualBounds, CaptureContext Context) : IDisposable { public void Dispose() => Bitmap.Dispose(); }

public sealed class ScreenCaptureService
{
    public ScreenSnapshot CaptureDesktop()
    {
        var context = CurrentContext();
        var virtualBounds = NativeMethods.VirtualScreen;
        var bitmap = new Bitmap(virtualBounds.Width, virtualBounds.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(virtualBounds.Left, virtualBounds.Top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
        return new ScreenSnapshot(bitmap, virtualBounds, context);
    }

    public ScreenSnapshot CaptureActiveWindow()
    {
        var context = CurrentContext();
        var bounds = context.ActiveWindowBounds.Intersect(NativeMethods.VirtualScreen);
        if (bounds.IsEmpty) throw new InvalidOperationException("The active window is no longer visible.");
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
        return new ScreenSnapshot(bitmap, bounds, context);
    }

    public ScreenSnapshot CaptureFullscreen()
    {
        var snapshot = CaptureDesktop();
        return snapshot;
    }

    public static Bitmap Extract(ScreenSnapshot snapshot, RECT screenRect)
    {
        var clipped = screenRect.Intersect(snapshot.VirtualBounds);
        if (clipped.IsEmpty) throw new InvalidOperationException("Select a non-empty part of the screen.");
        var source = new Rectangle(clipped.Left - snapshot.VirtualBounds.Left, clipped.Top - snapshot.VirtualBounds.Top, clipped.Width, clipped.Height);
        return snapshot.Bitmap.Clone(source, PixelFormat.Format32bppPArgb);
    }

    private static CaptureContext CurrentContext()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        return new CaptureContext(hwnd, NativeMethods.GetApplicationName(hwnd), NativeMethods.GetTitle(hwnd), NativeMethods.GetExtendedFrameBounds(hwnd));
    }
}
