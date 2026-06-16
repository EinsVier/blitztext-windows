# BlitzText Installieren

## Hinweis zur Idee

Die urspruengliche Blitztext-Idee stammt von Christoph Magnussen:

- [blitztext.de](https://blitztext.de/)
- [Speech-to-Text auf Knopfdruck: Meine Blitztext App!](https://youtu.be/ygfqOmDWj94)

Diese Windows-App ist eine eigenstaendige native Windows-Umsetzung, inspiriert von dieser Workflow-Idee.

## Empfohlen: winget

BlitzText Windows ist im Windows Package Manager verfuegbar:

```powershell
winget install --id EinsVier.BlitzText -e
```

winget installiert das WiX-MSI und beruecksichtigt die benoetigte .NET 8 Windows Desktop Runtime.

## Alternative: ZIP-Paket

1. ZIP entpacken.
2. PowerShell im entpackten Ordner oeffnen.
3. Installieren:

```powershell
.\install.ps1
```

Die App wird fuer den aktuellen Benutzer nach `%LOCALAPPDATA%\BlitzText\app` installiert. Es werden ein Startmenue-Eintrag und standardmaessig ein Autostart-Eintrag angelegt.

## Ohne Autostart

```powershell
.\install.ps1 -NoStartup
```

## Ohne direkten Start nach Installation

```powershell
.\install.ps1 -NoLaunch
```

## Deinstallieren

```powershell
.\uninstall.ps1
```

## MSI bauen

Das reproduzierbare MSI wird ueber WiX gebaut:

```powershell
.\scripts\build-wix-msi.ps1
```

Ausgabe:

```text
publish\msi\BlitzText-Windows-<Version>-win-x64.msi
```

Das Visual-Studio-Installer-Projekt unter `setup\BlitzText.Setup.vdproj` bleibt als Alternative erhalten, der WiX-Weg ist aber der bevorzugte automatisierbare Build.

Der MSI-Installer zeigt eine Dialogfolge mit Hinweistext, Installationspfad, Installationsbestaetigung, Fortschritt und Abschluss an.
