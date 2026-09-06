[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Exercise the real entry and pointer-routing methods with small Unity/UI doubles.
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

$controller = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\FreeCameraController.cs') -Raw
$runtime = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\FreeCameraRuntime.cs') -Raw
$controllerMethods = @(
    'public override void OnSelected()',
    'private void SyncLookAnglesFromCamera(bool resetRoll = false)',
    'internal void ProcessFrame(float unscaledDeltaTime)',
    'private void ProcessPointerInput()',
    'private void ProcessFocusSelectionInput()',
    'internal void ResetPointerState()'
) | ForEach-Object { Get-MethodSource $controller $_ }
$runtimeMethods = @(
    'internal bool CanProcessKeyboardInput()',
    'internal bool CanProcessPointerInput()',
    'internal bool CanProcessFocusSelectionInput()',
    'private bool? GetPointerCameraSurface(Vector2 screenPosition)'
) | ForEach-Object { Get-MethodSource $runtime $_ }

$testSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'tests\CameraInputTests.cs') -Raw
$testSource = $testSource.Replace('// CONTROLLER_METHODS', ($controllerMethods -join "`n"))
$testSource = $testSource.Replace('// RUNTIME_METHODS', ($runtimeMethods -join "`n"))
Add-Type -TypeDefinition $testSource
[SP2FreeCamera.Tests.CameraInputTests]::RunAll() | ForEach-Object {
    Write-Host "PASS: $_"
}
