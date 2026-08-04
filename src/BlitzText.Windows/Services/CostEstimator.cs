using NAudio.Wave;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class CostEstimator
{
    public static double? EstimateUsd(
        string wavPath,
        AppSettings settings,
        string transcript,
        string finalText)
    {
        var total = 0d;

        if (settings.TranscriptionProvider == TranscriptionProviderKind.OpenAI)
        {
            using var reader = new WaveFileReader(wavPath);
            var minutes = Math.Max(reader.TotalTime.TotalMinutes, 0.01d);
            total += settings.OpenAiTranscriptionModel.ToLowerInvariant() switch
            {
                "gpt-transcribe" => minutes * 0.0045d,
                "gpt-4o-mini-transcribe" => minutes * 0.003d,
                "whisper-1" => minutes * 0.006d,
                _ => 0d
            };
        }

        if (!string.Equals(finalText, transcript, StringComparison.Ordinal) && settings.RewriteProvider == RewriteProviderKind.OpenAI)
        {
            var inputTokens = Math.Max((transcript.Length + 800) / 4d, 1d);
            var outputTokens = Math.Max(finalText.Length / 4d, 1d);
            total += settings.OpenAiRewriteModel.ToLowerInvariant() switch
            {
                "gpt-4o-mini" => inputTokens / 1_000_000d * 0.15d + outputTokens / 1_000_000d * 0.60d,
                "gpt-4.1-mini" => inputTokens / 1_000_000d * 0.40d + outputTokens / 1_000_000d * 1.60d,
                "gpt-4.1" => inputTokens / 1_000_000d * 2.00d + outputTokens / 1_000_000d * 8.00d,
                _ => 0d
            };
        }

        return total > 0d ? total : null;
    }
}
