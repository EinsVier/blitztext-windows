# Modell-Auswahl

BlitzText trennt Transkription und Text-Rewrite. Beide Modellfelder werden
unabhängig voneinander konfiguriert. Die Modellnamen werden an den jeweils
ausgewählten Provider weitergegeben; BlitzText ersetzt sie nicht durch eine
eigene Modellliste.

## OpenAI

Die OpenAI-Felder akzeptieren Modell-IDs als freie Eingabe.

| Aufgabe | Empfohlener Startpunkt | Wann wechseln? |
| --- | --- | --- |
| Aufgenommene Audiodatei transkribieren | `gpt-transcribe` | `whisper-1` für Wort-/Segment-Zeitstempel oder englische Übersetzung; `gpt-4o-transcribe-diarize` für Sprecherlabels |
| Live-Transkription | `gpt-live-transcribe` | Nur relevant für einen späteren Live-Audio-Workflow |
| Text vorsichtig verbessern | `gpt-4o-mini` | Ein stärkeres Textmodell verwenden, wenn die Rewrite-Qualität bei anspruchsvollen Texten nicht genügt |

Für `gpt-transcribe` sendet BlitzText Sprachhinweise als `languages[]`.
Legacy-Modelle wie `whisper-1` und `gpt-4o-mini-transcribe` verwenden weiterhin
das einzelne Feld `language`.

Die aktuellen OpenAI-Empfehlungen unterscheiden außerdem zwischen allgemeiner
Dateitranskription, Live-Transkription, Sprecherlabels und Zeitstempeln. Details
stehen in der [OpenAI-Transkriptionsdokumentation](https://developers.openai.com/api/docs/guides/transcription).

### Kostenorientierung

Die Preise ändern sich unabhängig von BlitzText. Zum Zeitpunkt der letzten
Dokumentationsprüfung nennt OpenAI ungefähr:

- `gpt-4o-mini-transcribe`: $0,003 pro Audiominute
- `gpt-transcribe`: $0,0045 pro Audiominute
- `gpt-4o-mini`: $0,15 pro 1 Mio. Input-Tokens und $0,60 pro 1 Mio. Output-Tokens

Die [offizielle OpenAI-Preisliste](https://developers.openai.com/api/docs/pricing) ist maßgeblich.

## Andere Provider

- **OpenRouter:** Rewrite-Modell als Provider-Modell-ID eintragen, zum Beispiel
  `openai/gpt-4o-mini`. OpenRouter wird in BlitzText nur für Text-Rewrite
  verwendet.
- **Anthropic:** Rewrite-Modell als Claude-Modell-ID eintragen. Audio wird nicht
  an Anthropic gesendet.
- **Ollama:** BlitzText liest verfügbare lokale Modelle vom konfigurierten
  Ollama-Server ein. Das gewünschte Modell kann ausgewählt oder manuell
  eingetragen und mit dem Provider-Test geprüft werden.
- **Local Whisper:** Das Whisper-Modell wird als lokale Modelldatei für
  `whisper.cpp` ausgewählt. Diese Variante benötigt keine Online-
  Transkription.

## Konfiguration in BlitzText

Die Auswahl befindet sich im Tab **Provider**:

1. Transkriptions-Provider und Transkriptionsmodell auswählen.
2. Rewrite-Provider und Rewrite-Modell auswählen.
3. Bei OpenAI den API-Key hinterlegen.
4. Mit dem Provider-Test prüfen und anschließend eine echte Aufnahme testen.

Die voreingestellten Werte sind derzeit `whisper-1` für OpenAI-Transkription
und `gpt-4o-mini` für OpenAI-Rewrite. Für neue Aufnahmen mit OpenAI kann als
Transkriptionsmodell `gpt-transcribe` eingetragen werden.

Modellnamen und Provider-Einstellungen werden in `%APPDATA%\\BlitzText\\settings.json`
gespeichert. API-Keys bleiben im Windows Credential Manager und werden nicht in
die Einstellungsdatei exportiert.
