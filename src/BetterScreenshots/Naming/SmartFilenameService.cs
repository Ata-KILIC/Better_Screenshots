using System.Text;
using System.Text.RegularExpressions;
using BetterScreenshots.Capture;

namespace BetterScreenshots.Naming;

public sealed class SmartFilenameService
{
    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "for", "with", "from", "this", "that", "window", "file", "edit", "view", "help", "new", "untitled", "home", "page", "search", "to", "of", "in", "on", "at", "is", "are", "www", "http", "https", "com"
    };

    public string MakeName(CaptureContext context, string? ocrText, DateTime timestamp)
    {
        var tokens = new List<string>();
        AddTokens(tokens, context.ApplicationName, 2);
        AddTokens(tokens, WindowTitleWithoutApp(context.WindowTitle, context.ApplicationName), 3);
        AddTokens(tokens, ocrText, 4);
        var distinct = tokens.Where(x => !Noise.Contains(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToArray();
        var baseName = distinct.Length >= 2 ? string.Join('-', distinct) : Fallback(context.ApplicationName, timestamp);
        return Sanitize(baseName) + ".png";
    }

    public static string Sanitize(string candidate)
    {
        var normalized = candidate.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[<>:""/\\|?*\x00-\x1F]", " ");
        normalized = Regex.Replace(normalized, "[^\\p{L}\\p{N}]+", "-").Trim('-', '.', ' ');
        normalized = Regex.Replace(normalized, "-{2,}", "-");
        normalized = normalized.TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(normalized)) normalized = "better-screenshot";
        return normalized.Length > 96 ? normalized[..96].TrimEnd('-') : normalized;
    }

    public static string ResolveCollision(string folder, string requestedFileName)
    {
        var name = Path.GetFileNameWithoutExtension(requestedFileName);
        var extension = Path.GetExtension(requestedFileName);
        var path = Path.Combine(folder, requestedFileName);
        for (var i = 2; File.Exists(path); i++) path = Path.Combine(folder, $"{name}-{i}{extension}");
        return path;
    }

    private static string Fallback(string? app, DateTime timestamp) => $"{Sanitize(app ?? "better-screenshot")}-{timestamp:yyyy-MM-dd-HHmmss}";
    private static string WindowTitleWithoutApp(string? title, string? app) => string.IsNullOrWhiteSpace(title) ? "" : title.Replace(app ?? "", "", StringComparison.OrdinalIgnoreCase);
    private static void AddTokens(List<string> target, string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var added = 0;
        foreach (var value in Regex.Matches(text.Normalize(NormalizationForm.FormKC), "[\\p{L}\\p{N}][\\p{L}\\p{N}'_-]*").Select(m => m.Value))
        {
            var clean = Sanitize(value);
            if (clean.Length is > 1 and <= 32 && !Noise.Contains(clean)) { target.Add(clean); added++; }
            if (added >= max) break;
        }
    }
}
