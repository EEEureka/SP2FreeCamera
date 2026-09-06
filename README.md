[简体中文](doc/README.zh-CN.md)

# SP2 Free Camera

`SP2 Free Camera` is a local free-camera plugin for the Windows x64 edition of
`SimplePlanes 2`. The current version is `0.6.10`. It uses the BepInEx 5 Mono x64
runtime and is built for game version `0.7.6.100f`. The plugin only controls the
local camera and local UI: it does not modify vehicle physics, send network
messages, or depend on other custom plugins.

## Direct installation

The `release` directory contains a ready-to-install archive:

[`release/SP2FreeCamera-v0.6.10-win-x64.zip`](release/SP2FreeCamera-v0.6.10-win-x64.zip)

The archive includes both the Free Camera DLL and the complete BepInEx `5.4.23.5`
Windows x64 runtime, so BepInEx does not need to be installed separately.

1. Close `SimplePlanes 2` completely.
2. Extract everything from the archive directly into the game directory—the
   directory that contains `SimplePlanes 2.exe`.
3. If Windows asks whether to merge the `BepInEx` directory, allow it.
4. Start the game and enter a flight scene.

The important installed files are:

```text
SimplePlanes 2/
├─ .doorstop_version
├─ doorstop_config.ini
├─ winhttp.dll
├─ BepInEx/
│  ├─ core/
│  └─ plugins/
│     └─ SP2FreeCamera.dll
└─ SimplePlanes 2.exe
```

The release archive does not contain `BepInEx/config`, logs, caches, or other
plugins, so installing it over an existing setup will not reset Free Camera
settings. The archive checksum is recorded in
[`release/SHA256SUMS.txt`](release/SHA256SUMS.txt).

## Default controls

| Action | Default input |
| --- | --- |
| Enter or exit free camera | `Insert` |
| Open or close the standalone settings menu | `ScrollLock` |
| Toggle cinematic/general mode | `0` on the main keyboard number row |
| Move forward or backward | `W` / `S` |
| Move left or right | `A` / `D` |
| Move up or down | `E` / `Q` |
| Toggle normal and fast movement speed | `Keypad5` |
| Focus on your current vehicle or avatar | `Backspace` |
| Focus on the target selected by the game | `-` on the main keyboard number row |
| Toggle automatic FOV | `=` on the main keyboard number row |
| Focus on terrain, a part, or a moving object under the pointer | Middle mouse button |
| Clear focus and look freely | Hold and drag the left mouse button |
| Adjust FOV, or desired reference area while automatic FOV is active | Mouse wheel |

The default normal movement speed is `200 m/s`, and the default fast movement
speed is `2000 m/s`. When upgrading from an older version, the plugin only
migrates values that are still exactly equal to the old `20/200 m/s` defaults;
all other custom speed values are preserved.

The configuration file is generated after the first run:

```text
BepInEx/config/local.sp2.freecamera.cfg
```

The plugin includes English and Simplified Chinese interfaces. The language can
be changed from either the quick menu or the full settings menu.

## Camera entry and mouse input

Entering free camera immediately resets roll to zero, leveling the horizon while
retaining the current camera position, heading, pitch and FOV.

HUD target boxes do not interrupt left-mouse dragging or block wheel input. You
can start a drag over a target box and continue across its boundary. This applies
to both manual FOV and automatic-FOV reference-area adjustment; middle-click
focus selection uses the same boundary. Plugin menus, foreground game controls,
dialogs, text fields and the developer console still retain their input.

## Cinematic position movement

Press **0 on the main keyboard** (not Numpad 0) while free camera is active to
switch between **cinematic mode** and **general mode**. Cinematic mode is enabled
by default. The choice is saved locally as `[Movement] CinematicModeEnabled`
and retained when re-entering free camera or restarting the game.

- **Cinematic mode:** movement keys (`W/A/S/D/Q/E` by default) and quick-menu
  direction buttons move the camera with the configured speed and acceleration.
- **General mode:** the camera position stops immediately, with no residual
  glide. Movement keys return to the game's vehicle/avatar controls, including
  Q/E yaw with the default game bindings. Quick-menu movement and speed buttons
  are disabled.
- Mouse look, target focus, manual FOV and automatic FOV remain available in
  both modes. Switching modes does not reset the target, interrupt a drag or
  reset FOV smoothing.

