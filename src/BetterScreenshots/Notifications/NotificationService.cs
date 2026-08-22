using System.Drawing;
using System.Windows;

namespace BetterScreenshots.Notifications;

public sealed class NotificationService
{
    public void Show(string message, int milliseconds = 1800)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var toast = new ToastWindow(message);
            toast.Show();
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            timer.Tick += (_, _) => { timer.Stop(); toast.Close(); };
            timer.Start();
        });
    }

    public void ShowPreview(Bitmap bitmap, string? path, Action edit, Action ocr, Action folder, Action? copyPath, Action? openWith)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => new QuickPreviewWindow(bitmap, path, edit, ocr, folder, copyPath, openWith).Show());
    }
}
