# Automatic FOV: test checklist

Use this checklist after installing or updating the plugin. Restart the game
after replacing the DLL. It applies to both release packages and local test builds.

## Controls and expected behavior

- `Insert`: enter free camera.
- `=` on the main keyboard: toggle automatic FOV. The default binding is
  `ToggleAutoFov = Equals`; the numeric-keypad `+` is a different key.
- The full settings menu and quick menu also provide a toggle and show whether
  the mode is off, waiting for a target, or active.
- While active, the mouse wheel changes desired reference **area** relative to
  the framing captured on activation or target selection (`100%`). This is not
  a measurement of the real aircraft's size or its screen coverage.
- Automatic and manual FOV share **FOV smoothing time**. A nonzero value allows
  temporary size drift during rapid distance changes. Use `0` to check the exact
  geometric compensation without that intentional delay.
- The mode starts off when the plugin loads. Its switch is retained when
  entering/exiting free camera during that game session, but framing is recaptured
  for each newly selected target. The switch itself is not saved to configuration.

## In-game acceptance

1. With no focus, enable automatic FOV. Expect **waiting for a target**, normal
   manual wheel zoom, and no forced FOV change.
2. Focus terrain with the middle button, a part with the middle button, yourself
   with `Backspace`, and the selected game target with `-`. For each type, enable
   automatic FOV at a suitable framing and move toward/away from the focus point.
   Expect wider/narrower FOV without automatic camera translation.
3. Scroll while tracking. Expect a new desired reference-area percentage and a
   smooth zoom toward that framing; distance changes then retain the new ratio.
4. Compare **FOV smoothing time** at `0`, `0.12`, and `0.3` seconds. The aiming
   direction of moving targets must remain immediate and independent of zoom.
5. Disable automatic FOV during a zoom. Expect the currently displayed FOV to be
   retained, with the wheel returning to manual zoom; do not restore an old FOV.
6. Start a left-mouse drag. Expect focus to clear, automatic FOV to wait, and manual
   look/zoom to work. Lock again: expect a fresh reference without an FOV jump.
7. Switch to another target at a different distance. Expect the current FOV to be
   the new baseline, not a jump to the previous target's framing.
8. Destroy/unload the focused target. Expect stale auto-zoom to stop immediately
   and the mode to remain armed; ordinary existing lost-focus messages may occur.
9. Reach the minimum/maximum FOV, or enter the virtual unit sphere. Expect silent
   clamping, no limit notifications, and no NaN or infinite FOV. Returning to a
   usable distance restores the requested ratio. Reverse wheel input at a limit
   should react immediately, without unwinding accumulated out-of-range input.
10. Check sub-degree FOV, different frame rates, pause/resume, HUD hidden, menu
    open, text input, and a resized viewport. Menus/text input must not leak wheel
    or toggle actions to the camera. Position acceleration must be unchanged.

The reference is a mathematical unit sphere centered on the focus point. Real
target shape, orientation, deployed wings, destruction, and visible mesh area are
intentionally ignored. The sphere is never created or rendered. FOV limits and
smoothing still apply when the target's raw position jumps.

## Automated checks

```powershell
./test-auto-fov.ps1
./test-movement.ps1
```

These tests verify the production mathematics and state transitions, not Unity
rendering or real in-game input. The in-game checks above remain necessary.
