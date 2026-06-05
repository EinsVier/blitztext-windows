using System.IO;
using System.Text.Json;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string settingsPath;

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "BlitzText");
        Directory.CreateDirectory(directory);
        settingsPath = Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(settingsPath, json);
    }

    public void Export(AppSettings settings, string exportPath)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(exportPath, json);
    }

    public AppSettings Import(string importPath)
    {
        var json = File.ReadAllText(importPath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    public string ReadLegacyOpenAiApiKey()
    {
        if (!File.Exists(settingsPath))
        {
            return "";
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (document.RootElement.TryGetProperty("OpenAiApiKey", out var apiKeyElement))
            {
                return apiKeyElement.GetString() ?? "";
            }
        }
        catch
        {
            return "";
        }

        return "";
    }
}
