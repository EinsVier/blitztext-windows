using BlitzText.Windows.Models;
using NAudio.Wave;

namespace BlitzText.Windows.Services;

public sealed class BlitzWorkflowRunner(ProviderFactory providerFactory, AppSettings settings)
{
    private static readonly TimeSpan MinimumOpenAiRecordingDuration = TimeSpan.FromSeconds(1);

    public async Task<string> RunAsync(
        string wavPath,
        WorkflowKind workflow,
        Action<string>? progress,
        CancellationToken cancellationToken)
    {
        return (await RunWithTranscriptAsync(wavPath, workflow, progress, cancellationToken)).Text;
    }

    public async Task<WorkflowRunResult> RunWithTranscriptAsync(
        string wavPath,
        WorkflowKind workflow,
        Action<string>? progress,
        CancellationToken cancellationToken)
    {
        if (settings.TranscriptionProvider == TranscriptionProviderKind.OpenAI)
        {
            using var reader = new WaveFileReader(wavPath);
            if (reader.TotalTime < MinimumOpenAiRecordingDuration)
            {
                var message = settings.AppLanguage == AppLanguage.English
                    ? "Recording is shorter than 1 second. It was not sent to the OpenAI API. Please record for longer."
                    : "Die Aufnahme ist kürzer als 1 Sekunde. Sie wurde nicht an die OpenAI-API gesendet. Bitte länger aufnehmen.";
                throw new InvalidOperationException(message);
            }
        }

        var transcriptionProvider = providerFactory.CreateTranscriptionProvider();
        progress?.Invoke($"Transkribiere mit {transcriptionProvider.DisplayName}...");
        var transcript = await transcriptionProvider.TranscribeAsync(wavPath, cancellationToken);

        var text = await RunTextAsync(transcript, workflow, progress, cancellationToken);
        var estimatedCostUsd = CostEstimator.EstimateUsd(wavPath, settings, transcript, text);
        return new WorkflowRunResult(text, transcript, estimatedCostUsd);
    }

    public async Task<string> RunTextAsync(
        string text,
        WorkflowKind workflow,
        Action<string>? progress,
        CancellationToken cancellationToken)
    {
        var rewritePrompt = WorkflowPromptFactory.CreateRewritePrompt(workflow, text, settings);
        if (string.IsNullOrWhiteSpace(rewritePrompt))
        {
            return text;
        }

        var rewriteProvider = providerFactory.CreateRewriteProvider();
        progress?.Invoke($"Umschreibe mit {rewriteProvider.DisplayName}...");
        return await rewriteProvider.RewriteAsync(rewritePrompt, cancellationToken);
    }
}
