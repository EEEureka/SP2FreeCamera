# Cinematic/general mode: behavior and regression checklist

This checklist covers the cinematic/general mode switch included in v0.6.10.
The maintainer tested the local implementation and approved it for release.
Repeat the checks below after changes to movement, keyboard ownership or game
input integration; that approval does not imply every future build is validated.

## Behavior

- Press **0 on the main keyboard** while free camera is active to toggle
  cinematic movement. Numpad 0 is not the default shortcut.
- The mode is **enabled by default**. The selected state is saved as
  `[Movement] CinematicModeEnabled` in
  `BepInEx/config/local.sp2.freecamera.cfg` and reused on the next session.
- With cinematic mode on, movement keys (W/A/S/D/Q/E by default) and quick-menu
  buttons move the camera using the existing normal/fast speed and acceleration.
- With cinematic mode off, general mode holds the camera position. Switching
  off immediately clears position velocity and queued quick-menu movement;
  movement keys return to the native Craft/Character controls. Originally
  disabled bindings remain disabled, and other control-map categories and
  modified-key bindings are not changed.
- Mouse look, target focus, manual FOV and automatic FOV work in both modes.
  The mode toggle does not clear the target, interrupt a drag or reset FOV.
- Both menus show the selected mode and provide a toggle. Quick-menu movement
  and speed buttons are disabled in general mode. The full menu can also save
  the desired mode before entering free camera.
- The shortcut does not act outside free camera or while the full menu,
  dialogs, text/chat input, console or application focus prevent keyboard input.
- The selected normal/fast speed is separate from this mode switch. Switching
  cinematic movement off/on retains that speed choice within the same free-camera
  session but always clears previous movement velocity.

## Automated checks

```powershell
.\test-camera-input.ps1
.\test-auto-fov.ps1
.\test-movement.ps1
```

The input tests execute extracted production methods and the full keyboard-capture
class against minimal Unity/UI/Rewired doubles. Mode coverage includes defaults,
keyboard gates, immediate stopping, pointer/FOV independence, exact binding
restoration, repeated switches, controls reloads and Rewired lifecycle events.
The configuration double models saved values; it does not verify disk I/O.
These tests do not replace native game input or rendering checks.

## In-game acceptance checklist

1. Enter free camera with no saved mode preference: cinematic movement is on.
   Confirm the existing movement, acceleration and normal/fast speed behavior.
2. While moving (test both speed settings), press main-keyboard 0. The camera
   must stop immediately without sliding. W/A/S/D/Q/E must now control the
   aircraft according to the game's own bindings, including Q/E yaw.
3. In general mode, try every quick-menu movement button. None may move the
   camera. Confirm mouse drag, manual zoom and target selection still work.
4. Lock self, another game target, a part and a terrain point. Test both manual
   and automatic FOV. Switch modes while zoom is smoothing or while left-dragging;
   the mode change must not reset those operations or the current focus.
5. Turn cinematic mode back on. The camera must accelerate from rest with no
   old quick-menu input or drift; movement keys must stop controlling the craft.
6. Repeat several switches, exit and re-enter free camera, then restart the
   game. Confirm both saved off and saved on states are reused, and native
   movement controls are restored after exiting free camera in either mode.
7. Try 0 in chat/text fields, dialogs, the console and the full plugin menu.
   Typing must not toggle the mode. Try Numpad 0 in the game view: it must not
   toggle with the default binding.
8. Where practical, reload game controls or switch between a craft and a
   character. Confirm the selected mode retains the correct keyboard ownership.
