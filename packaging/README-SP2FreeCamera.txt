SP2 Free Camera v{PLUGIN_VERSION}
================================

Supported environment: Windows x64 / SimplePlanes 2 / BepInEx 5 Mono

Installation:
1. Close SimplePlanes 2 completely.
2. Extract everything from this archive directly into the game directory—the
   directory that contains "SimplePlanes 2.exe".
3. Allow the BepInEx directory to be merged. This package does not contain or
   overwrite personal configuration.
4. Start the game and enter a flight scene.

Default controls:
- Insert: enter or exit free camera
- ScrollLock: open or close the plugin settings menu
- Main keyboard 0: toggle cinematic/general mode (default on, saved locally)
- W / S / A / D / E / Q: move the camera in cinematic mode; control the game in general mode
- Keypad 5: toggle normal and fast movement speed
- Normal movement speed: 200 m/s; fast movement speed: 2000 m/s
- Backspace: focus on your current vehicle or avatar
- Main keyboard minus: focus on the target selected by the game
- Main keyboard equals (=): toggle automatic FOV
- Middle mouse button: focus on terrain, a part, or a moving object under the pointer
- Hold and drag the left mouse button: clear focus and look freely
- Mouse wheel: adjust field of view, or desired reference area while automatic FOV is active

Camera entry and mouse input:
- Entering free camera immediately clears roll while retaining position, heading, pitch and FOV.
- Target boxes do not interrupt left-mouse dragging or block wheel zoom/reference-area adjustment.
- Dragging may start over a target box and continue across its boundary; middle-click picking also works.
- Plugin menus, foreground game controls, dialogs, text fields and the console retain input protection.

Cinematic/general mode:
- Main keyboard 0 (not Numpad 0) toggles the mode while free camera is active.
- Cinematic mode starts enabled by default. Your choice is saved across game restarts.
- On: movement keys and quick-menu buttons move the camera using the existing speed and acceleration.
- Off (general mode): stop position movement immediately and return movement keys to the game, including Q/E yaw.
- Quick-menu movement and speed buttons are disabled in general mode.
- Both menus show the mode and provide a toggle. Mouse look, target focus and manual/automatic FOV remain available.
- Switching modes does not reset focus, interrupt a drag or reset FOV smoothing.
- Menus, dialogs, text/chat input, the console and application focus protect the toggle hotkey.
- Configuration: [Movement] CinematicModeEnabled = true; [Keys] ToggleCinematicMode = Alpha0.

Position movement (cinematic mode only):
- In the full menu, set Normal/Fast position acceleration (m/s²), then Apply values.
- Defaults: normal 80 m/s²; fast 800 m/s². Starts, braking and turns use the active mode.
- Switching normal/fast speed preserves velocity and uses the new speed setting's acceleration or braking rate.
- Switching cinematic movement off/on keeps the speed choice in the current session but clears position velocity.
- Lower values give longer glides. Releasing movement input gradually brakes.
- Movement smoothing (default 0.08 s) softens the final transition.
- Acceleration 0 disables that mode's limit. Set its acceleration and smoothing to 0 for instant movement.
- Example slow shot: normal speed 20 m/s, normal acceleration 20 m/s², smoothing 0.15 s.
- Only position velocity is affected; rotation, focus and FOV remain independent.

Moving targets use immediate per-render-frame rotation. Manual and automatic FOV
remain independent of rotation and use the configured FOV smoothing time.

Automatic FOV:
- Press main-keyboard = or use either plugin menu to toggle the mode.
- Any valid focus point is supported: terrain, parts, moving targets, yourself or a game target.
- Tracks the projected area of a virtual unit sphere, not the real target's size or shape.
- Initial framing is 100%; the wheel changes the desired reference area while active.
- Uses the existing FOV smoothing time. Nonzero smoothing allows temporary size drift.
- No valid target: waiting; the wheel adjusts FOV manually. Left-drag clears focus.
- Turning the mode off keeps the currently displayed FOV and restores manual zoom.
- New targets get a new framing baseline. FOV/near-distance limits are silent.
- The switch starts off on game launch and is not saved to configuration.
- Key configuration: [Keys] ToggleAutoFov = Equals (not KeypadPlus).

Uninstallation:
Delete BepInEx\plugins\SP2FreeCamera.dll. Do not remove BepInEx, winhttp.dll,
or doorstop_config.ini if other plugins still use BepInEx.

The configuration file is generated after the first run at:
BepInEx\config\local.sp2.freecamera.cfg

Privacy:
- The plugin has no telemetry, online update mechanism, or data-upload feature.
- This package does not contain developer or user configuration, logs, caches,
  account information, or local machine paths.
- Target names may be displayed in the local UI, but generic messages without
  target names are written to logs.

See THIRD_PARTY_NOTICES.txt and the licenses directory for third-party
components and licenses.
