[CmdletBinding()]
param(
    [string]$GameDir = $env:SP2_GAME_DIR,
    [string]$PluginDll
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'Pass -GameDir or set SP2_GAME_DIR.' }
if ([string]::IsNullOrWhiteSpace($PluginDll)) { $PluginDll = Join-Path $project 'bin/SP2FreeCamera.dll' }
$bepinex = & (Join-Path $PSScriptRoot 'Acquire-BepInEx.ps1')
Add-Type -Path (Join-Path $bepinex 'BepInEx/core/Mono.Cecil.dll')
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDir 'SimplePlanes 2_Data/Managed/Game.dll'))
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($PluginDll))
try {
    $patchType = $plugin.MainModule.GetType('SP2FreeCamera.FirstPersonFocusPatches')
    if ($null -eq $patchType) { throw 'The DLL does not contain the first-person extension.' }
    $count = 0
    foreach ($method in $patchType.Methods) {
        $descriptors = @($method.CustomAttributes | Where-Object {
            $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch'
        })
        if ($descriptors.Count -gt 1) { throw 'Multiple patch descriptors do not create multiple patch targets.' }
        foreach ($descriptor in $descriptors) {
            $typeName = $descriptor.ConstructorArguments[0].Value.FullName
            $methodName = $descriptor.ConstructorArguments[1].Value
            $type = $game.MainModule.GetType($typeName)
            if ($null -eq $type) { throw "Native camera type is missing: $typeName" }
            $targets = @($type.Methods | Where-Object Name -eq $methodName)
            if ($targets.Count -ne 1) { throw "Missing or ambiguous hook: $typeName.$methodName" }
            $target = $targets[0]
            if ($typeName -match 'TargetingPod|Orbit|Chase|FlyBy') { throw 'An excluded view has a direct patch.' }
            foreach ($parameter in $method.Parameters) {
                $name = $parameter.Name
                if ($name.StartsWith('___')) {
                    $fieldName = $name.Substring(3)
                    $field = @($type.Fields | Where-Object Name -eq $fieldName)
                    if ($field.Count -ne 1 -or $field[0].FieldType.FullName -ne $parameter.ParameterType.FullName) {
                        throw "Invalid Harmony field injection: $name"
                    }
                }
                elseif (!$name.StartsWith('__')) {
                    $nativeParameters = @($target.Parameters | Where-Object Name -eq $name)
                    if ($nativeParameters.Count -ne 1 -or
                        $nativeParameters[0].ParameterType.FullName -ne $parameter.ParameterType.FullName) {
                        throw "Invalid Harmony argument injection: $name"
                    }
                }
            }
            $count++
        }
    }
    if ($count -ne 15) { throw "Expected 15 hook descriptors, found $count." }
    $cameraNamespace = 'Assets.Scripts.Flight.Cameras.'
    $fields = @{
        InteractiveCameraController = @('_deltaRotation')
        FirstPersonCameraController = @('_lookAtCockpit', '_animatingRecenter')
        FirstPersonCharacterCameraController = @('_currentRotation', '_targetTransform', '_animatingRecenter')
    }
    foreach ($entry in $fields.GetEnumerator()) {
        $type = $game.MainModule.GetType($cameraNamespace + $entry.Key)
        foreach ($name in $entry.Value) {
            if (@($type.Fields | Where-Object Name -eq $name).Count -ne 1) { throw "Missing native field: $name" }
        }
    }
    foreach ($name in @('CockpitCameraController', 'FirstPersonCameraController', 'FirstPersonCharacterCameraController')) {
        $type = $game.MainModule.GetType($cameraNamespace + $name)
        $method = @($type.Methods | Where-Object Name -eq 'Update')[0]
        $baseCalls = @($method.Body.Instructions | Where-Object {
            $_.Operand -is [Mono.Cecil.MethodReference] -and
            $_.Operand.DeclaringType.FullName -eq ($cameraNamespace + 'InteractiveCameraController') -and
            $_.Operand.Name -eq 'Update'
        })
        if ($baseCalls.Count -ne 1) { throw "Native pose/input ordering changed: $name" }
    }
    Write-Host 'Verified: 15 unique hook targets, injected arguments/field, six native fields and three base-input/pose paths in the installed Game.dll.'
    Write-Host 'This is metadata/IL validation, not proof of Harmony detours or rendered behavior in a running game.'
}
finally {
    $plugin.Dispose()
    $game.Dispose()
}
