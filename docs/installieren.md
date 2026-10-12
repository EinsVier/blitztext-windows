# BlitzText installieren

## Hinweis zur Idee

Die ursprüngliche Blitztext-Idee stammt von Christoph Magnussen:

- [blitztext.de](https://blitztext.de/)
- [Speech-to-Text auf Knopfdruck: Meine Blitztext App!](https://youtu.be/ygfqOmDWj94)

Diese Windows-App ist eine eigenstaendige native Windows-Umsetzung, inspiriert von dieser Workflow-Idee.

## Empfohlen: WinGet

Installiere die aktuelle Version aus dem offiziellen WinGet-Katalog:

```powershell
winget install --id EinsVier.BlitzText --exact --source winget
```

WinGet installiert das WiX-MSI und berücksichtigt die benötigte .NET 8 Desktop Runtime.

Mit benutzerdefiniertem Installationspfad, zum Beispiel `C:\Apps\BlitzText`:

```powershell
winget install --id EinsVier.BlitzText --exact --source winget --location "C:\Apps\BlitzText"
```

## Alternative: GitHub-Release-MSI

Lade das [aktuelle MSI aus dem GitHub-Release](https://github.com/EinsVier/blitztext-windows/releases/latest) herunter und starte es. Der Installer bietet eine Dialogauswahl für den Installationspfad und installiert für den aktuellen Windows-Benutzer. Das MSI benötigt die [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

## Alternative: ZIP-Paket

1. ZIP entpacken.
2. PowerShell im entpackten Ordner oeffnen.
3. Installieren:

```powershell
.\install.ps1
```

Die App wird fuer den aktuellen Benutzer nach `%LOCALAPPDATA%\BlitzText\app` installiert. Es werden ein Startmenue-Eintrag und standardmaessig ein Autostart-Eintrag angelegt.

Nach dem Start kannst du den Autostart links in den Bedienungseinstellungen mit **„BlitzText bei der Anmeldung starten“** jederzeit ein- oder ausschalten. Bei der Windows-Anmeldung startet BlitzText im Infobereich; das Hauptfenster bleibt geschlossen.

Mit benutzerdefiniertem Installationspfad:

```powershell
.\install.ps1 -InstallDir "C:\Apps\BlitzText"
```

## Ohne Autostart

Der Autostart laesst sich auch spaeter in BlitzText unter den Bedienungseinstellungen aendern. Bei der Installation kannst du ihn direkt ausschalten:

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

Wenn BlitzText in einen benutzerdefinierten Pfad installiert wurde, nutzt die Deinstallation den gespeicherten Installationspfad. Alternativ kann der Pfad explizit angegeben werden:

```powershell
.\uninstall.ps1 -InstallDir "C:\Apps\BlitzText"
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
