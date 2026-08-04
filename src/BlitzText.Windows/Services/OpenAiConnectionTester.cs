using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;

namespace BlitzText.Windows.Services;

public sealed class OpenAiConnectionTester(HttpClient httpClient)
{
    public async Task<string> TestAsync(string apiKey, string transcriptionModel, string rewriteModel, bool english, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(english ? "OpenAI API key is missing." : "OpenAI API-Key fehlt.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI: {response.StatusCode} {responseText}");
        }

        using var document = JsonDocument.Parse(responseText);
        var models = document.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var transcriptionFound = models.Contains(transcriptionModel);
        var rewriteFound = models.Contains(rewriteModel);
        return english
            ? $"OpenAI reachable. Transcription model {(transcriptionFound ? "found" : "not listed")}; rewrite model {(rewriteFound ? "found" : "not listed")}."
            : $"OpenAI erreichbar. Transkriptionsmodell {(transcriptionFound ? "gefunden" : "nicht gelistet")}; Rewrite-Modell {(rewriteFound ? "gefunden" : "nicht gelistet")}.";
    }
}
