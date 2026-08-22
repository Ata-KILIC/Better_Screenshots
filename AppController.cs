using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using BetterScreenshots.Capture;
using BetterScreenshots.Clipboard;
using BetterScreenshots.Editor;
using BetterScreenshots.Hotkeys;
using BetterScreenshots.Naming;
using BetterScreenshots.Notifications;
using BetterScreenshots.Ocr;
using BetterScreenshots.Settings;
using BetterScreenshots.Tray;
using BetterScreenshots.Utilities;

namespace BetterScreenshots;

public sealed class AppController : IDisposable
{
    private readonly SettingsService _settings;
    private readonly GlobalHotkeyService _hotkeys;
    private readonly ScreenCaptureService _capture;
    private readonly OcrService _ocr;
    private readonly ClipboardService _clipboard;
    private readonly NotificationService _notifications;
    private readonly SmartFilenameService _names = new();
    private TrayIconService? _tray;
    private MainWindow? _window;
    private int _capturing;
    private bool _disposed;
    public bool IsExiting { get; private set; }
    public AppSettings Settings => _settings.Current.Copy();

    public AppController(SettingsService settings, GlobalHotkeyService hotkeys, ScreenCaptureService capture, OcrService ocr, ClipboardService clipboard, NotificationService notifications)
        => (_settings, _hotkeys, _capture, _ocr, _clipboard, _notifications) = (settings, hotkeys, capture, ocr, clipboard, notifications);

    public void Initialize(MainWindow window)
    {
        _window = window;
        window.SourceInitialized += (_, _) =>
        {
            _hotkeys.Attach(new WindowInteropHelper(window).Handle);
            if (!RegisterHotkeys(_settings.Current, out var error)) Notify(error);
            _tray = new TrayIconService(() => _ = CaptureRegionAsync(false), () => _ = CaptureRegionAsync(true), () => _ = CaptureWholeAsync(false), () => _ = CaptureActiveWindowAsync(), DelayCapture, OpenScreenshotsFolder, ShowSettings, Exit);
        };
    }

    public bool TrySaveSettings(AppSettings proposed, out string error)
    {
        if (!BetterScreenshots.Utilities.Shortcut.TryParse(proposed.ScreenshotShortcut, out var capture, out error) || !BetterScreenshots.Utilities.Shortcut.TryParse(proposed.OcrShortcut, out var ocr, out error)) return true;
        if (capture == ocr) { error = "The two shortcuts must be different."; return true; }
        proposed.ScreenshotShortcut = capture.ToString(); proposed.OcrShortcut = ocr.ToString();
        if (string.IsNullOrWhiteSpace(proposed.ScreenshotDirectory)) proposed.ScreenshotDirectory = AppSettings.DefaultDirectory;
        if (!RegisterHotkeys(proposed, out error)) return true;
        try { _settings.Save(proposed); error = ""; return false; }
        catch (Exception ex) { error = "Could not save settings: " + ex.Message; return true; }
    }

    private bool RegisterHotkeys(AppSettings candidate, out string error)
    {
        error = "";
        if (!BetterScreenshots.Utilities.Shortcut.TryParse(candidate.ScreenshotShortcut, out var capture, out error) || !BetterScreenshots.Utilities.Shortcut.TryParse(candidate.OcrShortcut, out var ocr, out error)) return false;
        if (capture == ocr) { error = "The two shortcuts must be different."; return false; }
        try { _hotkeys.Register(capture, ocr); _hotkeys.CapturePressed -= CaptureHotkey; _hotkeys.OcrPressed -= OcrHotkey; _hotkeys.CapturePressed += CaptureHotkey; _hotkeys.OcrPressed += OcrHotkey; return true; }
        catch (Exception ex)
        {
            error = "Could not register the shortcut: " + ex.Message;
            try
            {
                if (!ReferenceEquals(candidate, _settings.Current) && BetterScreenshots.Utilities.Shortcut.TryParse(_settings.Current.ScreenshotShortcut, out var oldCapture, out _) && BetterScreenshots.Utilities.Shortcut.TryParse(_settings.Current.OcrShortcut, out var oldOcr, out _)) _hotkeys.Register(oldCapture, oldOcr);
            }
            catch { }
            return false;
        }
    }
    private async void CaptureHotkey(object? sender, EventArgs e) => await CaptureRegionAsync(false);
    private async void OcrHotkey(object? sender, EventArgs e) => await CaptureRegionAsync(true);

    public async Task CaptureRegionAsync(bool ocrOnly)
    {
        if (Interlocked.CompareExchange(ref _capturing, 1, 0) != 0) return;
        try
        {
            using var snapshot = _capture.CaptureDesktop();
            var selection = await RegionCaptureOverlay.SelectAsync(snapshot);
            if (selection is null) return;
            using var image = ScreenCaptureService.Extract(snapshot, selection.Value);
            if (ocrOnly) await CopyTextAsync(image);
            else await CompleteScreenshotAsync(image, snapshot.Context);
        }
        catch (Exception ex) { Notify("Capture failed: " + ex.Message); }
        finally { Interlocked.Exchange(ref _capturing, 0); }
    }

    private async Task CaptureWholeAsync(bool ocrOnly)
    {
        if (Interlocked.CompareExchange(ref _capturing, 1, 0) != 0) return;
        try { using var snapshot = _capture.CaptureFullscreen(); using var image = (Bitmap)snapshot.Bitmap.Clone(); if (ocrOnly) await CopyTextAsync(image); else await CompleteScreenshotAsync(image, snapshot.Context); }
        catch (Exception ex) { Notify("Capture failed: " + ex.Message); }
        finally { Interlocked.Exchange(ref _capturing, 0); }
    }

