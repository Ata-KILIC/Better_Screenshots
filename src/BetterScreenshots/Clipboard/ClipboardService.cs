using System.Drawing;
using System.Windows;
using BetterScreenshots.Capture;

namespace BetterScreenshots.Clipboard;

public sealed class ClipboardService
{
    public async Task<bool> CopyImageAsync(Bitmap bitmap, CancellationToken cancellationToken = default)
    {
        var source = BitmapInterop.ToBitmapSource(bitmap);
        for (var attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => System.Windows.Clipboard.SetImage(source));
                return true;
            }
            catch (System.Runtime.InteropServices.COMException) when (attempt < 5)
            {
                await Task.Delay(65 * (attempt + 1), cancellationToken);
            }
        }
        return false;
    }

    public async Task<bool> CopyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => System.Windows.Clipboard.SetText(text, System.Windows.TextDataFormat.UnicodeText));
                return true;
            }
            catch (System.Runtime.InteropServices.COMException) when (attempt < 5)
            {
                await Task.Delay(65 * (attempt + 1), cancellationToken);
            }
        }
        return false;
    }
}
