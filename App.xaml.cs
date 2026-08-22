using BetterScreenshots.Capture;
using BetterScreenshots.Clipboard;
using BetterScreenshots.Hotkeys;
using BetterScreenshots.Notifications;
using BetterScreenshots.Ocr;
using BetterScreenshots.Settings;
using BetterScreenshots.Tray;
using System.Windows;

namespace BetterScreenshots;

public partial class App : System.Windows.Application
{
    private AppController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var settings = new SettingsService();
        var notifier = new NotificationService();
        var clipboard = new ClipboardService();
        _controller = new AppController(settings, new GlobalHotkeyService(), new ScreenCaptureService(),
            new OcrService(), clipboard, notifier);
        var window = new MainWindow(_controller);
        MainWindow = window;
        _controller.Initialize(window);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        base.OnExit(e);
    }
}
