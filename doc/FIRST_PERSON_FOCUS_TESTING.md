# Native first-person target lock: behavior and regression checklist

Native first-person target locking and Backspace recenter are included in
v0.6.11. The maintainer confirmed the basic behavior and approved the implementation
for release. Repeat this checklist after camera/input changes; that approval is
not a claim that every camera configuration or future build has been tested.

## Scope and implementation

- Independent first-person focus state; no freecam activation or movement-key
  capture. The default-on switch is `[FirstPerson] FocusEnabled`.
- Middle picks terrain, moving ground targets and other aircraft parts. Main
  `-` reads the current game target. It never writes the game's selected target.
- Only classic cockpit, player FPV and ordinary first-person camera parts are
  supported. Weapon/targeting pods and third-person cameras are excluded.
- Native Look At Cockpit has priority, including its cached controller flag.
- Capture native look angles after the base input update, before derived
  auto-centering. After each native pose update, retain its reference rotation
  and the seated FPV's native roll correction while replacing local aim.
- Use fresh focus coordinates for every native pose write, including physics,
  rendered LateUpdate and seated IK updates. Only input/loss timers use the real
  rendered frame counter. Do not copy the Look At Cockpit tracking algorithm.
- Leave position, focal position, FOV, physical transforms, targeting pods and
  weapon/network logic untouched. Existing freecam tracking/roll policy stays
  unchanged; only hit classification is shared.
- A manual native look/recenter releases the lock. Seed native look caches at
  release; changing cameras clears the separate state. Brief target loss holds
  the last absolute point for 0.2 seconds, then hands control back.
- Outside freecam, Backspace releases any target and invokes native recenter,
  with or without an active lock. Native roll and recenter transitions are
  retained; no new direct camera rotation write is introduced by the hotkey.
  It also recenters the local look offset under Look At Cockpit without changing
  that mode's priority. In freecam, Backspace keeps its focus-on-self behavior.
- Recenter has priority over same-frame picking/target acquisition, respects
  input/modal/pause guards and does not stack an already-running recenter tween.
- An active recenter animation must finish before acquiring another lock.

## Automated checks

Run each command in a fresh PowerShell process:

```powershell
.\test-first-person-focus.ps1
.\test-camera-input.ps1
.\test-auto-fov.ps1
.\test-movement.ps1
.\scripts\Verify-FirstPersonHooks.ps1 -GameDir $env:SP2_GAME_DIR
```

The focus tests run the real extension, math, picker and patch entry points
against explicit Unity/game doubles. Quaternion calculations are mathematical;
physics hits, native pose outputs and input timing are controlled fixtures.
The hook verifier checks the installed Game.dll without executing game code.
Neither test proves Harmony installation or visual behavior inside the game.

## In-game regression checklist

1. In each supported camera, middle-click terrain, a moving ground target and a
   remote aircraft part. Verify that the clicked point stays centered while
   either the camera's aircraft or the target moves. Test 30/60/high frame rates.
2. Bank through 90 degrees and inverted flight. Test different camera mounting
   angles, Auto Orient on/off and different native Chicken Head values. Roll
   should follow the corresponding native policy, not reset to world horizontal.
3. Test a directly overhead/below target, near/coincident target, large-distance
   floating-origin shift and transient/lost target. No NaN pose or stuck camera.
4. Press main `-` with/without a current game target; change the game selection
   afterward. Only pressing `-` again should adopt the new selection. Middle
   picking must not change the game's weapon target.
5. Test active and newly enabled Look At Cockpit. Neither middle nor `-` may
   override it. Turning it off must not revive an old hidden lock.
6. Test a weapon/targeting pod, Orbit, Chase and Fly-by. Their middle interaction,
   orientation, targeting and zoom must remain native, before/after FPV locking.
7. Release through left-drag, MouseLook, native look axes and recenter. A normal
   release should begin from the locked direction. Test a mouse click without
   drag and a recenter button that was previously disabled while centered.
   Test Backspace with/without a lock in each supported first-person camera and
   with Look At Cockpit enabled. Confirm native center/roll and transition,
   repeat presses/holding, and simultaneous middle/target-key input. No new lock
   should override a recenter in that frame. Verify freecam still focuses self.
8. Cross target boxes and real menu controls; click cockpit buttons; type `-` in
   chat/console. Test plugin menus, pause, application focus loss and UI hiding.
   A middle capture must not turn into a native pan halfway through a gesture.
9. Switch away and back, respawn, change aircraft and reload the flight scene.
   No old target may be silently rearmed. In cursor-locked FPV, picking uses the
   viewport center. On foot, native view-relative movement follows the view.
10. Verify native position/FOV/vehicle input, including W/A/S/D/Q/E. Then enter
    freecam while banked: roll must still zero, and existing focus, cinematic
    movement, zoom smoothing and automatic FOV must work as before.

## Local deployment

Close the game before replacing the DLL. Preserve the previous DLL for rollback
and do not overwrite a running game's loaded plugin. The feature's default
setting will be added by BepInEx on the next startup; existing values are kept.
Match the source version, built DLL and packaged DLL when preparing a release.
