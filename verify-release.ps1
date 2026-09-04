[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [Parameter(Mandatory = $true)]
    [string]$PluginVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ArchivePath = [IO.Path]::GetFullPath($ArchivePath)
if (-not (Test-Path -LiteralPath $ArchivePath -PathType Leaf)) {
    throw "Release archive not found."
}

$manifestPath = Join-Path $ProjectDir "packaging\BepInEx-5.4.23.5-manifest.sha256"
$officialFiles = @{}
foreach ($line in Get-Content -LiteralPath $manifestPath) {
    if ($line -match '^([0-9a-fA-F]{64})  (.+)$') {
        $officialFiles[$Matches[2]] = $Matches[1].ToLowerInvariant()
    }
}
if ($officialFiles.Count -eq 0) {
    throw "The pinned BepInEx manifest is empty or invalid."
}

$expectedFiles = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($name in $officialFiles.Keys) {
    [void]$expectedFiles.Add($name)
}
foreach ($name in @(
    "BepInEx/plugins/SP2FreeCamera.dll",
    "README-SP2FreeCamera.txt",
    "THIRD_PARTY_NOTICES.txt",
    "licenses/BepInEx-MIT.txt",
    "licenses/UnityDoorstop-LGPL-2.1.txt",
    "licenses/HarmonyX-MIT.txt",
    "licenses/MonoMod-MIT.txt",
    "licenses/Mono.Cecil-MIT.txt"
)) {
    [void]$expectedFiles.Add($name)
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
try {
    $actualFiles = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $archive.Entries) {
        if ([string]::IsNullOrEmpty($entry.Name)) {
            continue
        }

        $name = $entry.FullName.Replace('\', '/')
        if ($name.StartsWith('/') -or $name.Contains('../') -or -not $actualFiles.Add($name)) {
            throw "The release archive contains an unsafe or duplicate entry."
        }
    }

    $missing = @($expectedFiles | Where-Object { -not $actualFiles.Contains($_) })
    $unexpected = @($actualFiles | Where-Object { -not $expectedFiles.Contains($_) })
    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        throw "Release contents differ from the allowlist. Missing: $($missing -join ', '); unexpected: $($unexpected -join ', ')"
    }
}
finally {
    $archive.Dispose()
}

$BuildRoot = Join-Path $ProjectDir ".build"
$VerifyRoot = Join-Path $BuildRoot "verify"
$resolvedVerify = [IO.Path]::GetFullPath($VerifyRoot)
$resolvedBuild = [IO.Path]::GetFullPath($BuildRoot).TrimEnd('\') + '\'
if (-not $resolvedVerify.StartsWith($resolvedBuild, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Invalid verification directory."
}
if (Test-Path -LiteralPath $resolvedVerify) {
    Remove-Item -LiteralPath $resolvedVerify -Recurse -Force
}
New-Item -ItemType Directory -Path $resolvedVerify | Out-Null
[IO.Compression.ZipFile]::ExtractToDirectory($ArchivePath, $resolvedVerify)

foreach ($entry in $officialFiles.GetEnumerator()) {
    $path = Join-Path $resolvedVerify $entry.Key.Replace('/', '\')
    $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $entry.Value) {
        throw "Pinned BepInEx file verification failed: $($entry.Key)"
    }
}

$pluginPath = Join-Path $resolvedVerify "BepInEx\plugins\SP2FreeCamera.dll"
$pluginInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($pluginPath)
if ($pluginInfo.FileVersion -ne "$PluginVersion.0" -or $pluginInfo.ProductVersion -ne $PluginVersion) {
    throw "Packaged plugin version does not match the requested release version."
}

$bepInExInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo(
    (Join-Path $resolvedVerify "BepInEx\core\BepInEx.dll"))
if ($bepInExInfo.FileVersion -ne "5.4.23.5") {
    throw "Packaged BepInEx version is not 5.4.23.5."
}

$privatePathNeedles = @(
    [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile),
    [IO.Path]::GetFullPath($ProjectDir)
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

foreach ($file in Get-ChildItem -LiteralPath $resolvedVerify -Recurse -File) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $ascii = [Text.Encoding]::ASCII.GetString($bytes)
    $unicode = [Text.Encoding]::Unicode.GetString($bytes)
    foreach ($needle in $privatePathNeedles) {
        if ($ascii.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
            $unicode.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Release privacy verification found a local path in $($file.Name)."
        }
    }
}

$textExtensions = @('.txt', '.ini', '.xml')
foreach ($file in Get-ChildItem -LiteralPath $resolvedVerify -Recurse -File | Where-Object {
    $textExtensions -contains $_.Extension.ToLowerInvariant()
}) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    if ($text -match '(?i)[A-Z]:\\Users\\[^\\\r\n]+' -or
        $text -match '(?i)\\\\[^\\\s]+\\[^\\\s]+' -or
        $text -match '(?i)\b(?:10\.|192\.168\.|172\.(?:1[6-9]|2[0-9]|3[01])\.)\d{1,3}(?:\.\d{1,3}){2}\b') {
        throw "Release privacy verification found a user path, UNC path, or private-network address in $($file.Name)."
    }
}

Write-Host "Release verification passed: $([IO.Path]::GetFileName($ArchivePath))"
