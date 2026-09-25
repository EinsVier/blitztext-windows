using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class DictationProfileCatalog
{
    public static readonly IReadOnlyList<DictationProfile> Profiles =
    [
        new(
            "general",
            "Allgemein",
            "General",
            "Deutsches Diktat in vollstaendigen Saetzen. Setze Satzzeichen sinnvoll.",
            "General dictation in complete sentences. Use sensible punctuation.",
            ""),
        new(
            "email",
            "E-Mail",
            "Email",
            "Deutsches E-Mail-Diktat. Erkenne Anrede, Namen, Satzzeichen und Absätze sorgfaeltig.",
            "Email dictation. Recognize salutations, names, punctuation, and paragraphs carefully.",
            "Mit freundlichen Gruessen\nViele Gruesse"),
        new(
            "technology",
            "Technik und Code",
            "Technology and code",
            "Deutsches Technikdiktat. Bewahre Produktnamen, Dateinamen, Befehle und Fachbegriffe exakt.",
            "Technical dictation. Preserve product names, file names, commands, and technical terms exactly.",
            "AMD NPU\nWhisper\nwhisper.cpp\nVitis-AI\nRAI-Datei\nGGML-small\nOllama\nDocker\nPowerShell\nBlitzText"),
        new(
            "neukalen",
            "Neukalen und Eigennamen",
            "Neukalen and proper names",
            "Deutsches Diktat mit Orts- und Projektnamen. Bewahre die vorgegebenen Schreibweisen exakt.",
            "Dictation with place and project names. Preserve the supplied spellings exactly.",
            "Neukalen\nStadt Neukalen\nAmt Malchin\nTeterow\ngem.neukalen.de\nneukalen.de\nBrother\nBrother MFC-L3770CDW\nprinter.home.lan\nBlitzText")
    ];

    public static IReadOnlyList<DisplayOption<DictationProfile>> GetOptions(
        AppLanguage language,
        IEnumerable<SavedDictationProfile>? savedProfiles = null)
    {
        var options = Profiles
            .Select(profile => new DisplayOption<DictationProfile>(profile, profile.GetLabel(language)))
            .ToList();

        if (savedProfiles is not null)
        {
            options.AddRange(savedProfiles
                .Where(profile => !string.IsNullOrWhiteSpace(profile.Id) && !string.IsNullOrWhiteSpace(profile.Name))
                .Select(profile => new DisplayOption<DictationProfile>(
                    new DictationProfile(
                        profile.Id,
                        profile.Name,
                        profile.Name,
                        profile.TranscriptionPrompt,
                        profile.TranscriptionPrompt,
                        profile.CustomNames),
                    profile.Name)));
        }

        return options;
    }
}
