using System.IO;
using System.Text.Json;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class HistoryStore
{
    public const int MaxEntries = 50;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string historyPath;

    public HistoryStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "BlitzText");
        Directory.CreateDirectory(directory);
        historyPath = Path.Combine(directory, "history.json");
    }

    public IReadOnlyList<HistoryEntry> Load()
    {
        if (!File.Exists(historyPath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(historyPath);
            var entries = JsonSerializer.Deserialize<List<HistoryEntry>>(json, JsonOptions) ?? [];
            return entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
                .OrderByDescending(entry => entry.CreatedAt)
                .Take(MaxEntries)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<HistoryEntry> entries)
    {
        var trimmedEntries = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(MaxEntries)
            .ToList();
        var json = JsonSerializer.Serialize(trimmedEntries, JsonOptions);
        File.WriteAllText(historyPath, json);
    }
}
