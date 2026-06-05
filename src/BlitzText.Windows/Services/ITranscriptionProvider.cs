namespace BlitzText.Windows.Services;

public interface ITranscriptionProvider
{
    string DisplayName { get; }

    Task<string> TranscribeAsync(string wavPath, CancellationToken cancellationToken);
}
