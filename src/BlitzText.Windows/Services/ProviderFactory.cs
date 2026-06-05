using System.Net.Http;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class ProviderFactory(AppSettings settings)
{
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };

    public ITranscriptionProvider CreateTranscriptionProvider()
    {
        return settings.TranscriptionProvider switch
        {
            TranscriptionProviderKind.OpenAI => new OpenAiTranscriptionProvider(settings, httpClient),
            TranscriptionProviderKind.LocalWhisper => new LocalWhisperTranscriptionProvider(settings),
            _ => new OpenAiTranscriptionProvider(settings, httpClient)
        };
    }

    public ITextRewriteProvider CreateRewriteProvider()
    {
        return settings.RewriteProvider switch
        {
            RewriteProviderKind.OpenAI => new OpenAiTextRewriteProvider(settings, httpClient),
            RewriteProviderKind.Ollama => new OllamaTextRewriteProvider(settings, httpClient),
            _ => new OpenAiTextRewriteProvider(settings, httpClient)
        };
    }
}
