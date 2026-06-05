param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishScript = Join-Path $repoRoot "scripts\publish.ps1"
$setupSolution = Join-Path $repoRoot "BlitzText.Setup.sln"
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $setupSolution)) {
    throw "Setup solution not found: $setupSolution"
}

& $publishScript -Configuration $Configuration

$devenv = ""
if (Test-Path $vswhere) {
    $installPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.CoreEditor -property installationPath
    if ($installPath) {
        $candidate = Join-Path $installPath "Common7\IDE\devenv.com"
        if (Test-Path $candidate) {
            $devenv = $candidate
        }
    }
}

if (-not $devenv) {
    $fallback = "C:\Apps\Microsoft Visual Studio\18\Enterprise\Common7\IDE\devenv.com"
    if (Test-Path $fallback) {
        $devenv = $fallback
    }
}

if (-not $devenv) {
    throw "devenv.com was not found. Open Visual Studio Installer Projects in Visual Studio or add Visual Studio to PATH."
}

& $devenv $setupSolution /Build $Configuration

$msiPath = Join-Path $repoRoot "setup\$Configuration\BlitzText.Setup.msi"
if (Test-Path $msiPath) {
    Write-Host "MSI built: $msiPath"
} else {
    Write-Host "Setup build completed, but MSI was not found at: $msiPath"
}
