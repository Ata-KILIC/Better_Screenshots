using System.Text.Json.Serialization;

namespace BetterScreenshots.Settings;

public enum SaveBehavior { SaveAndClipboard, ClipboardOnly }

public sealed class AppSettings
{
    public string ScreenshotShortcut { get; set; } = "Shift+PrintScreen";
    public string OcrShortcut { get; set; } = "Ctrl+Shift+PrintScreen";
    public SaveBehavior SaveBehavior { get; set; } = SaveBehavior.SaveAndClipboard;
    public string ScreenshotDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Better Screenshots");
    public bool SmartNamesEnabled { get; set; } = true;
    public bool ShowPreview { get; set; } = true;
    [JsonIgnore] public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Better Screenshots");
    public AppSettings Copy() => (AppSettings)MemberwiseClone();
}
