[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PluginDll,

    [Parameter(Mandatory = $true)]
    [string]$PluginVersion,

    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $ProjectDir "release"
}

$PluginDll = [IO.Path]::GetFullPath($PluginDll)
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (-not (Test-Path -LiteralPath $PluginDll -PathType Leaf)) {
    throw "Plugin DLL not found. Build the plugin before packaging."
}

function Reset-ChildDirectory {
    param(
        [string]$Path,
        [string]$Parent
    )

    $resolvedPath = [IO.Path]::GetFullPath($Path)
    $resolvedParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $resolvedPath.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside the repository build area."
    }

    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $resolvedPath | Out-Null
}

$BepInExRoot = & (Join-Path $ProjectDir "scripts\Acquire-BepInEx.ps1")
$BuildRoot = Join-Path $ProjectDir ".build"
$StageRoot = Join-Path $BuildRoot "package"
New-Item -ItemType Directory -Path $BuildRoot -Force | Out-Null
Reset-ChildDirectory -Path $StageRoot -Parent $BuildRoot

Get-ChildItem -LiteralPath $BepInExRoot -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $StageRoot -Recurse -Force
}

$pluginDirectory = Join-Path $StageRoot "BepInEx\plugins"
New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
Copy-Item -LiteralPath $PluginDll -Destination (Join-Path $pluginDirectory "SP2FreeCamera.dll") -Force

$packageReadmeTemplate = Get-Content -LiteralPath (Join-Path $ProjectDir "packaging\README-SP2FreeCamera.txt") -Raw
$packageReadme = $packageReadmeTemplate.Replace("{PLUGIN_VERSION}", $PluginVersion)
[IO.File]::WriteAllText(
    (Join-Path $StageRoot "README-SP2FreeCamera.txt"),
    $packageReadme,
    (New-Object Text.UTF8Encoding($false)))

Copy-Item -LiteralPath (Join-Path $ProjectDir "THIRD_PARTY_NOTICES.md") `
    -Destination (Join-Path $StageRoot "THIRD_PARTY_NOTICES.txt")
$licenseDestination = Join-Path $StageRoot "licenses"
New-Item -ItemType Directory -Path $licenseDestination -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $ProjectDir "third_party\licenses") -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $licenseDestination
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$archiveName = "SP2FreeCamera-v$PluginVersion-win-x64.zip"
$archivePath = Join-Path $OutputDirectory $archiveName
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory(
    $StageRoot,
    $archivePath,
    [IO.Compression.CompressionLevel]::Optimal,
    $false)

& (Join-Path $ProjectDir "verify-release.ps1") `
    -ArchivePath $archivePath `
    -PluginVersion $PluginVersion

$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = Join-Path $OutputDirectory "SHA256SUMS.txt"
$checksumLine = "$archiveHash  $archiveName`n"
[IO.File]::WriteAllText($checksumPath, $checksumLine, (New-Object Text.UTF8Encoding($false)))

Write-Host "Packaged: release\$archiveName"
Write-Host "SHA-256: $archiveHash"