    private async Task CaptureActiveWindowAsync()
    {
        if (Interlocked.CompareExchange(ref _capturing, 1, 0) != 0) return;
        try { using var snapshot = _capture.CaptureActiveWindow(); using var image = (Bitmap)snapshot.Bitmap.Clone(); await CompleteScreenshotAsync(image, snapshot.Context); }
        catch (Exception ex) { Notify("Active-window capture failed: " + ex.Message); }
        finally { Interlocked.Exchange(ref _capturing, 0); }
    }

    private async Task CompleteScreenshotAsync(Bitmap image, CaptureContext context)
    {
        if (!await _clipboard.CopyImageAsync(image)) Notify("Image captured, but another app is holding the clipboard.");
        string? ocrText = null;
        if (_settings.Current.SmartNamesEnabled)
        {
            var ocr = await Task.Run(() => _ocr.ReadAsync(image).GetAwaiter().GetResult());
            ocrText = ocr.Text;
        }
        string? path = null;
        if (_settings.Current.SaveBehavior == SaveBehavior.SaveAndClipboard)
        {
            try { path = await Task.Run(() => SaveImage(image, context, ocrText)); }
            catch (Exception ex) { Notify("Copied to clipboard, but could not save: " + ex.Message); }
        }
        if (path is not null) Notify("Screenshot saved"); else if (_settings.Current.SaveBehavior == SaveBehavior.ClipboardOnly) Notify("Screenshot copied");
        if (_settings.Current.ShowPreview)
        {
            var bytes = ToPng(image);
            _notifications.ShowPreview(image, path, () => OpenEditor(FromPng(bytes), path), () => _ = CopyTextFromPngAsync(bytes), OpenScreenshotsFolder, path is null ? null : () => _ = _clipboard.CopyTextAsync(path), path is null ? null : () => OpenWith(path));
        }
    }

    private async Task CopyTextAsync(Bitmap image)
    {
        var result = await Task.Run(() => _ocr.ReadAsync(image).GetAwaiter().GetResult());
        if (!result.Available) { Notify(result.Error ?? "Windows OCR is unavailable."); return; }
        if (string.IsNullOrWhiteSpace(result.Text)) { Notify("No text detected"); return; }
        Notify(await _clipboard.CopyTextAsync(result.Text) ? "Text copied" : "Another app is holding the clipboard.");
    }
    private async Task CopyTextFromPngAsync(byte[] bytes)
    {
        using var image = FromPng(bytes);
        await CopyTextAsync(image);
    }

    private string SaveImage(Bitmap image, CaptureContext context, string? ocrText)
    {
        var options = _settings.Current; Directory.CreateDirectory(options.ScreenshotDirectory);
        var name = options.SmartNamesEnabled ? _names.MakeName(context, ocrText, DateTime.Now) : $"screenshot-{DateTime.Now:yyyy-MM-dd-HHmmss}.png";
        var destination = SmartFilenameService.ResolveCollision(options.ScreenshotDirectory, name);
        image.Save(destination, ImageFormat.Png);
        return destination;
    }

    private void OpenEditor(Bitmap image, string? originalPath)
    {
        var editor = new AnnotationWindow(image, flattened => SaveEdited(flattened, originalPath)) { Owner = _window };
        editor.Show(); image.Dispose();
    }
    private void SaveEdited(Bitmap image, string? originalPath)
    {
        try { if (originalPath is not null) image.Save(originalPath, ImageFormat.Png); _ = _clipboard.CopyImageAsync(image); Notify(originalPath is null ? "Edited screenshot copied" : "Edited screenshot saved and copied"); }
        catch (Exception ex) { Notify("Could not save edit: " + ex.Message); }
    }
    private static byte[] ToPng(Bitmap bitmap) { using var stream = new MemoryStream(); bitmap.Save(stream, ImageFormat.Png); return stream.ToArray(); }
    private static Bitmap FromPng(byte[] data) { using var stream = new MemoryStream(data); using var source = new Bitmap(stream); return new Bitmap(source); }

    private async void DelayCapture(int seconds)
    {
        Notify($"Capturing in {seconds} seconds"); await Task.Delay(TimeSpan.FromSeconds(seconds)); await CaptureRegionAsync(false);
    }
    public void OpenScreenshotsFolder()
    {
        try { Directory.CreateDirectory(_settings.Current.ScreenshotDirectory); Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_settings.Current.ScreenshotDirectory}\"") { UseShellExecute = true }); }
        catch (Exception ex) { Notify("Could not open folder: " + ex.Message); }
    }
    private void OpenWith(string path) => Process.Start(new ProcessStartInfo("rundll32.exe", $"shell32.dll,OpenAs_RunDLL \"{path}\"") { UseShellExecute = true });
    public void ShowSettings()
    {
        if (_window is null) return; if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal; _window.Show(); _window.Activate();
    }
    public void Notify(string message) => _notifications.Show(message);
    public void Exit() { if (IsExiting) return; IsExiting = true; System.Windows.Application.Current.Shutdown(); }
    public void Dispose() { if (_disposed) return; _disposed = true; _tray?.Dispose(); _hotkeys.Dispose(); }
}
