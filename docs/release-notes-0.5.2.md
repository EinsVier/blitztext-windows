# BlitzText Windows 0.5.2

BlitzText 0.5.2 improves daily dictation and makes the local transcription setup easier to inspect.

## Highlights

- Recordings shorter than one second are no longer sent to the OpenAI transcription API; valid short dictations can start at 0.6 seconds.
- Choose the microphone used by BlitzText and see its live input level while recording.
- Use built-in dictation profiles for general text, email, technology and code, or Neukalen names; save your own profiles as well.
- Local `whisper.cpp` now receives the configured dictation prompt safely and shows useful diagnostics after transcription.
- OpenAI rewrite requests handle current GPT-6 models with their supported request options.

## Notes

This document prepares the release. The public update manifest and download links remain on 0.5.1 until the 0.5.2 release artifacts have been published.
