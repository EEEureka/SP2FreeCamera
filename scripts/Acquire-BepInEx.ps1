[CmdletBinding()]
param(
    [string]$CacheDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$BepInExVersion = "5.4.23.5"
$AssetName = "BepInEx_win_x64_$BepInExVersion.zip"
$AssetUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$BepInExVersion/$AssetName"
$AssetSha256 = "82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4"
$RepositoryRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($CacheDirectory)) {
    $CacheDirectory = Join-Path $RepositoryRoot ".cache"
}

$CacheDirectory = [IO.Path]::GetFullPath($CacheDirectory)
New-Item -ItemType Directory -Path $CacheDirectory -Force | Out-Null

$ArchivePath = Join-Path $CacheDirectory $AssetName
$DownloadPath = $ArchivePath + ".download"
$ExtractRoot = Join-Path $CacheDirectory ("BepInEx_win_x64_" + $BepInExVersion)
$ExpectedCore = Join-Path $ExtractRoot "BepInEx\core\BepInEx.dll"

function Test-ArchiveHash {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    return $actual -eq $AssetSha256
}

function Assert-ChildPath {
    param(
        [string]$Path,
        [string]$Parent
    )

    $resolvedPath = [IO.Path]::GetFullPath($Path)
    $resolvedParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $resolvedPath.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the BepInEx cache."
    }
}

if (-not (Test-ArchiveHash $ArchivePath)) {
    if (Test-Path -LiteralPath $ArchivePath) {
        Remove-Item -LiteralPath $ArchivePath -Force
    }
    if (Test-Path -LiteralPath $DownloadPath) {
        Remove-Item -LiteralPath $DownloadPath -Force
    }

    Invoke-WebRequest -UseBasicParsing -Uri $AssetUrl -OutFile $DownloadPath
    if (-not (Test-ArchiveHash $DownloadPath)) {
        Remove-Item -LiteralPath $DownloadPath -Force
        throw "The downloaded BepInEx archive failed SHA-256 verification."
    }

    Move-Item -LiteralPath $DownloadPath -Destination $ArchivePath
}

if (-not (Test-Path -LiteralPath $ExpectedCore -PathType Leaf)) {
    Assert-ChildPath -Path $ExtractRoot -Parent $CacheDirectory
    if (Test-Path -LiteralPath $ExtractRoot) {
        Remove-Item -LiteralPath $ExtractRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $ExtractRoot | Out-Null
    Expand-Archive -LiteralPath $ArchivePath -DestinationPath $ExtractRoot
}

if (-not (Test-Path -LiteralPath $ExpectedCore -PathType Leaf)) {
    throw "The verified BepInEx archive did not contain the expected runtime."
}

Write-Output $ExtractRoot
