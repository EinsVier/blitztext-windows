param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "src\BlitzText.Windows\BlitzText.Windows.csproj"
$output = Join-Path $repoRoot "publish\BlitzText.Windows"

if (Test-Path $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}

$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained:$selfContainedValue `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    --output $output

Write-Host "Published BlitzText Windows to: $output"
Write-Host "Run: $output\BlitzText.Windows.exe"
