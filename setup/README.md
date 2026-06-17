# BlitzText MSI Setup Project

This folder contains installer project files.

The preferred automated MSI build is WiX:

```powershell
.\scripts\build-wix-msi.ps1
```

It publishes the app, harvests the published files, creates a per-user MSI, and writes it to:

```text
publish\msi
```

The Visual Studio Installer Projects `.vdproj` remains available as a manual alternative.

## Build input

Build or publish the app first:

```powershell
.\scripts\publish.ps1
```

Use this folder as installer input:

```text
publish\BlitzText.Windows
```

## Visual Studio setup

Open `BlitzText.Setup.sln` in Visual Studio and open `setup\BlitzText.Setup.vdproj`.

In the setup project's **File System** editor:

- Add the published files from `publish\BlitzText.Windows` to **Application Folder**.
- Create a shortcut to `BlitzText.Windows.exe` under **User's Programs Menu**.
- Optional: create a desktop shortcut.
- Set product metadata:
  - ProductName: `BlitzText Windows`
  - Manufacturer: `EinsVier`
  - Version: `0.4.1`

Avoid adding the OpenAI API key or user settings to the installer. Those remain under `%APPDATA%\BlitzText` and Windows Credential Manager.
