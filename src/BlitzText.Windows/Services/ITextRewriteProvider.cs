namespace BlitzText.Windows.Services;

public interface ITextRewriteProvider
{
    string DisplayName { get; }

    Task<string> RewriteAsync(string prompt, CancellationToken cancellationToken);
}
