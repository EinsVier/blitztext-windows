param(
    [string]$InstallDir
)

$ErrorActionPreference = "Stop"

$localAppData = [Environment]::GetFolderPath("LocalApplicationData")
$installRoot = Join-Path $localAppData "BlitzText"
$registryKey = "HKCU:\Software\EinsVier\BlitzText"
$registeredInstallDir = if (Test-Path $registryKey) {
    (Get-ItemProperty -Path $registryKey -Name "InstallDir" -ErrorAction SilentlyContinue).InstallDir
} else {
    $null
}
$defaultInstallDir = Join-Path $installRoot "app"
$installDir = if (-not [string]::IsNullOrWhiteSpace($InstallDir)) {
    [System.IO.Path]::GetFullPath($InstallDir)
} elseif (-not [string]::IsNullOrWhiteSpace($registeredInstallDir)) {
    [System.IO.Path]::GetFullPath($registeredInstallDir)
} else {
    $defaultInstallDir
}
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

function Assert-SafeExistingInstallDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetPathRoot($fullPath)
    if ($fullPath.TrimEnd('\') -eq $root.TrimEnd('\')) {
        throw "Refusing to remove a drive root: $fullPath"
    }

    if ((Test-Path $fullPath) -and -not (Test-Path (Join-Path $fullPath "BlitzText.Windows.exe"))) {
        throw "Refusing to remove a folder that does not look like a BlitzText install: $fullPath"
    }
}

Get-Process BlitzText.Windows -ErrorAction SilentlyContinue | Stop-Process -Force

if (Test-Path $startupShortcut) {
    Remove-Item -LiteralPath $startupShortcut -Force
}

if (Test-Path $startMenuDir) {
    Remove-Item -LiteralPath $startMenuDir -Recurse -Force
}

Assert-SafeExistingInstallDirectory -Path $installDir
if (Test-Path $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}

if (Test-Path $registryKey) {
    Remove-Item -LiteralPath $registryKey -Recurse -Force
}

Write-Host "Uninstalled BlitzText app files and shortcuts."
Write-Host "User settings and Windows Credential Manager entries were left intact."
