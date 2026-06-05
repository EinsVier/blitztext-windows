using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BlitzText.Windows.Services;

public sealed class OllamaConnectionTester
{
    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    public async Task<string> TestAsync(string baseUrl, string model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "Ollama URL fehlt.";
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return "Ollama Modell fehlt.";
        }

        var endpoint = $"{baseUrl.TrimEnd('/')}/api/tags";
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return $"Ollama antwortet mit {response.StatusCode}: {responseText}";
        }

        using var document = JsonDocument.Parse(responseText);
        var models = document.RootElement.GetProperty("models")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();

        if (models.Any(name => name.Equals(model, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Ollama OK. Modell gefunden: {model}";
        }

        var preview = models.Length == 0 ? "keine Modelle" : string.Join(", ", models.Take(6));
        return $"Ollama erreichbar, aber Modell '{model}' wurde nicht gefunden. Gefunden: {preview}";
    }

    public async Task<string> WarmAsync(string baseUrl, string model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
        {
            return "Ollama Warmhalten uebersprungen: URL oder Modell fehlt.";
        }

        var payload = new
        {
            model,
            stream = false,
            keep_alive = "30m",
            messages = new[]
            {
                new { role = "user", content = "ping" }
            },
            options = new
            {
                num_predict = 1
            }
        };

        var endpoint = $"{baseUrl.TrimEnd('/')}/api/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.IsSuccessStatusCode
            ? $"Ollama warmgehalten: {model}"
            : $"Ollama Warmhalten fehlgeschlagen: {response.StatusCode} {responseText}";
    }
}
