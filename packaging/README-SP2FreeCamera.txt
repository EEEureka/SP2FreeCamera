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
- W / S / A / D / E / Q: move the camera
- Keypad 5: toggle normal and fast movement speed
- Normal movement speed: 200 m/s; fast movement speed: 2000 m/s
- Backspace: focus on your current vehicle or avatar
- Main keyboard minus: focus on the target selected by the game
- Middle mouse button: focus on terrain, a part, or a moving object under the pointer
- Hold and drag the left mouse button: clear focus and look freely
- Mouse wheel: adjust field of view

Position movement:
- In the full menu, set Normal/Fast position acceleration (m/s²), then Apply values.
- Defaults: normal 80 m/s²; fast 800 m/s². Starts, braking and turns use the active mode.
- Switching modes preserves velocity and uses the new mode's acceleration or braking rate.
- Lower values give longer glides. Releasing movement input gradually brakes.
- Movement smoothing (default 0.08 s) softens the final transition.
- Acceleration 0 disables that mode's limit. Set its acceleration and smoothing to 0 for instant movement.
- Example slow shot: normal speed 20 m/s, normal acceleration 20 m/s², smoothing 0.15 s.
- Only position velocity is affected; rotation, focus and FOV remain independent.

Moving targets use immediate per-render-frame rotation. Mouse-wheel zoom remains
independent and uses the configured FOV smoothing time even while focused.

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
