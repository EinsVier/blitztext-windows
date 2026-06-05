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

- The generated text file is read and returned to `BlitzWorkflowRunner`.
- Rewrite workflows continue unchanged and can still use OpenAI or Ollama.
- Missing EXE/model paths are surfaced as clear app errors.
