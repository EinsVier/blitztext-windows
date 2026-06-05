using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class OpenAiTranscriptionProvider(AppSettings settings, HttpClient httpClient) : ITranscriptionProvider
{
    public string DisplayName => "OpenAI";

    public async Task<string> TranscribeAsync(string wavPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAiApiKey);

        await using var stream = File.OpenRead(wavPath);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(settings.OpenAiTranscriptionModel), "model" },
            { new StreamContent(stream), "file", Path.GetFileName(wavPath) }
        };

        var prompt = PromptContextBuilder.BuildTranscriptionPrompt(settings);
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            content.Add(new StringContent(prompt), "prompt");
        }

        var language = LanguageDisplay.ToOpenAiCode(settings.DictationLanguage);
        if (!string.IsNullOrWhiteSpace(language))
        {
            content.Add(new StringContent(language), "language");
        }

        request.Content = content;
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI transcription failed: {response.StatusCode} {responseText}");
        }

        using var document = JsonDocument.Parse(responseText);
        return document.RootElement.GetProperty("text").GetString()?.Trim() ?? "";
    }
}
