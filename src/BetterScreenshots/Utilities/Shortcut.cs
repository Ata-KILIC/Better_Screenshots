using System.Globalization;

namespace BetterScreenshots.Utilities;

[Flags]
public enum ShortcutModifiers : uint { None = 0, Alt = 0x0001, Ctrl = 0x0002, Shift = 0x0004, Win = 0x0008 }

public readonly record struct Shortcut(ShortcutModifiers Modifiers, uint VirtualKey)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(ShortcutModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ShortcutModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ShortcutModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ShortcutModifiers.Win)) parts.Add("Win");
        parts.Add(VirtualKey == 0x2C ? "PrintScreen" : ((char)VirtualKey).ToString());
        return string.Join('+', parts);
    }

    public static bool TryParse(string? input, out Shortcut shortcut, out string error)
    {
        shortcut = default;
        error = "";
        if (string.IsNullOrWhiteSpace(input)) { error = "A shortcut is required."; return false; }
        var parts = input.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) { error = "Enter a shortcut such as Shift+PrintScreen."; return false; }
        ShortcutModifiers modifiers = ShortcutModifiers.None;
        uint key = 0;
        foreach (var part in parts)
        {
            if (part.Equals("ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("control", StringComparison.OrdinalIgnoreCase)) modifiers |= ShortcutModifiers.Ctrl;
            else if (part.Equals("shift", StringComparison.OrdinalIgnoreCase)) modifiers |= ShortcutModifiers.Shift;
            else if (part.Equals("alt", StringComparison.OrdinalIgnoreCase)) modifiers |= ShortcutModifiers.Alt;
            else if (part.Equals("win", StringComparison.OrdinalIgnoreCase) || part.Equals("windows", StringComparison.OrdinalIgnoreCase)) modifiers |= ShortcutModifiers.Win;
            else if (part.Equals("printscreen", StringComparison.OrdinalIgnoreCase) || part.Equals("prtscn", StringComparison.OrdinalIgnoreCase)) key = 0x2C;
            else if (part.Length == 1 && char.IsLetterOrDigit(part[0])) key = char.ToUpperInvariant(part[0]);
            else { error = $"'{part}' is not a supported shortcut key."; return false; }
        }
        if (key == 0) { error = "Choose a key, for example PrintScreen or S."; return false; }
        if (modifiers == ShortcutModifiers.None) { error = "Use at least one modifier key to avoid interfering with normal typing."; return false; }
        if (key == 0x2C && modifiers is ShortcutModifiers.None or ShortcutModifiers.Win) { error = "Standard Windows screenshot shortcuts cannot be used."; return false; }
        shortcut = new Shortcut(modifiers, key);
        return true;
    }
}
