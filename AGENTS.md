# Agent Notes

## Project

BlitzText Windows is a native Windows implementation of a speech-to-text and text-rewrite workflow. Treat it as a Windows-first app, not a direct Swift/macOS port.

## Architecture

- Keep provider boundaries explicit.
- Use `ITranscriptionProvider` for speech-to-text engines.
- Use `ITextRewriteProvider` for rewrite engines.
- Keep OpenAI, Ollama, and future local providers swappable.
- Do not add trading, email-sending, or automation actions unless explicitly requested; the app should produce text and leave control with the user.

## Current Stack

- C#
- .NET 8
- WPF
- Windows Forms bridge for tray and paste hotkeys
- NAudio for microphone recording

## Safety

- Do not commit API keys or generated local settings.
- Avoid copying brand assets from the original macOS project unless trademark/licensing has been reviewed.
- Keep the OpenAI API key in Windows Credential Manager; do not export it with settings backups.
