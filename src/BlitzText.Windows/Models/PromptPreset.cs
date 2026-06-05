namespace BlitzText.Windows.Models;

public sealed record PromptPreset(string Id, string GermanLabel, string EnglishLabel, string GermanPrompt, string EnglishPrompt)
{
    public string GetLabel(AppLanguage language) => language == AppLanguage.English ? EnglishLabel : GermanLabel;

    public string GetPrompt(AppLanguage language) => language == AppLanguage.English ? EnglishPrompt : GermanPrompt;

    public override string ToString() => GermanLabel;
}
