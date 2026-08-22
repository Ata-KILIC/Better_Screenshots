using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using BetterScreenshots.Capture;

namespace BetterScreenshots.Notifications;

public partial class QuickPreviewWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };
    public QuickPreviewWindow(Bitmap bitmap, string? path, Action edit, Action copyText, Action openFolder, Action? copyPath, Action? openWith)
    {
        InitializeComponent();
        Thumbnail.Source = BitmapInterop.ToBitmapSource(bitmap);
        FileName.Text = path is null ? "Copied to clipboard" : Path.GetFileName(path);
        EditButton.Click += (_, _) => { edit(); Close(); };
        OcrButton.Click += (_, _) => { copyText(); Close(); };
        FolderButton.Click += (_, _) => { openFolder(); Close(); };
        SavedActions.Visibility = path is null ? Visibility.Collapsed : Visibility.Visible;
        PathButton.Click += (_, _) => { copyPath?.Invoke(); Close(); };
        OpenWithButton.Click += (_, _) => { openWith?.Invoke(); Close(); };
        CloseButton.Click += (_, _) => Close();
        _timer.Tick += (_, _) => Close();
        Loaded += (_, _) => { Left = SystemParameters.WorkArea.Right - ActualWidth - 18; Top = SystemParameters.WorkArea.Bottom - ActualHeight - 18; _timer.Start(); };
        Closed += (_, _) => _timer.Stop();
    }
}
