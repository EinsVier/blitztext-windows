param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishScript = Join-Path $repoRoot "scripts\publish.ps1"
$publishDir = Join-Path $repoRoot "publish\BlitzText.Windows"
$wixDir = Join-Path $repoRoot "setup\wix"
$productWxs = Join-Path $wixDir "Product.wxs"
$generatedWxs = Join-Path $wixDir "PublishedFiles.generated.wxs"
$msiDir = Join-Path $repoRoot "publish\msi"
$projectFile = Join-Path $repoRoot "src\BlitzText.Windows\BlitzText.Windows.csproj"

function Convert-ToWixId {
    param([Parameter(Mandatory = $true)][string]$Text)

    $bytes = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Text))
    return "id_" + [Convert]::ToHexString($bytes).Substring(0, 24)
}

function Escape-XmlAttribute {
    param([Parameter(Mandatory = $true)][string]$Text)

    return [System.Security.SecurityElement]::Escape($Text)
}

if ($SelfContained) {
    & $publishScript -Configuration $Configuration -Runtime $Runtime -SelfContained
} else {
    & $publishScript -Configuration $Configuration -Runtime $Runtime
}

if (-not (Test-Path (Join-Path $publishDir "BlitzText.Windows.exe"))) {
    throw "Published app is missing BlitzText.Windows.exe: $publishDir"
}

[xml]$projectXml = Get-Content $projectFile
$version = $projectXml.Project.PropertyGroup.Version
if (-not $version) {
    $version = "0.1.0"
}

New-Item -ItemType Directory -Force $wixDir | Out-Null
New-Item -ItemType Directory -Force $msiDir | Out-Null

$fileItems = Get-ChildItem -Path $publishDir -File | Sort-Object Name
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
$lines.Add('  <Fragment>')
$lines.Add('    <ComponentGroup Id="PublishedFiles" Directory="APPFOLDER">')

foreach ($file in $fileItems) {
    $componentId = Convert-ToWixId "cmp:$($file.Name)"
    $fileId = Convert-ToWixId "file:$($file.Name)"
    $source = Escape-XmlAttribute $file.FullName
    $lines.Add("      <Component Id=""$componentId"" Guid=""*"">")
    $lines.Add("        <File Id=""$fileId"" Source=""$source"" KeyPath=""yes"" />")
    $lines.Add('      </Component>')
}

$lines.Add('    </ComponentGroup>')
$lines.Add('  </Fragment>')
$lines.Add('</Wix>')
Set-Content -LiteralPath $generatedWxs -Value $lines -Encoding UTF8

$msiPath = Join-Path $msiDir "BlitzText-Windows-$version-$Runtime.msi"
if (Test-Path $msiPath) {
    Remove-Item -LiteralPath $msiPath -Force
}

dotnet tool restore | Out-Host
dotnet wix extension add WixToolset.UI.wixext/5.0.2 | Out-Host
dotnet wix build $productWxs $generatedWxs `
    -arch x64 `
    -ext WixToolset.UI.wixext `
    -culture de-DE `
    -d "RepoRoot=$repoRoot" `
    -d "ProductVersion=$version" `
    -out $msiPath

if ($LASTEXITCODE -ne 0) {
    throw "WiX MSI build failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $msiPath)) {
    throw "WiX build completed, but MSI was not found: $msiPath"
}

Write-Host "MSI built: $msiPath"
