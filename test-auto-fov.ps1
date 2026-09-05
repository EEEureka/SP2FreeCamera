[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Executes the production projection/state/smoothing code without Unity or a game.
Add-Type -Path @(
    (Join-Path $PSScriptRoot 'src\FovMath.cs'),
    (Join-Path $PSScriptRoot 'src\AutoFovState.cs'),
    (Join-Path $PSScriptRoot 'tests\AutoFovTests.cs')
)
[SP2FreeCamera.Tests.AutoFovTests]::RunAll() | ForEach-Object {
    Write-Host "PASS: $_"
}