Both plugin menus show the current mode and provide a toggle. The full menu can
also save your choice before entering free camera. The hotkey respects menus,
dialogs, text/chat input, the console and application focus. It is configurable
as `[Keys] ToggleCinematicMode = Alpha0` in the configuration file; no key-binding
editor is added to the settings menu.

Only cinematic mode captures the movement keys in the game's Craft/Character
keyboard maps. General mode restores their exact prior enabled states; the
plugin's mode keys (`0` and `=` by default) remain captured until free camera
exits. Other map categories, modified-key bindings, mouse and controller inputs
are unchanged. See the [mode test checklist](doc/CINEMATIC_MODE_TESTING.md).

### Speed and acceleration

Open the full settings menu with `ScrollLock` (or the settings button in the
quick menu). Under **Numeric settings**, edit **Normal position acceleration
(m/s²)** and **Fast position acceleration (m/s²)** independently, then click
**Apply values**. Defaults are `80 m/s²` for normal mode and `800 m/s²` for fast
mode. Both support `0–1000000` and are saved as `[Movement] NormalAcceleration`
and `FastAcceleration`.

The active speed setting's acceleration limits how quickly the camera's position
velocity can change when starting, releasing movement input or changing direction.
Switching normal/fast speed preserves the current velocity and uses the new rate,
including braking when switching from fast to normal. Keyboard and quick-menu
movement buttons use the same behavior. Velocity is retained in world space, so changing the view
direction while moving produces a gradual turn in the travel path. Acquiring a
focus target also preserves an ongoing move.

**Movement smoothing time** remains a shared setting (default `0.08 s`). It
eases the final approach to the requested speed or to a stop. With smoothing set
to `0`, position movement still accelerates and brakes at the configured rate.
Setting a mode's acceleration to `0` disables its limit and restores the previous
smoothing-only behavior in that mode; set its acceleration and movement smoothing
to `0` for instant movement.

Lower acceleration gives gentler, longer glides. As a starting point for a slow
shot, try a normal speed of `20 m/s`, normal acceleration of `20 m/s²`, and
movement smoothing of `0.15 s`. With the default speeds and accelerations, both
modes take at least `2.5 s` to accelerate from rest to their requested speed;
easing extends the final transition. Switching from fast to normal uses the
normal acceleration for deceleration, so it can take longer to slow down.
Releasing movement input gradually brakes the camera, so allow room for the
stopping distance.

These settings affect position only. Mouse-look rotation, immediate dynamic
target focus, and FOV zoom keep their independent settings. A focused camera may
finish braking from your movement input, but the target itself never pulls its
position. Switching to general mode, opening the full menu, losing input focus,
leaving free camera, or a frame interruption longer than `0.25 s` clears movement
immediately. Switching cinematic movement off/on retains the normal/fast speed
choice within the current free-camera session, but movement resumes from rest.

## Lock-on behavior

- Aircraft parts, released weapons, dynamic ground targets, your current
  vehicle or avatar, and the target selected by the game are resolved at their
  current raw positions every rendered frame, and the camera rotation is updated
  directly toward them.
- Dynamic lock-on does not use target prediction or focus smoothing and never
  moves the camera position.
- Manual and automatic FOV remain independent of focus rotation and always use
  the configured FOV smoothing time.
- The menu's terrain-point lock smoothing setting only affects static focus
  points such as terrain.
- Starting a left-button drag clears the active lock and returns to free look.

## Automatic FOV / reference-area lock

Press `=` on the main keyboard to toggle automatic FOV while free camera is
enabled. The full settings menu and quick menu also provide a toggle and show
**off**, **waiting for a target**, or **active**. Its configurable binding is
`[Keys] ToggleAutoFov = Equals`, not `KeypadPlus`.

With any valid focus target, the plugin compensates for distance changes using
the projected area of a **virtual unit sphere** centered on the focus point.
Terrain, parts, released weapons, dynamic ground targets, your vehicle/avatar,
and selected game targets all use the same rule. No sphere is created or rendered,
and the target's real dimensions, orientation, wing deployment and mesh shape
are intentionally ignored. The camera's position is never changed by this mode.

- Activation or a newly selected target captures the currently displayed FOV as
  the initial framing (`100%`), avoiding a jump to a previous zoom value.
- While active, the wheel changes desired reference **area** relative to that
  initial framing. The displayed percentage is not the real target's screen
  coverage. Wheel sensitivity uses the existing zoom sensitivity setting.
