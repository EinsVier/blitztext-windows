using System.IO;
using NAudio.Wave;

namespace BlitzText.Windows.Services;

public sealed class AudioRecorderService : IDisposable
{
    private static readonly TimeSpan MinimumRecordingDuration = TimeSpan.FromMilliseconds(600);
    private const long MinimumWavBytes = 1600;

    private WaveInEvent? waveIn;
    private WaveFileWriter? writer;
    private string? currentPath;
    private DateTimeOffset recordingStartedAt;

    public bool IsRecording => waveIn is not null;

    public Task StartAsync()
    {
        if (IsRecording)
        {
            return Task.CompletedTask;
        }

        if (WaveInEvent.DeviceCount <= 0)
        {
            throw new InvalidOperationException("Kein Mikrofon gefunden.");
        }

        var directory = Path.Combine(Path.GetTempPath(), "BlitzText");
        Directory.CreateDirectory(directory);
        currentPath = Path.Combine(directory, $"recording-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.wav");
        recordingStartedAt = DateTimeOffset.UtcNow;

        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1)
        };
        writer = new WaveFileWriter(currentPath, waveIn.WaveFormat);

        waveIn.DataAvailable += (_, args) => writer?.Write(args.Buffer, 0, args.BytesRecorded);
        waveIn.StartRecording();
        return Task.CompletedTask;
    }

    public Task<string> StopAsync()
    {
        if (!IsRecording || currentPath is null)
        {
            throw new InvalidOperationException("Recording has not started.");
        }

        waveIn!.StopRecording();
        waveIn.Dispose();
        waveIn = null;

        writer?.Dispose();
        writer = null;

        var duration = DateTimeOffset.UtcNow - recordingStartedAt;
        if (duration < MinimumRecordingDuration)
        {
            DeleteCurrentRecording();
            throw new InvalidOperationException("Aufnahme war zu kurz.");
        }

        if (!File.Exists(currentPath) || new FileInfo(currentPath).Length < MinimumWavBytes)
        {
            DeleteCurrentRecording();
            throw new InvalidOperationException("Aufnahme ist leer. Bitte Mikrofon pruefen.");
        }

        return Task.FromResult(currentPath);
    }

    private void DeleteCurrentRecording()
    {
        if (currentPath is null || !File.Exists(currentPath))
        {
            return;
        }

        try
        {
            File.Delete(currentPath);
        }
        catch
        {
            // Best-effort cleanup of temporary audio files.
        }
    }

    public void Dispose()
    {
        waveIn?.Dispose();
        writer?.Dispose();
    }
}
