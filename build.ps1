[CmdletBinding()]
param(
    [string]$GameDir = $env:SP2_GAME_DIR,
    [string]$Csc,
    [switch]$Deploy,
    [switch]$Package
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceDir = Join-Path $ProjectDir "src"
$OutputDir = Join-Path $ProjectDir "bin"
$Output = Join-Path $OutputDir "SP2FreeCamera.dll"

function Resolve-CSharpCompiler {
    param([string]$RequestedCompiler)

    if (-not [string]::IsNullOrWhiteSpace($RequestedCompiler)) {
        $resolved = [IO.Path]::GetFullPath($RequestedCompiler)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "C# compiler not found at the supplied path."
        }
        return $resolved
    }

    $pathCompiler = Get-Command "csc.exe" -ErrorAction SilentlyContinue
    if ($null -ne $pathCompiler) {
        return $pathCompiler.Source
    }

    $programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $vsWhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vsWhere -PathType Leaf) {
        $installationPath = & $vsWhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if (-not [string]::IsNullOrWhiteSpace($installationPath)) {
            $candidate = Join-Path $installationPath "MSBuild\Current\Bin\Roslyn\csc.exe"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return $candidate
            }
        }
    }

    throw "C# compiler not found. Install Visual Studio Build Tools or pass -Csc."
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    throw "Set SP2_GAME_DIR or pass -GameDir with the SimplePlanes 2 game directory."
}

$GameDir = [IO.Path]::GetFullPath($GameDir)
$GameExecutable = Join-Path $GameDir "SimplePlanes 2.exe"
$ManagedDir = Join-Path $GameDir "SimplePlanes 2_Data\Managed"
if (-not (Test-Path -LiteralPath $GameExecutable -PathType Leaf)) {
    throw "The supplied directory does not contain SimplePlanes 2.exe."
}

$Csc = Resolve-CSharpCompiler $Csc
$BepInExRoot = & (Join-Path $ProjectDir "scripts\Acquire-BepInEx.ps1")
$BepInExCore = Join-Path $BepInExRoot "BepInEx\core"

$references = @(
    (Join-Path $ManagedDir "mscorlib.dll"),
    (Join-Path $ManagedDir "netstandard.dll"),
    (Join-Path $ManagedDir "System.dll"),
    (Join-Path $ManagedDir "System.Core.dll"),
    (Join-Path $ManagedDir "System.Runtime.dll"),
    (Join-Path $ManagedDir "Game.dll"),
    (Join-Path $ManagedDir "Jundroo.Common.dll"),
    (Join-Path $ManagedDir "FishNet.Runtime.dll"),
    (Join-Path $ManagedDir "Rewired_Core.dll"),
    (Join-Path $ManagedDir "UnityEngine.dll"),
    (Join-Path $ManagedDir "UnityEngine.CoreModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.InputLegacyModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.IMGUIModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.PhysicsModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.TextRenderingModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.UIModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.UI.dll"),
    (Join-Path $BepInExCore "BepInEx.dll"),
    (Join-Path $BepInExCore "0Harmony.dll")
)

foreach ($reference in $references) {
    if (-not (Test-Path -LiteralPath $reference -PathType Leaf)) {
        throw "Required build reference is missing: $([IO.Path]::GetFileName($reference))"
    }
}

$sources = Get-ChildItem -LiteralPath $SourceDir -Filter "*.cs" -File | Sort-Object Name
if ($sources.Count -eq 0) {
    throw "No C# source files were found."
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$arguments = @(
    "/nologo",
    "/target:library",
    "/optimize+",
    "/deterministic+",
    "/langversion:7.3",
    "/nostdlib+",
    "/pathmap:$ProjectDir=.",
    "/out:$Output"
)
$arguments += $references | ForEach-Object { "/reference:$_" }
$arguments += $sources.FullName

& $Csc @arguments
if ($LASTEXITCODE -ne 0) {
    throw "SP2FreeCamera compilation failed with exit code $LASTEXITCODE."
}

$builtHash = (Get-FileHash -LiteralPath $Output -Algorithm SHA256).Hash
Write-Host "Built: bin\SP2FreeCamera.dll"
Write-Host "SHA-256: $builtHash"

if ($Deploy) {
    $runningGame = Get-Process -Name "SimplePlanes 2" -ErrorAction SilentlyContinue
    if ($null -ne $runningGame) {
        throw "SimplePlanes 2 is running. Close it before deployment."
    }

    $pluginDir = Join-Path $GameDir "BepInEx\plugins"
    New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    $deployed = Join-Path $pluginDir "SP2FreeCamera.dll"
    Copy-Item -LiteralPath $Output -Destination $deployed -Force

    $deployedHash = (Get-FileHash -LiteralPath $deployed -Algorithm SHA256).Hash
    if ($deployedHash -ne $builtHash) {
        throw "Deployed DLL hash does not match the build output."
    }

    Write-Host "Deployed: BepInEx\plugins\SP2FreeCamera.dll"
    Write-Host "Deployed SHA-256: $deployedHash"
}

if ($Package) {
    $pluginSource = Get-Content -LiteralPath (Join-Path $SourceDir "Plugin.cs") -Raw
    $versionMatch = [regex]::Match($pluginSource, 'PluginVersion\s*=\s*"([^"]+)"')
    if (-not $versionMatch.Success) {
        throw "PluginVersion could not be read from Plugin.cs."
    }

    & (Join-Path $ProjectDir "package.ps1") -PluginDll $Output -PluginVersion $versionMatch.Groups[1].Value
}
