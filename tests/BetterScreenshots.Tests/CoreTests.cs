using BetterScreenshots.Naming;
using BetterScreenshots.Settings;
using BetterScreenshots.Utilities;
using Xunit;

namespace BetterScreenshots.Tests;

public sealed class CoreTests
{
    [Theory]
    [InlineData("Shift+PrintScreen", ShortcutModifiers.Shift, 0x2C)]
    [InlineData("Ctrl + Shift + prtscn", ShortcutModifiers.Ctrl | ShortcutModifiers.Shift, 0x2C)]
    [InlineData("Alt+S", ShortcutModifiers.Alt, (uint)'S')]
    public void ShortcutParsing_NormalizesSupportedShortcuts(string source, ShortcutModifiers modifiers, uint virtualKey)
    {
        Assert.True(Shortcut.TryParse(source, out var shortcut, out _));
        Assert.Equal(modifiers, shortcut.Modifiers);
        Assert.Equal(virtualKey, shortcut.VirtualKey);
    }

    [Theory]
    [InlineData("PrintScreen", "Use at least one modifier")]
    [InlineData("Shift+F13", "not a supported")]
    [InlineData("Win+PrintScreen", "Standard Windows")]
    public void ShortcutParsing_RejectsUnsafeOrUnknownShortcuts(string source, string expectedMessage)
    {
        Assert.False(Shortcut.TryParse(source, out _, out var error));
        Assert.Contains(expectedMessage, error);
    }

    [Theory]
    [InlineData("Unity: NullReferenceException / PlayerController", "unity-nullreferenceexception-playercontroller")]
    [InlineData("  Çalışma  —  Bilkent  ", "çalışma-bilkent")]
    [InlineData("<>:\"/\\|?*", "better-screenshot")]
    public void Sanitize_MakesSafeReadableFileStems(string input, string expected)
        => Assert.Equal(expected, SmartFilenameService.Sanitize(input));

    [Fact]
    public void CollisionResolution_AppendsAnIncrementingSuffix()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BetterScreenshots-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "capture.png"), "x");
            File.WriteAllText(Path.Combine(folder, "capture-2.png"), "x");
            Assert.EndsWith("capture-3.png", SmartFilenameService.ResolveCollision(folder, "capture.png"));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void Settings_RoundTripThroughAtomicJsonFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BetterScreenshots-tests-" + Guid.NewGuid());
        var path = Path.Combine(folder, "settings.json");
        try
        {
            var original = new AppSettings { ScreenshotShortcut = "Alt+S", OcrShortcut = "Alt+T", ScreenshotDirectory = "C:\\Screens", SaveBehavior = SaveBehavior.ClipboardOnly, SmartNamesEnabled = false, ShowPreview = false };
            new SettingsService(path).Save(original);
            var restored = new SettingsService(path).Current;
            Assert.Equal("Alt+S", restored.ScreenshotShortcut);
            Assert.Equal("Alt+T", restored.OcrShortcut);
            Assert.Equal(SaveBehavior.ClipboardOnly, restored.SaveBehavior);
            Assert.False(restored.SmartNamesEnabled);
            Assert.False(restored.ShowPreview);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
