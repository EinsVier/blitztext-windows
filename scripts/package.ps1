param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishScript = Join-Path $repoRoot "scripts\publish.ps1"
$publishDir = Join-Path $repoRoot "publish\BlitzText.Windows"
$stageRoot = Join-Path $repoRoot "publish\package-stage"
$stageDir = Join-Path $stageRoot "BlitzText"
$packageDir = Join-Path $repoRoot "publish\packages"
$timestamp = Get-Date -Format "yyyyMMdd-HHmm"
$zipPath = Join-Path $packageDir "BlitzText-Windows-$timestamp.zip"
$latestZipPath = Join-Path $packageDir "BlitzText-Windows-latest.zip"

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

if ($SelfContained) {
    & $publishScript -Configuration $Configuration -Runtime $Runtime -SelfContained
} else {
    & $publishScript -Configuration $Configuration -Runtime $Runtime
}

Assert-UnderDirectory -Path $stageRoot -Parent (Join-Path $repoRoot "publish")
if (Test-Path $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force (Join-Path $stageDir "app") | Out-Null
New-Item -ItemType Directory -Force $packageDir | Out-Null

Copy-Item -Path (Join-Path $publishDir "*") -Destination (Join-Path $stageDir "app") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\install-package-user.ps1") -Destination (Join-Path $stageDir "install.ps1") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\uninstall-user.ps1") -Destination (Join-Path $stageDir "uninstall.ps1") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination (Join-Path $stageDir "README.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination (Join-Path $stageDir "LICENSE") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "docs\installieren.md") -Destination (Join-Path $stageDir "INSTALLIEREN.md") -Force

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -Force
Copy-Item -LiteralPath $zipPath -Destination $latestZipPath -Force

Write-Host "Packaged BlitzText Windows to: $zipPath"
Write-Host "Latest package copy: $latestZipPath"
Write-Host "Install from extracted package with: .\install.ps1"
