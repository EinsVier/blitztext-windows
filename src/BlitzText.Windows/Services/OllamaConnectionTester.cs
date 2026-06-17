using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BlitzText.Windows.Services;

public sealed record OllamaTestResult(string Message, IReadOnlyList<string> Models, bool ModelFound);

public sealed class OllamaConnectionTester
{
    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    public async Task<OllamaTestResult> TestAsync(string baseUrl, string model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new OllamaTestResult("Ollama URL fehlt.", [], false);
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return new OllamaTestResult("Ollama Modell fehlt.", [], false);
        }

        var endpoint = $"{baseUrl.TrimEnd('/')}/api/tags";
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new OllamaTestResult($"Ollama antwortet mit {response.StatusCode}: {responseText}", [], false);
        }

        using var document = JsonDocument.Parse(responseText);
        var models = document.RootElement.GetProperty("models")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (models.Any(name => name.Equals(model, StringComparison.OrdinalIgnoreCase)))
        {
            return new OllamaTestResult($"Ollama OK. Modell gefunden: {model}", models, true);
        }

        var preview = models.Length == 0 ? "keine Modelle" : string.Join(", ", models.Take(6));
        return new OllamaTestResult($"Ollama erreichbar, aber Modell '{model}' wurde nicht gefunden. Gefunden: {preview}", models, false);
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
