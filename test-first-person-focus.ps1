[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Run in a fresh PowerShell process, like the other standalone test scripts.
# The real production module/math/picker and patch entry points run with explicit
# game/Unity doubles. This cannot prove rendering or native input timing in-game.
$paths = @('tests/FirstPersonFocusTests.cs', 'src/FirstPersonFocus.cs',
    'src/FirstPersonFocusMath.cs', 'src/FirstPersonFocusPatches.cs',
    'src/FocusSelection.cs', 'src/NumericUtility.cs')
$sources = $paths | ForEach-Object {
    Get-Content -LiteralPath (Join-Path $PSScriptRoot $_) -Raw -Encoding UTF8
}
$imports = @($sources | ForEach-Object { [regex]::Matches($_, '(?m)^using [^\r\n]+;\r?$').Value }) | Select-Object -Unique
$bodies = $sources | ForEach-Object { [regex]::Replace($_, '(?m)^using [^\r\n]+;\r?$', '') }
Add-Type -TypeDefinition (($imports -join "`n") + "`n" + ($bodies -join "`n"))
[SP2FreeCamera.Tests.FirstPersonFocusTests]::RunAll() | ForEach-Object {
    Write-Host "PASS: $_"
}
