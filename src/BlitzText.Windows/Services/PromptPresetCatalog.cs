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
            "Rewrite the dictated draft into clear, natural, readable text. Correct grammar, sentence flow, and obvious dictation mistakes. Preserve meaning, facts, names, numbers, language, and tone. Do not invent new content."),
        new(
            "email",
            "E-Mail freundlich",
            "Friendly email",
            "Formuliere den diktierten Rohtext als klare, freundliche E-Mail oder Nachricht. Erhalte Anliegen, Fakten, Namen, Zahlen und gewuenschte naechste Schritte. Schreibe natuerlich, verbindlich und nicht zu foermlich. Erfinde keine neuen Inhalte.",
            "Turn the dictated draft into a clear, friendly email or message. Preserve the request, facts, names, numbers, and desired next steps. Keep it natural, confident, and not overly formal. Do not invent new content."),
        new(
            "chat",
            "Chat kurz",
            "Short chat",
            "Formuliere den diktierten Rohtext als kurze, direkte Chat-Nachricht. Entferne Fuellwoerter, erhalte die Aussage und bleibe locker, aber professionell. Erfinde keine neuen Inhalte.",
            "Turn the dictated draft into a short, direct chat message. Remove filler, preserve the point, and keep it casual but professional. Do not invent new content."),
        new(
            "bullets",
            "Stichpunkte",
            "Bullet points",
            "Strukturiere den diktierten Rohtext als uebersichtliche Stichpunkte. Gruppiere zusammengehoerige Gedanken, erhalte alle wichtigen Details, Namen, Zahlen und Aufgaben. Erfinde keine neuen Inhalte.",
            "Structure the dictated draft as clear bullet points. Group related thoughts and preserve important details, names, numbers, and tasks. Do not invent new content."),
        new(
            "tasks",
            "Aufgabenliste",
            "Task list",
            "Wandle den diktierten Rohtext in eine konkrete Aufgabenliste um. Formuliere jede Aufgabe als klare Handlung. Erhalte Zustaendige, Termine, Prioritaeten, Namen, Zahlen und Abhaengigkeiten, falls sie genannt wurden. Markiere offene Punkte als Frage, aber erfinde keine neuen Aufgaben.",
            "Turn the dictated draft into a concrete task list. Phrase each task as a clear action. Preserve owners, deadlines, priorities, names, numbers, and dependencies if they were mentioned. Mark open points as questions, but do not invent new tasks."),
        new(
            "meeting-note",
            "Meetingnotiz",
            "Meeting note",
            "Formuliere den diktierten Rohtext als kompakte Meeting- oder Telefonnotiz. Gliedere sinnvoll in Thema, wichtige Punkte, Entscheidungen und naechste Schritte, wenn diese Informationen vorhanden sind. Erhalte Namen, Termine, Zahlen und offene Fragen. Erfinde keine neuen Inhalte.",
            "Turn the dictated draft into a concise meeting or call note. Structure it into topic, key points, decisions, and next steps when that information is present. Preserve names, dates, numbers, and open questions. Do not invent new content."),
        new(
            "customer-reply",
            "Kundenantwort",
            "Customer reply",
            "Formuliere den diktierten Rohtext als freundliche, professionelle Antwort an einen Kunden oder Partner. Bleibe verbindlich und loesungsorientiert. Erhalte Zusagen, Einschraenkungen, Termine, Preise, Namen und Fakten exakt. Erfinde keine neuen Zusagen.",
            "Turn the dictated draft into a friendly, professional reply to a customer or partner. Keep it confident and solution-oriented. Preserve commitments, limitations, dates, prices, names, and facts exactly. Do not invent new commitments."),
        new(
            "decision-note",
            "Entscheidungsnotiz",
            "Decision note",
            "Strukturiere den diktierten Rohtext als Entscheidungsnotiz. Trenne Ausgangslage, Optionen, Entscheidung, Begruendung, Risiken und naechste Schritte, soweit im Text vorhanden. Erhalte Fakten, Zahlen, Namen und Unsicherheiten. Erfinde keine Gruende oder Entscheidungen.",
            "Structure the dictated draft as a decision note. Separate context, options, decision, rationale, risks, and next steps where present in the draft. Preserve facts, numbers, names, and uncertainties. Do not invent reasons or decisions."),
        new(
            "howto",
            "Anleitung",
            "How-to guide",
            "Formuliere den diktierten Rohtext als klare Schritt-fuer-Schritt-Anleitung. Nummeriere die Schritte, erhalte Menuepunkte, Schaltflaechen, Pfade, Befehle, URLs, Warnungen und Reihenfolgen exakt. Erfinde keine Schritte, die nicht genannt wurden.",
            "Turn the dictated draft into a clear step-by-step guide. Number the steps and preserve menu items, buttons, paths, commands, URLs, warnings, and order exactly. Do not invent steps that were not mentioned."),
        new(
            "concise-professional",
            "Kurz professionell",
            "Concise professional",
            "Kuerze den diktierten Rohtext deutlich und formuliere ihn professionell. Erhalte die Kernaussage, konkrete Bitten, Fakten, Namen, Zahlen und Termine. Entferne Wiederholungen und Fuellwoerter. Erfinde keine neuen Inhalte.",
            "Shorten the dictated draft substantially and make it professional. Preserve the core message, concrete requests, facts, names, numbers, and dates. Remove repetition and filler. Do not invent new content."),
        new(
            "ai-prompt",
            "KI-Prompt",
            "AI prompt",
            "Formuliere den diktierten Rohtext als klaren Prompt fuer eine KI. Beschreibe Ziel, Kontext, gewuenschtes Ergebnis, Ausgabeformat, wichtige Details und Einschraenkungen. Erhalte Fakten, Namen, Zahlen und konkrete Vorgaben exakt. Wenn Informationen fehlen, markiere sie als kurze Rueckfrage oder Platzhalter. Erfinde keine neuen Fakten.",
            "Turn the dictated draft into a clear prompt for an AI assistant. Describe the goal, context, desired result, output format, important details, and constraints. Preserve facts, names, numbers, and concrete requirements exactly. If information is missing, mark it as a short question or placeholder. Do not invent new facts."),
        new(
            "image-prompt",
            "KI-Bildprompt",
            "AI image prompt",
            "Formuliere den diktierten Rohtext als strukturierten Prompt fuer eine Bild-KI. Beschreibe Motiv, Szene, Stil, Stimmung, Licht, Perspektive, Bildausschnitt, Farben, Details und Format, soweit genannt. Erhalte Namen, Marken, Texte im Bild und konkrete Vorgaben exakt. Erfinde keine wichtigen Bildelemente; fehlende Angaben als optionale Hinweise markieren.",
            "Turn the dictated draft into a structured prompt for an image AI. Describe subject, scene, style, mood, lighting, perspective, framing, colors, details, and format where mentioned. Preserve names, brands, text in the image, and concrete requirements exactly. Do not invent important visual elements; mark missing details as optional suggestions."),
        new(
            "music-prompt",
            "Musik-Prompt",
            "Music prompt",
            "Formuliere den diktierten Rohtext als Prompt fuer eine Musik-KI. Beschreibe Genre, Stimmung, Tempo, Instrumente, Gesang, Sprache, Songstruktur, Energie, Klangbild und Einsatzkontext, soweit genannt. Erhalte konkrete Vorgaben exakt. Erfinde keine Lyrics; wenn Text oder Refrain fehlen, markiere sie als Platzhalter oder optionale Richtung.",
            "Turn the dictated draft into a prompt for a music AI. Describe genre, mood, tempo, instruments, vocals, language, song structure, energy, sound, and use case where mentioned. Preserve concrete requirements exactly. Do not invent lyrics; if lyrics or a chorus are missing, mark them as placeholders or optional direction."),
        new(
            "technical",
            "Technischer Text",
            "Technical text",
            "Formuliere den diktierten Rohtext als praezisen technischen Text. Erhalte Fachbegriffe, Produktnamen, Befehle, Pfade, Versionsnummern und Fehlermeldungen exakt. Korrigiere nur offensichtliche Diktierfehler. Erfinde keine neuen Inhalte.",
            "Turn the dictated draft into precise technical text. Preserve terms, product names, commands, paths, version numbers, and error messages exactly. Correct only obvious dictation mistakes. Do not invent new content.")
    ];

    public static IReadOnlyList<DisplayOption<PromptPreset>> GetOptions(AppLanguage language)
    {
        return Presets.Select(preset => new DisplayOption<PromptPreset>(preset, preset.GetLabel(language))).ToList();
    }
}
