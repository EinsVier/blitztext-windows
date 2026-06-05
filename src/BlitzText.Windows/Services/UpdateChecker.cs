using System.Net.Http;
using System.Text.Json;

namespace BlitzText.Windows.Services;

public sealed class UpdateChecker(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<UpdateCheckResult> CheckAsync(string manifestUrl, string currentVersion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            throw new InvalidOperationException("Update manifest URL is not configured.");
        }

        var current = ParseVersion(currentVersion);
        using var response = await httpClient.GetAsync(manifestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Update manifest is empty.");

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("Update manifest does not contain a version.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Url))
        {
            throw new InvalidOperationException("Update manifest does not contain a download URL.");
        }

        var latest = ParseVersion(manifest.Version);
        return new UpdateCheckResult(
            latest > current,
            current,
            latest,
            manifest.Url,
            string.IsNullOrWhiteSpace(manifest.ReleaseNotes) ? manifest.NotesUrl : manifest.ReleaseNotes);
    }

    private static Version ParseVersion(string value)
    {
        var normalized = value.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        return Version.TryParse(normalized, out var version)
            ? version
            : throw new InvalidOperationException($"Invalid version: {value}");
    }

    private sealed class UpdateManifest
    {
        public string Version { get; init; } = "";

        public string Url { get; init; } = "";

        public string NotesUrl { get; init; } = "";

        public string ReleaseNotes { get; init; } = "";
    }
}

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    Version CurrentVersion,
    Version LatestVersion,
    string DownloadUrl,
    string? ReleaseNotes);
