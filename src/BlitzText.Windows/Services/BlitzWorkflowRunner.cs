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
        var transcriptionProvider = providerFactory.CreateTranscriptionProvider();
        progress?.Invoke($"Transkribiere mit {transcriptionProvider.DisplayName}...");
        var transcript = await transcriptionProvider.TranscribeAsync(wavPath, cancellationToken);

        var rewritePrompt = WorkflowPromptFactory.CreateRewritePrompt(workflow, transcript, settings);
        if (string.IsNullOrWhiteSpace(rewritePrompt))
        {
            return transcript;
        }

        var rewriteProvider = providerFactory.CreateRewriteProvider();
        progress?.Invoke($"Umschreibe mit {rewriteProvider.DisplayName}...");
        return await rewriteProvider.RewriteAsync(rewritePrompt, cancellationToken);
    }
}
