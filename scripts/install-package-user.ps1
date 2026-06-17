param(
    [switch]$NoStartup,
    [switch]$NoLaunch,
    [string]$InstallDir
)

$ErrorActionPreference = "Stop"

$packageRoot = Resolve-Path $PSScriptRoot
$packageAppDir = Join-Path $packageRoot "app"
$localAppData = [Environment]::GetFolderPath("LocalApplicationData")
$installRoot = Join-Path $localAppData "BlitzText"
$defaultInstallDir = Join-Path $installRoot "app"
$installDir = if ([string]::IsNullOrWhiteSpace($InstallDir)) { $defaultInstallDir } else { [System.IO.Path]::GetFullPath($InstallDir) }
$startMenuDir = Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs\BlitzText"
$startupDir = [Environment]::GetFolderPath("Startup")
$exePath = Join-Path $installDir "BlitzText.Windows.exe"
$registryKey = "HKCU:\Software\EinsVier\BlitzText"

function Assert-UnderDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Parent
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullParent = [System.IO.Path]::GetFullPath($Parent)
    if (-not $fullPath.StartsWith($fullParent, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside expected directory: $fullPath"
    }
}

function Assert-SafeInstallDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetPathRoot($fullPath)
    if ($fullPath.TrimEnd('\') -eq $root.TrimEnd('\')) {
        throw "Refusing to install directly into a drive root: $fullPath"
    }

    if (Test-Path $fullPath) {
        $hasBlitzTextExe = Test-Path (Join-Path $fullPath "BlitzText.Windows.exe")
        $isEmpty = -not (Get-ChildItem -LiteralPath $fullPath -Force -ErrorAction SilentlyContinue | Select-Object -First 1)
        if (-not $hasBlitzTextExe -and -not $isEmpty) {
            throw "Refusing to replace a non-empty folder that does not look like a BlitzText install: $fullPath"
        }
    }
}

if (-not (Test-Path (Join-Path $packageAppDir "BlitzText.Windows.exe"))) {
    throw "Package app folder is missing BlitzText.Windows.exe: $packageAppDir"
}

Get-Process BlitzText.Windows -ErrorAction SilentlyContinue | Stop-Process -Force

Assert-SafeInstallDirectory -Path $installDir
if (Test-Path $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}

New-Item -ItemType Directory -Force $installDir | Out-Null
Copy-Item -Path (Join-Path $packageAppDir "*") -Destination $installDir -Recurse -Force

New-Item -Path $registryKey -Force | Out-Null
Set-ItemProperty -Path $registryKey -Name "InstallDir" -Value $installDir

New-Item -ItemType Directory -Force $startMenuDir | Out-Null

$shell = New-Object -ComObject WScript.Shell
$startMenuShortcut = Join-Path $startMenuDir "BlitzText.lnk"
$shortcut = $shell.CreateShortcut($startMenuShortcut)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = $exePath
$shortcut.Save()

$startupShortcut = Join-Path $startupDir "BlitzText.lnk"
if ($NoStartup) {
    if (Test-Path $startupShortcut) {
        Remove-Item -LiteralPath $startupShortcut -Force
    }
} else {
    $shortcut = $shell.CreateShortcut($startupShortcut)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $installDir
    $shortcut.IconLocation = $exePath
    $shortcut.Save()
}

Write-Host "Installed BlitzText to: $installDir"
Write-Host "Start Menu shortcut: $startMenuShortcut"
if ($NoStartup) {
    Write-Host "Autostart: disabled"
} else {
    Write-Host "Autostart shortcut: $startupShortcut"
}

if (-not $NoLaunch) {
    Start-Process -FilePath $exePath -WorkingDirectory $installDir -WindowStyle Hidden
    Write-Host "BlitzText started."
}
