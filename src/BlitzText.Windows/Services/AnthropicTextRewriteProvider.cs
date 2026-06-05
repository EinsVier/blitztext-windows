using System.Net.Http;
using System.Text;
using System.Text.Json;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class AnthropicTextRewriteProvider(AppSettings settings, HttpClient httpClient) : ITextRewriteProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string DisplayName => "Anthropic";

    public async Task<string> RewriteAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AnthropicApiKey))
        {
            throw new InvalidOperationException("Anthropic API key is missing.");
        }

        var payload = new
        {
            model = settings.AnthropicRewriteModel,
            max_tokens = 4096,
            temperature = 0.2,
            system = "You are a concise writing assistant. Return only the requested final text.",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.TryAddWithoutValidation("x-api-key", settings.AnthropicApiKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic rewrite failed: {response.StatusCode} {responseText}");
        }

        using var document = JsonDocument.Parse(responseText);
        var contentItems = document.RootElement.GetProperty("content").EnumerateArray();
        foreach (var contentItem in contentItems)
        {
            if (contentItem.TryGetProperty("type", out var type)
                && type.GetString() == "text"
                && contentItem.TryGetProperty("text", out var text))
            {
                return text.GetString()?.Trim() ?? "";
            }
        }

        return "";
    }
}
