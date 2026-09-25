# Local Transcription

BlitzText supports OpenAI for online speech-to-text and `LocalWhisper` for local speech-to-text.

`LocalWhisper` calls an external `whisper.cpp` executable. This keeps the app lightweight and avoids bundling native inference libraries in the first Windows MVP.

## Requirements

- A Windows build of `whisper.cpp`, for example `whisper-cli.exe`.
- A local Whisper model file, for example a `.bin` model used by `whisper.cpp`.
- Both paths entered on the Provider tab.

## Runtime Shape

- `AudioRecorderService` records WAV audio.
- `LocalWhisperTranscriptionProvider` runs:

```text
whisper-cli.exe -m "<model>" -f "<audio.wav>" -otxt -of "<temp-output>"
```

- If configured, the transcription hint and custom names are passed to
  `whisper.cpp` as an initial `--prompt`. This helps local Whisper preserve
  domain terms, names, and preferred spellings.
- BlitzText can select a specific Windows microphone and shows its live peak
  level while recording. The Results tab records the provider, model, language,
  microphone peak, configured Whisper start prompt, and matching VitisAI cache
  status for the last transcription.
- The Prompts tab offers dictation profiles for general text, email, technology,
  and Neukalen/proper names. Applying a profile fills the transcription hint and
  relevant spelling context; the immediately previous context can be restored.
- The current transcription hint and spelling context can also be saved under a
  custom profile name. Custom profiles are included in settings export and import.
- The generated text file is read and returned to `BlitzWorkflowRunner`.
- Rewrite workflows continue unchanged and can still use OpenAI or Ollama.
- Missing EXE/model paths are surfaced as clear app errors.
