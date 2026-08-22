using System.ComponentModel;
using System.Windows;
using BetterScreenshots.Settings;

namespace BetterScreenshots;

public partial class MainWindow : Window
{
    private readonly AppController _controller;
    public MainWindow(AppController controller)
    {
        InitializeComponent();
        _controller = controller;
        LoadSettings(controller.Settings);
        Closing += WindowClosing;
    }
    private void LoadSettings(AppSettings settings)
    {
        ScreenshotShortcut.Text = settings.ScreenshotShortcut; OcrShortcut.Text = settings.OcrShortcut; ScreenshotDirectory.Text = settings.ScreenshotDirectory;
        SaveAndClipboard.IsChecked = settings.SaveBehavior == SaveBehavior.SaveAndClipboard; ClipboardOnly.IsChecked = settings.SaveBehavior == SaveBehavior.ClipboardOnly;
        SmartNames.IsChecked = settings.SmartNamesEnabled; ShowPreview.IsChecked = settings.ShowPreview;
    }
    private void SaveClick(object sender, RoutedEventArgs e)
    {
        var result = new AppSettings { ScreenshotShortcut = ScreenshotShortcut.Text, OcrShortcut = OcrShortcut.Text, ScreenshotDirectory = ScreenshotDirectory.Text.Trim(), SaveBehavior = ClipboardOnly.IsChecked == true ? SaveBehavior.ClipboardOnly : SaveBehavior.SaveAndClipboard, SmartNamesEnabled = SmartNames.IsChecked == true, ShowPreview = ShowPreview.IsChecked == true };
        if (_controller.TrySaveSettings(result, out var error)) System.Windows.MessageBox.Show(this, error, "Better Screenshots", MessageBoxButton.OK, MessageBoxImage.Warning);
        else _controller.Notify("Settings saved");
    }
    private void BrowseClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Choose where Better Screenshots saves images", SelectedPath = ScreenshotDirectory.Text };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) ScreenshotDirectory.Text = dialog.SelectedPath;
    }
    private void OpenFolderClick(object sender, RoutedEventArgs e) => _controller.OpenScreenshotsFolder();
    private void WindowClosing(object? sender, CancelEventArgs e) { if (!_controller.IsExiting) _controller.Exit(); }
}
