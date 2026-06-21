namespace BlitzText.Windows.Models;

public static class DefaultPrompts
{
    public const string Improve = "Formuliere den Text klar, natuerlich und gut lesbar. Entferne Fuellwoerter und unnoetige Wiederholungen, ohne den Ton oder die Aussage zu veraendern. Nutze Abschnitte oder Listen nur, wenn sie das Verstaendnis verbessern.";
    public const string Calm = "Formuliere den Text ruhig, sachlich und respektvoll. Entferne Eskalation, Unterstellungen und verletzende Formulierungen, aber erhalte das konkrete Anliegen, die geforderte Handlung und berechtigte Kritik in ihrer Staerke.";
    public const string Emojis = "Pruefe zuerst die Textart. Verwende keine Emojis bei technischen, formellen, rechtlichen, konfliktbezogenen oder sensiblen Texten sowie bei Gesundheitsthemen wie Untersuchung, Befund, Diagnose, Krankheit, Arzt oder Behandlung. Wenn keine dieser Ausnahmen vorliegt, fuege ein bis drei passende Emojis ein. Platziere sie natuerlich am Satzende oder als dezente Gliederung. Veraendere Aussage und Ton nicht.";

    public const string ImproveEnglish = "Make the text clear, natural, and easy to read. Remove filler and unnecessary repetition without changing its tone or message. Use paragraphs or lists only when they improve comprehension.";
    public const string CalmEnglish = "Make the text calm, factual, and respectful. Remove escalation, assumptions, and hurtful phrasing while keeping the concrete concern, requested action, and valid criticism equally clear.";
    public const string EmojisEnglish = "Classify the text type first. Use no emojis in technical, formal, legal, conflict-related, or sensitive text, or in health topics such as examinations, findings, diagnoses, illness, doctors, or treatment. Otherwise add one to three fitting emojis. Place them naturally at sentence endings or as subtle visual structure. Do not change the message or tone.";

    public static string GetImprove(AppLanguage language) => language == AppLanguage.English ? ImproveEnglish : Improve;

    public static string GetCalm(AppLanguage language) => language == AppLanguage.English ? CalmEnglish : Calm;

    public static string GetEmojis(AppLanguage language) => language == AppLanguage.English ? EmojisEnglish : Emojis;
}
