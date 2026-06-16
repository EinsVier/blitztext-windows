# BlitzText Windows

Native Windows implementation of the BlitzText workflow idea: press a hotkey, speak, transcribe the recording, optionally rewrite the text, and copy or paste it into the app you were using.

This project is a Windows-first implementation, not a Swift/macOS code port. No macOS source files or brand assets are copied into this repository.

## Inspiration

The original Blitztext idea comes from Christoph Magnussen. His macOS-focused open-source experiment is documented at:

- [blitztext.de](https://blitztext.de/)
- [Speech-to-Text auf Knopfdruck: Meine Blitztext App!](https://youtu.be/ygfqOmDWj94)

This Windows app is an independent native Windows implementation inspired by that workflow idea. It is not an official Christoph Magnussen or BLACKBOAT release.

## Status

BlitzText Windows is an open-source preview. It is useful for daily testing, but it is not a polished commercial product. Use it at your own risk, especially because it works with microphone input, global hotkeys, the clipboard, simulated paste, and optional online or local AI providers.

Provider behavior depends on your configuration:

- OpenAI transcription and rewrite requests send audio or text to the OpenAI API using your own API key.
- Ollama rewrite requests are sent to the configured local or network Ollama server.
- Local Whisper transcription runs through your configured `whisper.cpp` executable and model file.

## Current MVP

- WPF desktop app for Windows.
- Tray icon with open, workflow, and exit commands.
- Configurable workflow hotkeys, one hotkey per workflow.
- App language setting for German and English.
- Dictation language setting for automatic detection, German, or English.
- Hotkey detection dialog for checking special keys such as Browser Home.
- Optional middle mouse button trigger.
- Microphone recording to a temporary WAV file via NAudio.
- OpenAI transcription provider.
- Local `whisper.cpp` transcription provider.
- Local Whisper model picker, path browse buttons, test button, and configurable timeout.
- Text rewrite provider abstraction.
- OpenAI rewrite provider.
- OpenRouter rewrite provider.
- Anthropic rewrite provider for Claude models.
- Ollama rewrite provider via `http://localhost:11434/api/chat`.
- Ollama connection test via `/api/tags`.
- Optional Ollama warm-up with `keep_alive`.
- Step-by-step processing status and cancel button while processing.
- OpenAI API key storage in Windows Credential Manager.
- Custom names/vocabulary context for transcription and rewrite prompts.
- Editable workflow prompts for improve, calm, and emoji modes.
- Prompt presets for the Improve workflow, such as friendly email, short chat, bullet points, task lists, meeting notes, customer replies, decision notes, how-to guides, concise professional messages, AI prompts, AI image prompts, music prompts, and technical text.
- Workflows:
  - `Transcribe`
  - `Improve`
  - `Calm`
  - `Emojis`
- Result is copied to the clipboard.
- Optional target-aware auto-paste via simulated `Ctrl+V`.
- Local result history under `%APPDATA%\BlitzText\history.json`.
- Optional local result history.
- Settings export/import from the Backup tab.
- Manual update check from the Backup tab through a small `latest.json` manifest.

## Requirements

- Windows 10/11.
- .NET 8 Desktop Runtime for running.
- .NET SDK for development.
- OpenAI API key for online transcription.
- Optional: OpenRouter API key for OpenRouter rewrite workflows.
- Optional: Anthropic API key for Claude rewrite workflows.
- Optional: Ollama installed and running for local rewrite workflows.
- Optional: `whisper.cpp` executable and a Whisper model file for local transcription.

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run --project src\BlitzText.Windows
```

## Install With winget

BlitzText Windows is available through the Windows Package Manager:

```powershell
winget install --id EinsVier.BlitzText -e
```

winget installs the WiX MSI package and resolves the required .NET 8 Windows Desktop Runtime dependency.

## Publish

Create a local runnable build:

```powershell
.\scripts\publish.ps1
```

The app will be written to:

```text
publish\BlitzText.Windows\BlitzText.Windows.exe
```

For a build that does not require a separately installed .NET Desktop Runtime:

```powershell
.\scripts\publish.ps1 -SelfContained
```

## Package

Create a ZIP package with app files, `install.ps1`, `uninstall.ps1`, and README:

```powershell
.\scripts\package.ps1
```

Create a ZIP package that does not require a separately installed .NET Desktop Runtime:

```powershell
.\scripts\package.ps1 -SelfContained
```

The ZIP is written to:

```text
publish\packages
```

The package script also refreshes:

```text
publish\packages\BlitzText-Windows-latest.zip
```

## Update Check

BlitzText does not silently replace itself in the background. The Backup tab can check a small update manifest and open the configured download page when a newer version is available.

The default manifest URL is:

```text
https://raw.githubusercontent.com/EinsVier/blitztext-windows/master/update/latest.json
```

Manifest format:

```json
{
  "version": "0.4.0",
  "url": "https://github.com/EinsVier/blitztext-windows/releases/latest",
  "notesUrl": "https://github.com/EinsVier/blitztext-windows/releases/latest"
}
```

After extracting the ZIP, install for the current Windows user:

```powershell
.\install.ps1
```

## MSI Setup Project

The recommended automated MSI build uses WiX:

```powershell
.\scripts\build-wix-msi.ps1
```

The MSI is written to:

```text
publish\msi
```

The WiX MSI includes a dialog flow with notice text, install directory selection, installation confirmation, progress, and completion pages.

The older Visual Studio Installer Projects setup project is stored in:

```text
setup\BlitzText.Setup.vdproj
```

It is kept as an alternative for Visual Studio users. Open the solution in Visual Studio after installing the `Microsoft Visual Studio Installer Projects` extension. Publish the app first with `.\scripts\publish.ps1`, then add the files from `publish\BlitzText.Windows` to the setup project's Application Folder.

After the setup project is configured, build the MSI from PowerShell:

```powershell
.\scripts\build-msi.ps1
```

## Install For Current User

Install BlitzText into `%LOCALAPPDATA%\BlitzText\app`, create a Start Menu shortcut, and add it to Windows startup:

```powershell
.\scripts\install-user.ps1
```

Install without autostart:

```powershell
.\scripts\install-user.ps1 -NoStartup
```

Install or update without launching the app afterwards:

```powershell
.\scripts\install-user.ps1 -NoLaunch
```

Install as self-contained build:

```powershell
.\scripts\install-user.ps1 -SelfContained
```

Uninstall app files and shortcuts:

```powershell
.\scripts\uninstall-user.ps1
```

## Notes

Settings are stored as JSON under `%APPDATA%\BlitzText\settings.json`. This includes provider settings and the selected keyboard or mouse trigger. API keys are stored separately in Windows Credential Manager under `BlitzText.OpenAI.ApiKey`, `BlitzText.OpenRouter.ApiKey`, and `BlitzText.Anthropic.ApiKey`.

The Prompts tab stores custom names and workflow instructions in settings. Custom names are passed as context to OpenAI transcription when supported and to rewrite providers such as OpenAI or Ollama.

The Backup tab can export and import provider settings, hotkeys, prompts, and workflow choices. API keys are intentionally not exported and remain in Windows Credential Manager.

Windows cannot reliably use the physical `Fn` key as an app hotkey because it is usually handled by the keyboard firmware. BlitzText therefore uses Windows-visible combinations such as `Ctrl+Shift+F8` through `Ctrl+Shift+F12`.

Ollama is used for text rewriting only. Local speech-to-text is available as a separate `LocalWhisper` transcription provider. It calls a configured `whisper.cpp` executable with the configured model path and reads the generated transcript. The dictation language setting is passed to local Whisper as `auto`, `de`, or `en`.

## License

BlitzText Windows is released under the MIT License. See [LICENSE](LICENSE).
