using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class LanguageDisplay
{
    public static IReadOnlyList<DisplayOption<AppLanguage>> AppLanguageOptions(AppLanguage appLanguage)
    {
        return appLanguage == AppLanguage.English
            ?
            [
                new(AppLanguage.German, "German"),
                new(AppLanguage.English, "English")
            ]
            :
            [
                new(AppLanguage.German, "Deutsch"),
                new(AppLanguage.English, "Englisch")
            ];
    }

    public static IReadOnlyList<DisplayOption<DictationLanguage>> DictationLanguageOptions(AppLanguage appLanguage)
    {
        return appLanguage == AppLanguage.English
            ?
            [
                new(DictationLanguage.Auto, "Automatic"),
                new(DictationLanguage.German, "German"),
                new(DictationLanguage.English, "English")
            ]
            :
            [
                new(DictationLanguage.Auto, "Automatisch"),
                new(DictationLanguage.German, "Deutsch"),
                new(DictationLanguage.English, "Englisch")
            ];
    }

    public static DisplayOption<AppLanguage> FindAppLanguage(AppLanguage value, AppLanguage appLanguage)
    {
        return AppLanguageOptions(appLanguage).FirstOrDefault(option => option.Value == value) ?? AppLanguageOptions(appLanguage)[0];
    }

    public static DisplayOption<DictationLanguage> FindDictationLanguage(DictationLanguage value, AppLanguage appLanguage)
    {
        return DictationLanguageOptions(appLanguage).FirstOrDefault(option => option.Value == value) ?? DictationLanguageOptions(appLanguage)[0];
    }

    public static string ToWhisperCode(DictationLanguage language)
    {
        return language switch
        {
            DictationLanguage.German => "de",
            DictationLanguage.English => "en",
            _ => "auto"
        };
    }

    public static string? ToOpenAiCode(DictationLanguage language)
    {
        return language switch
        {
            DictationLanguage.German => "de",
            DictationLanguage.English => "en",
            _ => null
        };
    }
}
