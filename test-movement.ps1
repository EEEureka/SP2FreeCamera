[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The production scalar integrator has no Unity dependencies, so these tests
# exercise the exact implementation without starting a game or a Unity runtime.
Add-Type -Path @(
    (Join-Path $PSScriptRoot 'src\MovementIntegrator.cs'),
    (Join-Path $PSScriptRoot 'tests\MovementIntegratorTests.cs')
)
[SP2FreeCamera.Tests.MovementIntegratorTests]::RunAll() | ForEach-Object {
    Write-Host "PASS: $_"
}
