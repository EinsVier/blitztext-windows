using System.Diagnostics;
using System.IO;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public sealed class LocalWhisperTranscriptionProvider(AppSettings settings) : ITranscriptionProvider
{
    public string DisplayName => "Lokales Whisper";

    public async Task<string> TranscribeAsync(string wavPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.LocalWhisperExecutablePath) || !File.Exists(settings.LocalWhisperExecutablePath))
        {
            throw new InvalidOperationException("Lokale Transkription: whisper.cpp EXE wurde nicht gefunden.");
        }

        if (string.IsNullOrWhiteSpace(settings.LocalWhisperModelPath) || !File.Exists(settings.LocalWhisperModelPath))
        {
            throw new InvalidOperationException("Lokale Transkription: Whisper-Modell wurde nicht gefunden.");
        }

        var outputBasePath = Path.Combine(Path.GetTempPath(), $"blitztext-{Guid.NewGuid():N}");
        var outputTextPath = outputBasePath + ".txt";
        using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(settings.LocalWhisperTimeoutSeconds));
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = settings.LocalWhisperExecutablePath,
                    Arguments = $"-m {Quote(settings.LocalWhisperModelPath)} -f {Quote(wavPath)} -l {LanguageDisplay.ToWhisperCode(settings.DictationLanguage)} -otxt -of {Quote(outputBasePath)}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCancellation.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(linkedCancellation.Token);
            await process.WaitForExitAsync(linkedCancellation.Token);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Lokale Transkription fehlgeschlagen: {stderr.Trim()}");
            }

            if (File.Exists(outputTextPath))
            {
                var text = await File.ReadAllTextAsync(outputTextPath, cancellationToken);
                return text.Trim();
            }

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                return stdout.Trim();
            }

            throw new InvalidOperationException("Lokale Transkription hat keinen Text erzeugt.");
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Lokale Transkription hat laenger als {settings.LocalWhisperTimeoutSeconds} Sekunden gedauert.");
        }
        finally
        {
            TryDelete(outputTextPath);
        }
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temporary transcript cleanup is best-effort.
        }
    }
}
