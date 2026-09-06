[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Exercise real entry, pointer routing, movement modes and binding capture with
# small Unity/UI/Rewired doubles. Config doubles model the BepInEx save contract;
# they do not verify disk I/O or the timing of native game input consumption.
# This is a source-level regression test, not a substitute for in-game rendering QA.
function Get-MethodSource {
    param([string]$Source, [string]$Signature)

    $start = $Source.IndexOf($Signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Production method not found: $Signature" }
    $body = $Source.IndexOf('{', $start)
    $depth = 0
    for ($i = $body; $i -lt $Source.Length; $i++) {
        if ($Source[$i] -eq '{') { $depth++ }
        if ($Source[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $Source.Substring($start, $i - $start + 1) }
        }
    }
    throw "Unterminated production method: $Signature"
}

$controller = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\FreeCameraController.cs') -Raw -Encoding UTF8
$runtime = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\FreeCameraRuntime.cs') -Raw -Encoding UTF8
$controllerMethods = @(
    'public override void OnSelected()',
    'private void SyncLookAnglesFromCamera(bool resetRoll = false)',
    'internal void ProcessFrame(float unscaledDeltaTime)',
    'private void ProcessPointerInput()',
    'private void ProcessFocusSelectionInput()',
    'internal void ResetPointerState()',
    'internal void ResetMovement()',
    'private void ProcessMovement(float unscaledDeltaTime, bool allowInput)',
    'private static bool IsFinite(Vector3 value)'
) | ForEach-Object { Get-MethodSource $controller $_ }
$runtimeMethods = @(
    'internal bool CanProcessKeyboardInput()',
    'internal bool CanProcessPointerInput()',
    'internal bool CanProcessFocusSelectionInput()',
    'internal bool CanProcessFirstPersonKeyboardInput()',
    'internal bool CanProcessFirstPersonPointerInput(Vector2 position)',
    'private bool? GetPointerCameraSurface(Vector2 screenPosition)',
    'internal bool CinematicModeEnabled',
    'internal string CinematicModeStatusText',
    'internal Vector3 QuickMovementAxes',
    'internal void ToggleFastMode()',
    'private void ProcessCinematicModeHotkey(bool canProcessKeyboardInput)',
    'internal void SetCinematicModeEnabled(bool enabled)',
    'private void ApplyCinematicModeIfChanged()',
    'private void RefreshKeyboardCapture()'
) | ForEach-Object { Get-MethodSource $runtime $_ }

$testSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'tests\CameraInputTests.cs') -Raw -Encoding UTF8
$modeTests = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'tests\CinematicModeTests.cs') -Raw -Encoding UTF8
$plugin = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\Plugin.cs') -Raw -Encoding UTF8
foreach ($name in @('CinematicModeEnabled', 'ToggleCinematicModeKey')) {
    $binding = [regex]::Match($plugin, "(?s)\b$name = (?:Config\.Bind|BindKey)\(.*?\);")
    if (-not $binding.Success) { throw "Production configuration binding not found: $name" }
    $modeTests = $modeTests.Replace("// BIND_$name", $binding.Value)
}
$testSource = $testSource.Replace('// CONTROLLER_METHODS', ($controllerMethods -join "`n"))
$testSource = $testSource.Replace('// RUNTIME_METHODS', ($runtimeMethods -join "`n"))
$production = @('FreeCameraKeyboardCapture.cs', 'MovementIntegrator.cs', 'LogPrivacy.cs') |
    ForEach-Object { Get-Content -LiteralPath (Join-Path $PSScriptRoot "src\$_") -Raw -Encoding UTF8 }
# Move compilation-unit imports before all namespace declarations.
$sources = @($testSource, $modeTests) + $production
$imports = @($sources | ForEach-Object { [regex]::Matches($_, '(?m)^using [^\r\n]+;\r?$').Value }) | Select-Object -Unique
$bodies = $sources | ForEach-Object { [regex]::Replace($_, '(?m)^using [^\r\n]+;\r?$', '') }
Add-Type -TypeDefinition (($imports -join "`n") + "`n" + ($bodies -join "`n"))
[SP2FreeCamera.Tests.CameraInputTests]::RunAll() | ForEach-Object {
    Write-Host "PASS: $_"
}
[SP2FreeCamera.Tests.CinematicModeTests]::RunAll() | ForEach-Object {
    Write-Host "PASS: $_"
}
