$ErrorActionPreference = "Stop"

$localAppData = [Environment]::GetFolderPath("LocalApplicationData")
$installRoot = Join-Path $localAppData "BlitzText"
$installDir = Join-Path $installRoot "app"
$startMenuDir = Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs\BlitzText"
$startupShortcut = Join-Path ([Environment]::GetFolderPath("Startup")) "BlitzText.lnk"

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

if (Test-Path $startupShortcut) {
    Remove-Item -LiteralPath $startupShortcut -Force
}

if (Test-Path $startMenuDir) {
    Remove-Item -LiteralPath $startMenuDir -Recurse -Force
}

Assert-UnderDirectory -Path $installDir -Parent $installRoot
if (Test-Path $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}

Write-Host "Uninstalled BlitzText app files and shortcuts."
Write-Host "User settings and Windows Credential Manager entries were left intact."
