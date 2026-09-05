# [Plugin Release][Windows] SP2 Free Camera — Cinematic Movement, Target Lock and Automatic FOV

I made a local free-camera plugin for **SimplePlanes 2** called **SP2 Free Camera**.

It is designed for observation, target tracking, screenshots, and video capture. In addition to six-axis movement, mouse look, and FOV control, it can focus on your own vehicle, the target selected by the game, aircraft parts, released weapons, and supported moving ground targets.

![SP2 Free Camera](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/01-cinematic-overview.png)

The plugin provides separate position acceleration settings for normal and fast movement. Starting, braking and changing travel direction follow the active mode's acceleration, giving you more control over smooth dolly moves and gliding shots. Camera rotation, dynamic target focus and FOV zoom remain independent.

**Public source repository:** [github.com/EEEureka/SP2FreeCamera](https://github.com/EEEureka/SP2FreeCamera)

## Cinematic Position Movement

Open the full settings menu with `ScrollLock`, or use the settings button in the quick menu. Under **Numeric settings**, edit the two independent inputs and click **Apply values**:

- Normal position acceleration: `80 m/s²` by default.
- Fast position acceleration: `800 m/s²` by default.

These settings change how quickly the camera's **position velocity** changes. They apply to starting, releasing the movement keys, turning, reversing, and switching movement modes. The quick-menu direction buttons use the same movement behavior.

Changing modes keeps the current velocity and applies the new mode's acceleration. Switching from fast to normal also uses the normal acceleration for braking, so slowing down from high speed can take longer. Acquiring a focus target preserves an ongoing move instead of stopping the camera suddenly.

The shared **Movement smoothing time** setting (default `0.08 s`) softens the final approach to the requested speed or a stop. Lower acceleration produces gentler, longer glides. For a slow shot, try normal speed `20 m/s`, normal acceleration `20 m/s²`, and movement smoothing `0.15 s`.

Setting one mode's acceleration to `0` disables its acceleration limit. Set that mode's acceleration and movement smoothing to `0` for instant movement. None of these position settings smooth the target's aiming direction or change the FOV smoothing setting.

## Moving-Target Focus

The plugin supports moving-target focus on every rendered frame, updating the view direction without automatically translating the camera.

![Per-render-frame moving-target focus](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/02-dynamic-lock.gif)

For aircraft parts, released missiles or bombs, supported moving ground targets, your own vehicle or avatar, and the target selected by the game, the plugin:

- reads the target's latest position on every rendered frame;
- points the camera directly at that position;
- does not predict the target's future position;
- does not apply the menu's terrain-focus smoothing value;
- keeps a settled camera position fixed; after releasing movement input, the camera finishes braking first;
- lets you move the camera manually while continuing to face the target.

The menu's **Terrain focus smoothing time** setting applies only to static focus points such as terrain. Starting a left-mouse drag clears the current focus and returns to free look.

## Smooth FOV Zoom While Focused

Manual and automatic zoom use the menu's **FOV smoothing time**, whether the camera is in free look, focused on terrain, or locked to a moving object. This is separate from target rotation: a non-zero FOV smoothing value gives zoom a gradual visual transition without making the aim trail behind the target.

Set **FOV smoothing time** to `0` if you prefer instant zoom. The default is `0.12 s`.

## Automatic FOV and Reference-Area Lock

The plugin supports automatic FOV compensation for changing target distance. Press `=` on the main keyboard, or use the toggle in either plugin menu. It works with every valid focus type, including terrain points, parts, your vehicle/avatar and selected game targets, without moving the camera.

The mode uses a virtual unit sphere at the focus point as its mathematical reference, independently of the real target's size or shape. Initial framing is `100%`; while active, the mouse wheel changes the desired reference **area**. This percentage is relative to the captured framing, not a measurement of the real target's screen coverage.

Automatic zoom uses the same **FOV smoothing time** as manual zoom. Nonzero smoothing allows temporary size drift during fast distance changes; set it to `0` for immediate compensation. FOV and near-distance limits are handled silently.

Without a valid target, the mode waits and wheel zoom stays manual. Left-mouse dragging clears focus but keeps the switch armed. Turning the mode off preserves the current FOV, and selecting a new target captures a fresh framing baseline. The switch starts off when the game launches.

## Fixed-Position Target Tracking

Once movement has stopped, the plugin keeps the camera's position fixed while rotating the view to follow the target.

![Target approaching a fixed camera position](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/03-close-follow.gif)

## Free Framing and Video Capture

The plugin supports free camera positioning for screenshots and cinematic video capture.

![Cinematic free-camera capture](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/04-cinematic-capture.gif)

![Aircraft close-up over the ocean](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/05-aircraft-closeup.png)

![Top-down view above the airport](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/06-topdown-follow.png)

## Focus Controls

- `Backspace`: focus on your current vehicle, or your avatar after leaving the vehicle;
- `-` on the main keyboard number row: focus on the target currently selected by the game's targeting system;
- middle mouse button: focus on terrain, an aircraft part, a supported moving ground target, or a released missile/bomb under the pointer;
- left-mouse drag: clear the current focus and look freely.

![Released-weapon focus](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/07-focus-notification.png)

## Default Controls

- Enter or exit free camera: `Insert`
- Open or close the full settings menu: `ScrollLock`
- Move forward or backward: `W` / `S`
- Move left or right: `A` / `D`
- Move up or down: `E` / `Q`
- Toggle normal and fast movement speed: `Keypad5`
- Focus on your own vehicle or avatar: `Backspace`
- Focus on the target selected by the game: `-` on the main keyboard number row
- Toggle automatic FOV: `=` on the main keyboard number row
- Focus on an object under the pointer: Middle mouse button
- Clear focus and look freely: Hold and drag the left mouse button
- Adjust FOV, or desired reference area while automatic FOV is active: Mouse wheel

The default movement speeds are:

- normal mode: `200 m/s`;
- fast mode: `2000 m/s`.

While free camera is active, movement keys (`W / A / S / D / Q / E` by default) and the automatic-FOV key (`=` by default) are temporarily blocked from the vehicle and avatar keyboard mappings. This prevents camera controls from also operating the vehicle. Mouse, controller, and right-mouse vehicle controls are not affected by this rule.

The plugin includes English and Simplified Chinese interfaces. Movement speed, normal/fast position acceleration, movement smoothing, look sensitivity, FOV, FOV smoothing, terrain-focus smoothing, UI settings, and key bindings can be changed from the settings menu.

## Download

- **Ready-to-install package:** [Download from the latest release](https://github.com/EEEureka/SP2FreeCamera/releases/latest) — choose the Windows x64 ZIP under **Assets**, not GitHub's automatically generated source-code archives.
- **Release notes:** [Latest release](https://github.com/EEEureka/SP2FreeCamera/releases/latest)
- **Public source repository:** [EEEureka/SP2FreeCamera](https://github.com/EEEureka/SP2FreeCamera)
- **English documentation:** [README.md](https://github.com/EEEureka/SP2FreeCamera/blob/main/README.md)
- **Simplified Chinese documentation:** [README.zh-CN.md](https://github.com/EEEureka/SP2FreeCamera/blob/main/doc/README.zh-CN.md)
- **Checksum file:** [Latest SHA256SUMS.txt](https://github.com/EEEureka/SP2FreeCamera/releases/latest/download/SHA256SUMS.txt)

These release links automatically follow the latest published release. To verify a download, compare its SHA-256 with the checksum file from the same release as the ZIP.

## Deployment / Installation

The package already contains **BepInEx for Windows x64**, so a separate BepInEx installation is not required.

1. Close `SimplePlanes 2` completely.
2. Download the ready-to-install Windows x64 ZIP from the [latest release](https://github.com/EEEureka/SP2FreeCamera/releases/latest).
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

If an older configuration has `FovSmoothingTime = 0`, choose a non-zero **FOV smoothing time** in the menu to enable gradual zoom. Moving-target rotation remains immediate and independent of that value.

Before upgrading, check the [latest release notes](https://github.com/EEEureka/SP2FreeCamera/releases/latest) for any configuration migration or compatibility guidance.

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

Before sharing your own `BepInEx/LogOutput.log`, review content written by the game or other plugins.

## Current Limitations

- Only non-VR flight scenes on Windows x64 are currently supported.
- Check the [latest release notes](https://github.com/EEEureka/SP2FreeCamera/releases/latest) and [current documentation](https://github.com/EEEureka/SP2FreeCamera/blob/main/README.md) for game-version compatibility; compatibility should be rechecked after game updates.
- The minimum FOV is `0.1°`.
- If the target's original Transform is updated only on physics ticks, aiming on every rendered frame can use only the latest raw position available to that frame.
- The source repository is public, but SP2 Free Camera does not currently declare an open-source license. Please contact the author before modifying or redistributing the source.

Please report problems through [GitHub Issues](https://github.com/EEEureka/SP2FreeCamera/issues), preferably with the game version, plugin version, and reproduction steps.
