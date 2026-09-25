namespace BlitzText.Windows.Services;

public static class ModelCatalog
{
    public static IReadOnlyList<string> OpenAiTranscriptionModels { get; } =
    [
        "gpt-transcribe",
        "gpt-4o-mini-transcribe",
        "whisper-1"
    ];

    public static IReadOnlyList<string> OpenAiRewriteModels { get; } =
    [
        "gpt-6-luna",
        "gpt-4o-mini",
        "gpt-4.1-mini",
        "gpt-4.1"
    ];
}
