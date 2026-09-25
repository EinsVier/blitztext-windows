namespace BlitzText.Windows.Models;

public sealed record DictationProfile(
    string Id,
    string GermanLabel,
    string EnglishLabel,
    string GermanTranscriptionPrompt,
    string EnglishTranscriptionPrompt,
    string CustomNames)
{
    public string GetLabel(AppLanguage language) => language == AppLanguage.English ? EnglishLabel : GermanLabel;

    public string GetTranscriptionPrompt(AppLanguage language) => language == AppLanguage.English
        ? EnglishTranscriptionPrompt
        : GermanTranscriptionPrompt;
}
