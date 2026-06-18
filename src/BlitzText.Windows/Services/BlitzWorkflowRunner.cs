using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class BlitzWorkflowRunner(ProviderFactory providerFactory, AppSettings settings)
{
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
        var transcriptionProvider = providerFactory.CreateTranscriptionProvider();
        progress?.Invoke($"Transkribiere mit {transcriptionProvider.DisplayName}...");
        var transcript = await transcriptionProvider.TranscribeAsync(wavPath, cancellationToken);

        var text = await RunTextAsync(transcript, workflow, progress, cancellationToken);
        return new WorkflowRunResult(text, transcript);
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
