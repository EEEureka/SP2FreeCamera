# [Plugin Release][Windows] SP2 Free Camera v0.6.5 — Free Camera and Per-Frame Target Lock

![SP2 Free Camera demonstration](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/01-cinematic-overview.png)

I made a local free-camera plugin for **SimplePlanes 2** called **SP2 Free Camera**.

It is designed for observation, target tracking, screenshots, and video capture. In addition to six-axis movement, mouse look, and FOV control, it can focus on your own vehicle, the target selected by the game, aircraft parts, released weapons, and supported moving ground targets.

Version **v0.6.5** improves moving-target focus. On every rendered frame, the camera points directly at the latest raw target position available to that frame. Focusing changes the camera's rotation only; it does not make the camera position follow the target.

**Public source repository:** [github.com/EEEureka/SP2FreeCamera](https://github.com/EEEureka/SP2FreeCamera)

## Moving-Target Focus

This HUD-free clip shows the camera keeping a fast-moving aircraft in view through a dive and turn. The focus system continuously updates the view direction without automatically translating the camera.

![Per-render-frame moving-target focus](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/02-dynamic-lock.gif)

For aircraft parts, released missiles or bombs, supported moving ground targets, your own vehicle or avatar, and the target selected by the game, the plugin:

- reads the target's latest position on every rendered frame;
- points the camera directly at that position;
- does not predict the target's future position;
- does not apply the menu's focus-smoothing value;
- keeps the camera position still when there is no movement input;
- lets you move the camera manually while continuing to face the target.

The menu's **Terrain focus smoothing time** setting applies only to static focus points such as terrain. Starting a left-mouse drag clears the current focus and returns to free look.

## Close Pass from a Fixed Camera Position

In this clip, the target approaches the observation point and grows visibly larger while remaining near the center of the frame. This makes the rotation-only focus behavior easier to see: the camera does not travel alongside the target.

![Target approaching a fixed camera position](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/03-close-follow.gif)

## Free Framing and Video Capture

With the game HUD hidden, the free camera can be positioned for cleaner screenshots and cinematic footage.

![Cinematic free-camera capture](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/04-cinematic-capture.gif)

![Aircraft close-up over the ocean](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/05-aircraft-closeup.png)

![Top-down view above the airport](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/06-topdown-follow.png)

## Focus Controls

![In-game notification after focusing on a released missile](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/07-focus-notification.png)

- `Backspace`: focus on your current vehicle, or your avatar after leaving the vehicle;
- `-` on the main keyboard number row: focus on the target currently selected by the game's targeting system;
- middle mouse button: focus on terrain, an aircraft part, a supported moving ground target, or a released missile/bomb under the pointer;
- left-mouse drag: clear the current focus and look freely.

## Default Controls

| Action | Default input |
| --- | --- |
| Enter or exit free camera | `Insert` |
| Open or close the full settings menu | `ScrollLock` |
| Move forward or backward | `W` / `S` |
| Move left or right | `A` / `D` |
| Move up or down | `E` / `Q` |
| Toggle normal and fast movement speed | `Keypad5` |
| Focus on your own vehicle or avatar | `Backspace` |
| Focus on the target selected by the game | `-` on the main keyboard number row |
| Focus on an object under the pointer | Middle mouse button |
| Clear focus and look freely | Hold and drag the left mouse button |
| Adjust field of view | Mouse wheel |

The default movement speeds are:

- normal mode: `200 m/s`;
- fast mode: `2000 m/s`.

While free camera is active, unmodified `W / A / S / D / Q / E` input is reserved for camera movement and temporarily blocked from the vehicle and avatar keyboard mappings. This prevents the camera and vehicle from responding at the same time. Mouse, controller, and right-mouse vehicle controls are not affected by this rule.

The plugin includes English and Simplified Chinese interfaces. Movement speed, movement smoothing, look sensitivity, FOV, terrain-focus smoothing, UI settings, and key bindings can be changed from the settings menu.

## Download

- **Ready-to-install package:** [SP2FreeCamera-v0.6.5-win-x64.zip](https://github.com/EEEureka/SP2FreeCamera/releases/download/v0.6.5/SP2FreeCamera-v0.6.5-win-x64.zip)
- **Release page:** [SP2 Free Camera v0.6.5](https://github.com/EEEureka/SP2FreeCamera/releases/tag/v0.6.5)
- **Public source repository:** [EEEureka/SP2FreeCamera](https://github.com/EEEureka/SP2FreeCamera)
- **English documentation:** [README.md](https://github.com/EEEureka/SP2FreeCamera/blob/main/README.md)
- **Simplified Chinese documentation:** [README.zh-CN.md](https://github.com/EEEureka/SP2FreeCamera/blob/main/doc/README.zh-CN.md)
- **Checksum file:** [SHA256SUMS.txt](https://github.com/EEEureka/SP2FreeCamera/releases/download/v0.6.5/SHA256SUMS.txt)

SHA-256 of the ready-to-install package:

```text
750333d5ba93ca5eb104215479914d1cb00027d71390b1607026599a4a7cb878
```

## Deployment / Installation

The package already contains **BepInEx 5.4.23.5 for Windows x64**, so a separate BepInEx installation is not required.

1. Close `SimplePlanes 2` completely.
2. Download `SP2FreeCamera-v0.6.5-win-x64.zip` from the link above.
3. Locate the game directory containing `SimplePlanes 2.exe`.
4. Extract **all files and folders** from the archive directly into that game directory.
5. If Windows asks whether to merge the existing `BepInEx` folder, allow it.
6. Start the game and enter a flight scene.
7. Press `Insert` to enable the free camera.

The important deployed files should look like this:

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

If BepInEx is already installed, merge the folders rather than replacing the entire installation. The release archive does not contain personal configuration, logs, caches, or other plugins, so installing it over an existing setup will not reset Free Camera settings.

The configuration file is created after the first run:

```text
BepInEx/config/local.sp2.freecamera.cfg
```

When upgrading from an older version, the plugin migrates speed values only if they are still exactly equal to the old `20/200 m/s` defaults. Other custom speed values are preserved.

## Uninstallation

To remove only SP2 Free Camera, delete:

```text
BepInEx/plugins/SP2FreeCamera.dll
```

Do not remove the entire `BepInEx` folder, `winhttp.dll`, or `doorstop_config.ini` if other plugins still depend on BepInEx.

## Privacy and Networking

- The plugin controls only the local camera and local UI.
- It does not modify vehicle physics.
- It does not send game network messages.
- It has no telemetry, online updater, or data-upload feature.
- The release package does not include personal configuration, runtime logs, caches, account information, access tokens, or developer-machine paths.
- Target names are displayed only in the local UI; the plugin writes generic messages without target names to the BepInEx log.

Player names visible in the demonstration media come from the in-game UI. Before sharing your own `BepInEx/LogOutput.log`, you should still review content written by the game or other plugins.

## Current Limitations

- Only non-VR flight scenes on Windows x64 are currently supported.
- This release was built for game version `0.7.6.100f`; compatibility should be rechecked after game updates.
- The minimum FOV is `0.1°`.
- If the target's original Transform is updated only on physics ticks, aiming on every rendered frame can use only the latest raw position available to that frame.
- The source repository is public, but SP2 Free Camera does not currently declare an open-source license. Please contact the author before modifying or redistributing the source.

Please report problems through [GitHub Issues](https://github.com/EEEureka/SP2FreeCamera/issues), preferably with the game version, plugin version, and reproduction steps.
