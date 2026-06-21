using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class PromptPresetCatalog
{
    public static readonly IReadOnlyList<PromptPreset> Presets =
    [
        new(
            "general",
            "Allgemein klar",
            "General clarity",
            DefaultPrompts.Improve,
            DefaultPrompts.ImproveEnglish),
        new(
            "email",
            "E-Mail freundlich",
            "Friendly email",
            "Formuliere eine freundliche, verbindliche E-Mail. Nutze eine passende Anrede und einen kurzen Abschluss nur, wenn der Rohtext sie enthaelt oder eindeutig nahelegt. Stelle Anliegen und naechsten Schritt klar heraus; vermeide steife Floskeln.",
            "Write a friendly, confident email. Use a suitable greeting and brief closing only when the draft contains or clearly implies them. Make the request and next step clear; avoid stiff formalities."),
        new(
            "chat",
            "Chat kurz",
            "Short chat",
            "Formuliere eine kurze, direkte Chat-Nachricht. Schreibe locker und natuerlich, ohne Anrede, Abschluss oder unnoetige Erklaerungen. Behalte alle handlungsrelevanten Details.",
            "Write a short, direct chat message. Keep it casual and natural, with no greeting, closing, or unnecessary explanation. Retain every action-relevant detail."),
        new(
            "bullets",
            "Stichpunkte",
            "Bullet points",
            "Gib den Inhalt als knappe Stichpunkte aus. Ein Gedanke pro Punkt; gruppiere zusammengehoerige Punkte unter kurzen Zwischenueberschriften, wenn mehrere Themen vorkommen. Verwende keine einleitende oder abschliessende Floskel.",
            "Return the content as concise bullet points. Use one idea per bullet and short section headings when several topics are present. Add no introductory or closing filler."),
        new(
            "tasks",
            "Aufgabenliste",
            "Task list",
            "Gib eine umsetzbare Aufgabenliste aus. Beginne jeden Punkt mit einem Verb. Fuege genannte Zustaendige, Termine, Prioritaeten und Abhaengigkeiten kompakt hinzu. Trenne offene Fragen in einen Abschnitt \"Offen\".",
            "Return an actionable task list. Start each item with a verb. Add stated owners, deadlines, priorities, and dependencies compactly. Put unresolved questions in an \"Open\" section."),
        new(
            "meeting-note",
            "Meetingnotiz",
            "Meeting note",
            "Erstelle eine kompakte Meetingnotiz. Nutze nur passende Abschnitte aus: Thema, Kernaussagen, Entscheidungen, Aufgaben und offene Fragen. Formuliere Aufgaben mit Zustaendigkeit und Termin, sofern genannt.",
            "Create a concise meeting note. Use only applicable sections from: Topic, Key points, Decisions, Actions, and Open questions. Include owner and deadline for actions when stated."),
        new(
            "customer-reply",
            "Kundenantwort",
            "Customer reply",
            "Formuliere eine freundliche, professionelle Kundenantwort. Bestaetige das konkrete Anliegen knapp, erklaere Loesung oder naechsten Schritt klar und nenne Einschraenkungen offen. Mache keine weitergehenden Zusagen als im Rohtext.",
            "Write a friendly, professional customer reply. Briefly acknowledge the specific concern, explain the solution or next step clearly, and state limitations openly. Make no commitment beyond the draft."),
        new(
            "decision-note",
            "Entscheidungsnotiz",
            "Decision note",
            "Erstelle eine Entscheidungsnotiz mit nur den vorhandenen Abschnitten: Ausgangslage, Optionen, Entscheidung, Begruendung, Risiken und naechste Schritte. Kennzeichne Unsicherheiten und noch nicht getroffene Entscheidungen eindeutig.",
            "Create a decision note using only sections supported by the draft: Context, Options, Decision, Rationale, Risks, and Next steps. Clearly mark uncertainty and decisions that remain open."),
        new(
            "howto",
            "Anleitung",
            "How-to guide",
            "Erstelle eine nummerierte Schritt-fuer-Schritt-Anleitung. Beginne jeden Schritt mit einer Handlung und halte Voraussetzungen oder Warnungen getrennt davon. Gib Menuepunkte, Schaltflaechen, Pfade, Befehle und URLs unveraendert wieder.",
            "Create a numbered step-by-step guide. Start each step with an action and keep prerequisites or warnings separate. Reproduce menu items, buttons, paths, commands, and URLs unchanged."),
        new(
            "concise-professional",
            "Kurz professionell",
            "Concise professional",
            "Kuerze den Text deutlich und formuliere ihn professionell. Behalte Kernaussage, konkrete Bitte und handlungsrelevante Details. Entferne Wiederholungen, Fuellwoerter und Nebenbemerkungen.",
            "Shorten the text substantially and make it professional. Keep the core message, specific request, and action-relevant details. Remove repetition, filler, and side remarks."),
        new(
            "ai-prompt",
            "KI-Prompt",
            "AI prompt",
            "Erstelle einen direkt nutzbaren KI-Prompt. Ordne vorhandene Angaben in Ziel, Kontext, Aufgabe, Anforderungen und Ausgabeformat. Formuliere klare Arbeitsanweisungen. Markiere wirklich notwendige fehlende Angaben knapp als [OFFEN: ...].",
            "Create a ready-to-use AI prompt. Organize available information into Goal, Context, Task, Requirements, and Output format. Use clear working instructions. Mark only essential missing information briefly as [OPEN: ...]."),
        new(
            "image-prompt",
            "KI-Bildprompt",
            "AI image prompt",
            "Erstelle einen kompakten Bild-Prompt. Ordne vorhandene Angaben in Motiv, Umgebung, Komposition, Stil, Licht, Farben und Format. Nenne sichtbaren Text woertlich. Fuege keine nicht verlangten Motive, Marken oder Gestaltungselemente hinzu.",
            "Create a compact image prompt. Organize available details into subject, environment, composition, style, lighting, colors, and format. Quote visible text exactly. Add no unrequested subjects, brands, or design elements."),
        new(
            "music-prompt",
            "Musik-Prompt",
            "Music prompt",
            "Erstelle einen kompakten Musik-Prompt. Ordne vorhandene Angaben in Genre, Stimmung, Tempo, Instrumentierung, Gesang, Struktur und Klangbild. Gib vorhandene Lyrics exakt wieder; erfinde keine neuen Textzeilen.",
            "Create a compact music prompt. Organize available details into genre, mood, tempo, instrumentation, vocals, structure, and sonic character. Reproduce supplied lyrics exactly; do not invent new lines."),
        new(
            "technical",
            "Technischer Text",
            "Technical text",
            "Formuliere einen praezisen, sachlichen technischen Text. Strukturiere Problem, Umgebung, Beobachtung, Schritte und Ergebnis, soweit vorhanden. Gib Code, Befehle, Pfade, Bezeichner, Versionen und Fehlermeldungen unveraendert in geeigneter Formatierung wieder.",
            "Write precise, factual technical text. Structure the problem, environment, observation, steps, and result where available. Reproduce code, commands, paths, identifiers, versions, and error messages unchanged using suitable formatting.")
    ];

    public static IReadOnlyList<DisplayOption<PromptPreset>> GetOptions(AppLanguage language)
    {
        return Presets.Select(preset => new DisplayOption<PromptPreset>(preset, preset.GetLabel(language))).ToList();
    }
}
