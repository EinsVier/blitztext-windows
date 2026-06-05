using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class PromptContextBuilder
{
    public static string BuildTranscriptionPrompt(AppSettings settings)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(settings.TranscriptionPrompt))
        {
            parts.Add(settings.TranscriptionPrompt.Trim());
        }

        var names = FormatCustomNames(settings.CustomNames);
        if (!string.IsNullOrWhiteSpace(names))
        {
            parts.Add($"Achte besonders auf diese Eigennamen und Schreibweisen: {names}");
        }

        return string.Join(Environment.NewLine, parts);
    }

    public static string BuildRewriteContext(AppSettings settings)
    {
        var names = FormatCustomNames(settings.CustomNames);
        return string.IsNullOrWhiteSpace(names)
            ? ""
            : $"Achte besonders auf diese Eigennamen und Schreibweisen: {names}";
    }

    private static string FormatCustomNames(string customNames)
    {
        var names = customNames
            .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return names.Length == 0 ? "" : string.Join(", ", names);
    }
}