- Ideal FOV is calculated once per rendered frame using the same fresh target
  position as focus rotation. Actual FOV uses **FOV smoothing time** (default
  `0.12 s`), exactly like manual zoom. Nonzero smoothing allows temporary framing
  drift during fast distance changes; `0` applies the geometric result directly.
- Without a valid target, the mode waits and the wheel adjusts FOV normally.
  Left-mouse dragging clears focus and leaves the switch armed.
- Disabling the mode keeps the currently displayed FOV and returns the wheel to
  manual zoom. Lost targets stop stale auto-zoom; a new lock captures a new baseline.
- FOV limits and distances on/inside the reference sphere are handled silently.
  Distance-only clamping retains the desired ratio for recovery, and reverse
  scrolling at a limit responds without accumulated out-of-range wheel input.

The switch starts off when the plugin loads. It is retained across free-camera
sessions until the game is closed, but is not saved to configuration. A practical
[test checklist](doc/testing/auto-fov.md) covers the controls and boundary cases.

## Uninstallation

To remove only Free Camera, delete:

```text
BepInEx/plugins/SP2FreeCamera.dll
```

Do not remove `BepInEx`, `winhttp.dll`, or `doorstop_config.ini` if other plugins
still use BepInEx.

## Privacy and release scope

- The plugin has no telemetry, online update mechanism, or data-upload feature.
- The repository and release archive do not include personal configuration,
  runtime logs, caches, account details, access tokens, private-network
  addresses, or developer-machine absolute paths.
- Target names are displayed only in the local UI. Generic messages without
  target names are written to the BepInEx log.
- Logs produced by the game or other plugins may contain unrelated information;
  review `BepInEx/LogOutput.log` before sharing it.
- BepInEx in the release archive comes from the official `5.4.23.5` Windows x64
  release and is verified against a pinned SHA-256 checksum. It is never copied
  from the local game installation.

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for third-party components
and licenses. This repository currently does not declare an open-source license
for the Free Camera source code; the bundled third-party licenses do not grant
redistribution rights for that source code.

## Building from source

Requirements:

- Windows PowerShell 5.1 or PowerShell 7
- Visual Studio Build Tools with the Roslyn C# compiler
- A local installation of `SimplePlanes 2`

Set the game directory and build:

```powershell
$env:SP2_GAME_DIR = '<SteamLibrary>\steamapps\common\SimplePlanes 2'
.\build.ps1
```

If the compiler cannot be found automatically, provide a generic compiler path:

```powershell
.\build.ps1 -Csc '<VisualStudio>\MSBuild\Current\Bin\Roslyn\csc.exe'
```

Build the DLL and create the complete ready-to-install archive:

```powershell
.\build.ps1 -Package
```

Build and deploy the DLL to the local game installation. The script refuses to
overwrite the DLL while the game is running:

```powershell
.\build.ps1 -Deploy
```

The DLL is written to `bin/SP2FreeCamera.dll`, and the archive is written to
`release`. The packaging script downloads only the official BepInEx `5.4.23.5`
archive and verifies this pinned SHA-256 checksum:

```text
82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4
```

`verify-release.ps1` checks the archive allowlist, every bundled BepInEx file,
the plugin DLL version, and the absence of local user paths, repository paths,
UNC paths, and private-network addresses.

Run the camera-input, automatic-FOV and position-movement regression tests without
starting the game:

```powershell
.\test-camera-input.ps1
.\test-auto-fov.ps1
.\test-movement.ps1
```

Camera-input tests exercise production entry, pointer routing, movement-mode
methods and the keyboard-capture class with minimal Unity/UI/Rewired doubles.
They cover roll reset, target-box crossings, wheel input, UI protection, mode
defaults and keyboard gates, immediate stopping, exact binding restoration and
control-map lifecycle events. The configuration double models saved values but
does not verify disk I/O. These tests do not replace in-game rendering and
interaction checks.

The FOV tests cover unit-sphere projection, wheel ratios, state changes, silent
limits, near-distance safety, viewport aspect changes, and shared zoom smoothing.
Movement tests cover acceleration, braking distance, turns, reversals, speed
switches, smoothing combinations, and travel at different rendered-frame rates.

## Current limitations

- Only non-VR flight scenes are currently supported.
- The minimum field of view is `0.1°`; a custom projection matrix is used for
  extremely small values.
- If a target's original transform is updated only on physics ticks, aiming on
  every rendered frame can only use the latest raw position available to that
  frame.
- Rebuild and perform an in-flight regression test after the game is updated.
