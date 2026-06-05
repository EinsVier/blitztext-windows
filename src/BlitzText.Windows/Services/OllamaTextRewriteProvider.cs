using System.Net.Http;
using System.Text;
using System.Text.Json;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class OllamaTextRewriteProvider(AppSettings settings, HttpClient httpClient) : ITextRewriteProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string DisplayName => "Ollama";

    public async Task<string> RewriteAsync(string prompt, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = settings.OllamaRewriteModel,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = "You are a concise writing assistant. Return only the requested final text." },
                new { role = "user", content = prompt }
            }
        };

        var endpoint = $"{settings.OllamaBaseUrl}/api/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Ollama rewrite failed: {response.StatusCode} {responseText}");
        }

        using var document = JsonDocument.Parse(responseText);
        return document.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim() ?? "";
    }
}
