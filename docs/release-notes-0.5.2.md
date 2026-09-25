# BlitzText Windows 0.5.2

BlitzText 0.5.2 improves daily dictation and makes the local transcription setup easier to inspect.

## Highlights

- Recordings shorter than one second are no longer sent to the OpenAI transcription API; valid short dictations can start at 0.6 seconds.
- Choose the microphone used by BlitzText and see its live input level while recording.
- Use built-in dictation profiles for general text, email, technology and code, or Neukalen names; save your own profiles as well.
- Local `whisper.cpp` now receives the configured dictation prompt safely and shows useful diagnostics after transcription.
- OpenAI rewrite requests handle current GPT-6 models with their supported request options.
- The MSI remembers a custom install folder so that uninstall removes the same files again.
- The Improve workflow explicitly repairs missing spaces between words and restores sensible punctuation.

## Notes

The MSI installs for the current Windows user and requires the .NET 8 Desktop Runtime. The ZIP package includes scripts for per-user installation and removal.
