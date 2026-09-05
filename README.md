[简体中文](doc/README.zh-CN.md)

# SP2 Free Camera

`SP2 Free Camera` is a local free-camera plugin for the Windows x64 edition of
`SimplePlanes 2`. The current version is `0.6.7`. It uses the BepInEx 5 Mono x64
runtime and is built for game version `0.7.6.100f`. The plugin only controls the
local camera and local UI: it does not modify vehicle physics, send network
messages, or depend on other custom plugins.

## Direct installation

The `release` directory contains a ready-to-install archive:

[`release/SP2FreeCamera-v0.6.7-win-x64.zip`](release/SP2FreeCamera-v0.6.7-win-x64.zip)

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
| Move forward or backward | `W` / `S` |
| Move left or right | `A` / `D` |
| Move up or down | `E` / `Q` |
| Toggle normal and fast movement speed | `Keypad5` |
| Focus on your current vehicle or avatar | `Backspace` |
| Focus on the target selected by the game | `-` on the main keyboard number row |
| Focus on terrain, a part, or a moving object under the pointer | Middle mouse button |
| Clear focus and look freely | Hold and drag the left mouse button |
| Adjust field of view | Mouse wheel |

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

## Cinematic position movement

Open the full settings menu with `ScrollLock` (or the settings button in the
quick menu). Under **Numeric settings**, edit **Normal position acceleration
(m/s²)** and **Fast position acceleration (m/s²)** independently, then click
**Apply values**. Defaults are `80 m/s²` for normal mode and `800 m/s²` for fast
mode. Both support `0–1000000` and are saved as `[Movement] NormalAcceleration`
and `FastAcceleration`.

The active mode's acceleration limits how quickly the camera's position velocity
can change when starting, releasing movement input or changing direction. A mode
switch preserves the current velocity and immediately uses the new mode's rate,
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
position. Opening the full menu, losing input focus, leaving free camera, or a
frame interruption longer than `0.25 s` still clears movement immediately.

## Lock-on behavior

- Aircraft parts, released weapons, dynamic ground targets, your current
  vehicle or avatar, and the target selected by the game are resolved at their
  current raw positions every rendered frame, and the camera rotation is updated
  directly toward them.
- Dynamic lock-on does not use target prediction or focus smoothing and never
  moves the camera position.
- Mouse-wheel zoom remains independent of target focus and always uses the
  configured FOV smoothing time.
- The menu's terrain-point lock smoothing setting only affects static focus
  points such as terrain.
- Starting a left-button drag clears the active lock and returns to free look.

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

Run the position-movement regression tests without starting the game:

```powershell
.\test-movement.ps1
```

They cover acceleration, braking distance, turns, reversals, speed switches,
smoothing combinations, and matching travel at different rendered-frame rates.

## Current limitations

- Only non-VR flight scenes are currently supported.
- The minimum field of view is `0.1°`; a custom projection matrix is used for
  extremely small values.
- If a target's original transform is updated only on physics ticks, aiming on
  every rendered frame can only use the latest raw position available to that
  frame.
- Rebuild and perform an in-flight regression test after the game is updated.
