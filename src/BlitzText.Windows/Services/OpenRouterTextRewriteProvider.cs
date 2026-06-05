using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class OpenRouterTextRewriteProvider(AppSettings settings, HttpClient httpClient) : ITextRewriteProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string DisplayName => "OpenRouter";

    public async Task<string> RewriteAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.OpenRouterApiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is missing.");
        }

        var payload = new
        {
            model = settings.OpenRouterRewriteModel,
            messages = new[]
            {
                new { role = "system", content = "You are a concise writing assistant. Return only the requested final text." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenRouterApiKey);
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://github.com/EinsVier/blitztext-windows");
        request.Headers.TryAddWithoutValidation("X-Title", "BlitzText Windows");
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenRouter rewrite failed: {response.StatusCode} {responseText}");
        }

        using var document = JsonDocument.Parse(responseText);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim() ?? "";
    }
}
