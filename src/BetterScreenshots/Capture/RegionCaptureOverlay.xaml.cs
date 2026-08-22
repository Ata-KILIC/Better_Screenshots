using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BetterScreenshots.Utilities;
using DrawingBitmap = System.Drawing.Bitmap;

namespace BetterScreenshots.Capture;

public partial class RegionCaptureOverlay : Window
{
    private readonly ScreenSnapshot _snapshot;
    private readonly TaskCompletionSource<RECT?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private System.Windows.Point _start;
    private System.Windows.Point _current;
    private bool _selecting;

    private RegionCaptureOverlay(ScreenSnapshot snapshot)
    {
        InitializeComponent();
        _snapshot = snapshot;
        SnapshotImage.Source = BitmapInterop.ToBitmapSource(snapshot.Bitmap);
        Loaded += (_, _) => { Focus(); Keyboard.Focus(Root); }; 
        SourceInitialized += (_, _) => PositionOverVirtualDesktop();
        Closed += (_, _) => _completion.TrySetResult(null);
    }

    public static Task<RECT?> SelectAsync(ScreenSnapshot snapshot)
    {
        var overlay = new RegionCaptureOverlay(snapshot);
        overlay.Show();
        return overlay._completion.Task;
    }

    private void PositionOverVirtualDesktop()
    {
        var bounds = _snapshot.VirtualBounds;
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(handle, 0, bounds.Left, bounds.Top, bounds.Width, bounds.Height, 0x0040); // SWP_SHOWWINDOW
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = _current = e.GetPosition(OverlayCanvas);
        _selecting = true;
        CaptureMouse();
        UpdateSelection();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _current = e.GetPosition(OverlayCanvas);
        if (_selecting) UpdateSelection();
        UpdateMagnifier(_current);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_selecting) return;
        _selecting = false;
        ReleaseMouseCapture();
        var rect = Selection;
        if (rect.Width < 2 || rect.Height < 2) { ResetSelection(); return; }
        _completion.TrySetResult(ToScreenRect(rect));
        Close();
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { _completion.TrySetResult(null); Close(); }
    }

    private Rect Selection => new(Math.Min(_start.X, _current.X), Math.Min(_start.Y, _current.Y), Math.Abs(_current.X - _start.X), Math.Abs(_current.Y - _start.Y));
    private RECT ToScreenRect(Rect rect)
    {
        var scaleX = _snapshot.VirtualBounds.Width / Math.Max(1.0, OverlayCanvas.ActualWidth);
        var scaleY = _snapshot.VirtualBounds.Height / Math.Max(1.0, OverlayCanvas.ActualHeight);
        var left = _snapshot.VirtualBounds.Left + (int)Math.Floor(rect.Left * scaleX);
        var top = _snapshot.VirtualBounds.Top + (int)Math.Floor(rect.Top * scaleY);
        var right = _snapshot.VirtualBounds.Left + (int)Math.Ceiling(rect.Right * scaleX);
        var bottom = _snapshot.VirtualBounds.Top + (int)Math.Ceiling(rect.Bottom * scaleY);
        return new RECT(left, top, right, bottom).Intersect(_snapshot.VirtualBounds);
    }

    private void UpdateSelection()
    {
        var rect = Selection;
        var width = OverlayCanvas.ActualWidth;
        var height = OverlayCanvas.ActualHeight;
        SelectionBorder.Visibility = Dimensions.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, rect.Left); Canvas.SetTop(SelectionBorder, rect.Top);
        SelectionBorder.Width = rect.Width; SelectionBorder.Height = rect.Height;
        Canvas.SetLeft(ShadeTop, 0); Canvas.SetTop(ShadeTop, 0); ShadeTop.Width = width; ShadeTop.Height = rect.Top;
        Canvas.SetLeft(ShadeLeft, 0); Canvas.SetTop(ShadeLeft, rect.Top); ShadeLeft.Width = rect.Left; ShadeLeft.Height = rect.Height;
        Canvas.SetLeft(ShadeRight, rect.Right); Canvas.SetTop(ShadeRight, rect.Top); ShadeRight.Width = Math.Max(0, width - rect.Right); ShadeRight.Height = rect.Height;
        Canvas.SetLeft(ShadeBottom, 0); Canvas.SetTop(ShadeBottom, rect.Bottom); ShadeBottom.Width = width; ShadeBottom.Height = Math.Max(0, height - rect.Bottom);
        var raw = ToScreenRect(rect);
        Dimensions.Text = $"{raw.Width} × {raw.Height}";
        Canvas.SetLeft(Dimensions, Math.Min(Math.Max(0, rect.Left), Math.Max(0, width - Dimensions.ActualWidth - 2)));
        Canvas.SetTop(Dimensions, rect.Top > 26 ? rect.Top - 26 : rect.Bottom + 5);
    }

    private void UpdateMagnifier(System.Windows.Point point)
    {
        if (_snapshot.Bitmap.Width < 4 || _snapshot.Bitmap.Height < 4) return;
        var raw = ToScreenRect(new Rect(point.X, point.Y, 1, 1));
        var x = Math.Clamp(raw.Left - _snapshot.VirtualBounds.Left - 13, 0, Math.Max(0, _snapshot.Bitmap.Width - 26));
        var y = Math.Clamp(raw.Top - _snapshot.VirtualBounds.Top - 13, 0, Math.Max(0, _snapshot.Bitmap.Height - 26));
        MagnifierImage.Source = new CroppedBitmap((BitmapSource)SnapshotImage.Source, new Int32Rect(x, y, Math.Min(26, _snapshot.Bitmap.Width - x), Math.Min(26, _snapshot.Bitmap.Height - y)));
        Magnifier.Visibility = Visibility.Visible;
        Canvas.SetLeft(Magnifier, Math.Min(Math.Max(0, point.X + 18), Math.Max(0, OverlayCanvas.ActualWidth - Magnifier.Width)));
        Canvas.SetTop(Magnifier, point.Y > 150 ? point.Y - Magnifier.Height - 18 : point.Y + 18);
    }

    private void ResetSelection()
    {
        SelectionBorder.Visibility = Dimensions.Visibility = Visibility.Collapsed;
        ShadeTop.Width = ShadeTop.Height = ShadeLeft.Width = ShadeLeft.Height = ShadeRight.Width = ShadeRight.Height = ShadeBottom.Width = ShadeBottom.Height = 0;
    }
}

internal static class BitmapInterop
{
    [System.Runtime.InteropServices.DllImport("gdi32.dll")] private static extern bool DeleteObject(nint hObject);
    public static BitmapSource ToBitmapSource(DrawingBitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(handle, nint.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally { DeleteObject(handle); }
    }
}
