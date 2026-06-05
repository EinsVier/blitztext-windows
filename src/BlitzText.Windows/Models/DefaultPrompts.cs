namespace BlitzText.Windows.Models;

public static class DefaultPrompts
{
    public const string Improve = "Schreibe den diktierten Rohtext klar, natuerlich und gut lesbar. Korrigiere Grammatik, Satzbau, Interpunktion und offensichtliche Diktierfehler. Erhalte Bedeutung, Fakten, Namen, Zahlen, Termine, Sprache und Ton. Wenn der Text Listen, Aufgaben oder Schritte enthaelt, strukturiere sie uebersichtlich. Erfinde keine neuen Inhalte.";
    public const string Calm = "Formuliere den Text ruhig, sachlich und professionell. Erhalte das konkrete Anliegen, Fakten, Namen, Zahlen, Termine und gewuenschte Handlungen, aber entferne Eskalation, Vorwuerfe und verletzenden Ton. Schwaeche berechtigte Kritik nicht ab. Erfinde keine neuen Inhalte, Zusagen oder Entschuldigungen.";
    public const string Emojis = "Ergaenze wenige passende Emojis, wenn sie den Text natuerlicher oder freundlicher machen. Verwende Emojis sparsam, maximal dort, wo sie wirklich passen. Bleibe professionell, uebertreibe nicht und veraendere die Aussage nicht.";

    public const string ImproveEnglish = "Rewrite the dictated draft into clear, natural, readable text. Correct grammar, punctuation, sentence flow, and obvious dictation mistakes. Preserve meaning, facts, names, numbers, dates, language, and tone. If the text contains lists, tasks, or steps, structure them clearly. Do not invent new content.";
    public const string CalmEnglish = "Rewrite the text into a calm, factual, professional message. Preserve the concrete concern, facts, names, numbers, dates, and desired actions, but remove escalation, blame, and hurtful tone. Do not weaken valid criticism. Do not invent new content, commitments, or apologies.";
    public const string EmojisEnglish = "Add a small number of fitting emojis where they make the text feel more natural or friendly. Use emojis sparingly and only where they truly fit. Stay professional, do not overdo it, and do not change the meaning.";

    public static string GetImprove(AppLanguage language) => language == AppLanguage.English ? ImproveEnglish : Improve;

    public static string GetCalm(AppLanguage language) => language == AppLanguage.English ? CalmEnglish : Calm;

    public static string GetEmojis(AppLanguage language) => language == AppLanguage.English ? EmojisEnglish : Emojis;
}
