param(
    [switch]$SelfContained,
    [switch]$NoStartup,
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishScript = Join-Path $repoRoot "scripts\publish.ps1"
$publishDir = Join-Path $repoRoot "publish\BlitzText.Windows"
$localAppData = [Environment]::GetFolderPath("LocalApplicationData")
$installDir = Join-Path $localAppData "BlitzText\app"
$startMenuDir = Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs\BlitzText"
$startupDir = [Environment]::GetFolderPath("Startup")
$exePath = Join-Path $installDir "BlitzText.Windows.exe"

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

Get-Process BlitzText.Windows -ErrorAction SilentlyContinue | Stop-Process -Force

if ($SelfContained) {
    & $publishScript -SelfContained
} else {
    & $publishScript
}

Assert-UnderDirectory -Path $installDir -Parent (Join-Path $localAppData "BlitzText")
if (Test-Path $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}

New-Item -ItemType Directory -Force $installDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $installDir -Recurse -Force

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
Write-Host "Run now: $exePath"

if (-not $NoLaunch) {
    Start-Process -FilePath $exePath -WorkingDirectory $installDir -WindowStyle Hidden
    Write-Host "BlitzText started."
}
