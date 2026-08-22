using System.Text.Json;

namespace BetterScreenshots.Settings;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;
    public AppSettings Current { get; private set; } = new();

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterScreenshots", "settings.json");
        Current = Load();
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var parsed = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath), JsonOptions);
                if (parsed is not null)
                {
                    parsed.ScreenshotDirectory = string.IsNullOrWhiteSpace(parsed.ScreenshotDirectory) ? AppSettings.DefaultDirectory : parsed.ScreenshotDirectory;
                    return parsed;
                }
            }
        }
        catch (Exception) { /* Invalid settings should never prevent the utility starting. */ }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        var temporary = _settingsPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporary, _settingsPath, true);
        Current = settings.Copy();
    }
}
