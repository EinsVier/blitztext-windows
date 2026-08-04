# Modell-Auswahl

BlitzText trennt Transkription und Text-Rewrite. Beide Modellfelder werden
unabhängig voneinander konfiguriert. Die Modellnamen werden an den jeweils
ausgewählten Provider weitergegeben; BlitzText ersetzt sie nicht durch eine
eigene Modellliste.

## OpenAI

Die OpenAI-Felder akzeptieren Modell-IDs als freie Eingabe.

BlitzText bietet dafür zusätzlich eine editierbare Auswahlliste mit gängigen
Empfehlungen. Eigene oder neue Modell-IDs können weiterhin direkt eingetragen
werden.

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

Die in BlitzText hinterlegten Eigennamen werden bei `gpt-transcribe` zusätzlich
als `keywords[]` übertragen. Dadurch können Namen, Ortsangaben und Fachbegriffe
gezielter erkannt werden. Die Liste wird im Tab **Prompts** gepflegt.

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

Der OpenAI-Test prüft API-Key und Modellliste. Eine als „nicht gelistet"
gemeldete Modell-ID ist ein Hinweis zur Prüfung, aber nicht allein ein Beweis,
dass der Transkriptionsendpunkt sie nicht akzeptiert.

### Datenschutzprofil

Mit **Nur lokal** erzwingt BlitzText lokale Transkription über Local Whisper und
lokales Rewrite über Ollama. Die Cloud-Provider bleiben konfiguriert, werden in
diesem Profil aber nicht verwendet. Im Standardprofil können die Provider frei
kombiniert werden.

Nach jeder Cloud-Verarbeitung zeigt BlitzText außerdem eine grobe
Kostenindikation. Sie basiert auf Aufnahmedauer, Modell und Textlänge und ist
nicht als Rechnungsbetrag zu verstehen.

Nach einer sehr leisen Aufnahme erscheint ein Hinweis, den Mikrofonpegel oder
das aktive Windows-Aufnahmegerät zu prüfen.

Die voreingestellten Werte sind derzeit `whisper-1` für OpenAI-Transkription
und `gpt-4o-mini` für OpenAI-Rewrite. Für neue Aufnahmen mit OpenAI kann als
Transkriptionsmodell `gpt-transcribe` eingetragen werden.

Modellnamen und Provider-Einstellungen werden in `%APPDATA%\\BlitzText\\settings.json`
gespeichert. API-Keys bleiben im Windows Credential Manager und werden nicht in
die Einstellungsdatei exportiert.
