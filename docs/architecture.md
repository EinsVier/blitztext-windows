# Architecture

BlitzText Windows is split into four practical layers.

## Shell

The WPF app owns the main window, tray icon, workflow-specific global keyboard hotkeys, and optional middle mouse button trigger. Each workflow can have its own hotkey so the user can start dictation directly in the desired mode.

## Recording

`AudioRecorderService` records microphone input into a temporary 16 kHz mono WAV file with NAudio.

## Providers

Transcription and rewriting are separate capabilities:

- `ITranscriptionProvider`
- `ITextRewriteProvider`

The first transcription providers are OpenAI and local `whisper.cpp`. Rewrite providers include OpenAI, Ollama, OpenRouter, and Anthropic.

Transcription and rewrite provider enums are intentionally separate. This prevents text-only providers such as Ollama from being selected for audio transcription, and leaves a clean path for local Whisper-style engines.

OpenAI credentials are stored with Windows Credential Manager through `CredentialStore`. Non-secret preferences remain in `%APPDATA%\BlitzText\settings.json`.

Ollama settings can be checked with `OllamaConnectionTester`, which calls `/api/tags` and verifies that the configured rewrite model exists.

If enabled, BlitzText sends a tiny Ollama chat request with `keep_alive` so the configured rewrite model stays warm for faster follow-up rewrites.

Prompt customization lives in `AppSettings` and is applied through `PromptContextBuilder` and `WorkflowPromptFactory`. Custom names are included as vocabulary context for transcription and rewrite, while workflow-specific instructions are added to the rewrite prompt for improve, calm, and emoji modes.

## Workflows

`BlitzWorkflowRunner` coordinates the app workflow:

1. Record audio.
2. Transcribe audio.
3. Optionally build a rewrite prompt.
4. Send the prompt to the selected rewrite provider.
5. Copy the final text to the clipboard.

The visible workflows are `Transcribe`, `Improve`, and `Calm`.
`Improve` and `Calm` can optionally include the separately configured emoji instruction in the same rewrite request. The legacy `Emojis` enum value remains readable for settings and history compatibility but is no longer offered as a workflow.

The workflow runner accepts a progress callback so the UI can show whether it is recording, transcribing, rewriting, or waiting for a provider. During provider work, the record button becomes a cancel button backed by a cancellation token.

Workflow hotkeys are registered with `RegisterHotKey` in `GlobalHotkeyService`. The service maps Windows hotkey IDs back to `WorkflowKind`, and the UI stores the active workflow when recording starts.

`HotkeyCaptureWindow` is a focused key-detection dialog. It captures the next key press while the dialog is active, maps the virtual key and modifiers back to known `HotkeyOptions`, and applies the detected preset to the selected workflow.

Auto-paste uses `TargetWindowService` to capture the foreground window before recording starts. After transcription/rewrite, BlitzText reactivates that window and only then sends `Ctrl+V`. If the target window no longer exists, the text remains in the clipboard and the UI reports that paste was skipped.
