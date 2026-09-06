using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace SP2FreeCamera
{
    internal static class Localization
    {
        internal const string EnglishCode = "English";
        internal const string SimplifiedChineseCode = "SimplifiedChinese";

        private static readonly Dictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "WindowTitle", "SP2 Free Camera" },
                { "Language", "Language" },
                { "InterfaceLanguage", "Interface language" },
                { "LanguageChanged", "Interface language changed." },
                { "Status", "Status" },
                { "Camera", "Camera: " },
                { "FreeCamera", "Free camera" },
                { "GameCamera", "Game camera" },
                { "Speed", "Speed: " },
                { "Fast", "Fast" },
                { "Normal", "Normal" },
                { "FocusTarget", "Focus target: " },
                { "Focus", "Focus: " },
                { "None", "None" },
                { "EnterFreeCamera", "Enter free camera" },
                { "ExitFreeCamera", "Exit free camera" },
                { "LockSelf", "Focus on self" },
                { "FocusSelectedTarget", "Focus selected game target" },
                { "ClearFocus", "Clear focus" },
                { "ClearLock", "Clear focus" },
                { "CinematicModeToggle", "Cinematic movement mode (remembered)" },
                { "EnableCinematicMode", "Enable cinematic movement" },
                { "DisableCinematicMode", "Use general mode (fixed position)" },
                { "CinematicModeOn", "Cinematic mode: camera movement enabled" },
                { "CinematicModeOff", "General mode: fixed camera position; movement keys control the game" },
                { "CinematicModeHelp", "Enabled by default and saved locally. On: movement keys and quick-menu buttons move the camera. Off: stop immediately and return movement keys to the game. Mouse look, target focus and FOV remain available in both modes. Default hotkey during free camera: main keyboard 0." },
                { "AutoFovToggle", "Automatic FOV (focused targets only)" },
                { "EnableAutoFov", "Enable automatic FOV" },
                { "DisableAutoFov", "Disable automatic FOV" },
                { "AutoFovOff", "Auto FOV: off" },
                { "AutoFovWaiting", "Auto FOV: waiting for a target" },
                { "AutoFovActiveFormat", "Auto FOV: active | Desired reference area: {0}%" },
                { "AutoFovEnabled", "Automatic FOV enabled; it takes effect while a target is focused." },
                { "AutoFovDisabled", "Automatic FOV disabled. Manual zoom keeps the current FOV." },
                { "AutoFovHelp", "Tracks a virtual unit sphere at the focus point, independent of real target size. The wheel changes its desired projected area relative to the initial framing (100%). Uses the same FOV smoothing time; temporary size drift is expected with smoothing. Without a target, the wheel adjusts FOV normally. Limits are silent." },
                { "HideGameUi", "Hide game UI" },
                { "ShowGameUi", "Show game UI" },
                { "CloseMenu", "Close this menu" },
                { "BehaviorSettings", "Behavior" },
                { "EnableFreeCamera", "Enable free camera" },
                { "AutoHideUi", "Automatically hide game UI when entering free camera" },
                { "InvertLookY", "Invert vertical look while dragging with the left mouse button" },
                { "ScaleLookWithFov", "Scale look sensitivity with the current rendered FOV" },
                { "ShowStatusMessages", "Show status messages while the game UI is visible" },
                { "ShowQuickMenu", "Show the draggable shortcut in flight scenes" },
                { "NumericSettings", "Numeric settings" },
                { "NormalSpeed", "Normal movement speed (m/s)" },
                { "FastSpeed", "Fast movement speed (m/s)" },
                { "NormalAcceleration", "Normal position acceleration (m/s²)" },
                { "FastAcceleration", "Fast position acceleration (m/s²)" },
                { "MovementAccelerationHelp", "The active mode controls starts, braking and direction changes. Switching modes preserves velocity. Lower values give longer glides; 0 disables that mode's limit. Movement smoothing softens the final transition." },
                { "MovementSmoothingTime", "Movement smoothing time (seconds)" },
                { "LookSensitivity", "Base left-drag sensitivity (at 60 deg FOV)" },
                { "LookSmoothingTime", "Left-drag look smoothing time (seconds)" },
                { "DragThreshold", "Left-drag threshold (pixels)" },
                { "FovScrollSensitivity", "Mouse-wheel zoom / framing sensitivity" },
                { "FovSmoothingTime", "FOV smoothing time (seconds, including while focused)" },
                { "MaximumFov", "Maximum FOV (degrees)" },
                { "FocusMaximumDistance", "Focus ray distance (m)" },
                { "FocusSmoothingTime", "Terrain focus smoothing time (seconds)" },
                { "NumericHelp", "For immediate position movement, set the active mode's acceleration and movement smoothing to 0. Look and FOV smoothing are independent. Minimum FOV is fixed at 0.1 deg." },
                { "ApplyValues", "Apply values" },
                { "ReloadValues", "Reload current configuration" },
                { "ReloadedValues", "Current configuration reloaded." },
                { "InvalidNumericValues", "Invalid numeric value. Check all inputs." },
                { "AppliedValues", "Numeric configuration applied." },
                { "CurrentBindings", "Current key bindings" },
                { "BindingsHelp", "Key bindings can be changed in BepInEx/config/local.sp2.freecamera.cfg." },
                { "BindingToggleCamera", "Enter/exit free camera" },
                { "BindingToggleMenu", "Open/close this menu" },
                { "BindingToggleCinematicMode", "Toggle cinematic/general mode" },
                { "BindingMoveForward", "Move forward" },
                { "BindingMoveBackward", "Move backward" },
                { "BindingMoveLeft", "Move left" },
                { "BindingMoveRight", "Move right" },
                { "BindingMoveUp", "Move up" },
                { "BindingMoveDown", "Move down" },
                { "BindingToggleSpeed", "Toggle normal/fast speed" },
                { "BindingLockSelf", "Focus on self" },
                { "BindingFocusSelectedTarget", "Focus selected game target" },
                { "BindingToggleAutoFov", "Toggle automatic FOV" },
                { "DuplicateKeyWarning", "Notice: {0} is assigned to multiple free-camera actions." },
                { "FixedMouseControls", "Fixed mouse controls" },
                { "MouseLeft", "Left mouse drag: smoothly look around and clear the current focus" },
                { "MouseMiddle", "Middle mouse click: focus on terrain, aircraft parts, or released missiles and bombs under the actual pointer" },
                { "MouseWheel", "Mouse wheel: smoothly adjust FOV; while auto FOV is active, adjust the desired reference area" },
                { "MouseRight", "Right mouse button: not bound to camera control (clicks are still blocked over plugin menus)" },
                { "HideUiHelp", "Hide/show UI: {0} (game Screenshot Mode); open free-camera settings: {1}" },
                { "HideUiShortcutHint", "Hide/show UI: {0}  |  Open settings: {1}" },
                { "Unbound", "Unbound" },
                { "InputHelp", "Cinematic mode owns movement keys (W/A/S/D/Q/E by default); general mode returns them to the game. While free camera is active, the mode keys (0 and = by default) remain suppressed in Craft and Character keyboard maps. Mouse, joystick, and right-mouse vehicle controls remain available." },
                { "CameraLauncher", "Camera" },
                { "CollapseCamera", "Close" },
                { "QuickTitle", "Free Camera Quick Controls" },
                { "SwitchNormalSpeed", "Use normal speed" },
                { "SwitchFastSpeed", "Use fast speed" },
                { "HoldButtonsToMove", "Hold a button to move the camera" },
                { "Forward", "Forward" },
                { "Backward", "Back" },
                { "Left", "Left" },
                { "Right", "Right" },
                { "Up", "Up" },
                { "Down", "Down" },
                { "FullSettings", "Full settings" },
                { "Collapse", "Collapse" },
                { "Ready", "Ready" },
                { "FreeCameraRunning", "Free camera active" },
                { "FreeCameraDisabled", "Free camera is disabled in the configuration." },
                { "SceneChangedExit", "The scene changed. Free camera exited safely." },
                { "GameCameraChangedExit", "The game switched to another camera. Free camera exited." },
                { "OnlyInFlight", "Free camera can only be enabled in a flight scene." },
                { "VrNotSupported", "Free camera is not supported in VR mode in this version." },
                { "FlightCameraNotReady", "The flight camera is not ready. Try again shortly." },
                { "FreeCameraEnabled", "Free camera enabled." },
                { "EnableFailed", "Could not enable free camera. The original camera was restored." },
                { "UiBusy", "The game UI is handling input, so free camera cannot be switched right now." },
                { "FreeCameraClosed", "Free camera closed." },
                { "SwitchedFast", "Switched to fast movement." },
                { "SwitchedNormal", "Switched to normal movement." },
                { "EnableFirst", "Enable free camera first." },
                { "LocalPlayerNotReady", "The local player is not ready." },
                { "NoSelectedGameTarget", "No game target is currently selected." },
                { "FirstPersonFocusLocked", "First-person target lock enabled. Look manually to release, or press Backspace to recenter." },
                { "FirstPersonFocusLost", "First-person target lost; native camera control restored." },
                { "FirstPersonPickMiss", "No terrain, aircraft part or moving ground target was hit." },
                { "ConfigFirstPersonFocus", "Enable middle-click picking, the selected-target key and Backspace recenter in native first-person cameras. Excludes targeting pods and VR; Look At Cockpit takes priority but allows native recenter. Independent of freecam movement and roll." },
                { "NoControllableUi", "No controllable flight UI is currently available." },
                { "GameUiShown", "Game UI shown." },
                { "TargetUnavailable", "The focus target is currently unavailable." },
                { "FocusCleared", "Focus cleared." },
                { "SelectionOutsideViewport", "The pointer is outside the camera view, so no target can be selected." },
                { "RayMiss", "No focusable terrain, aircraft part, dynamic ground target, or released weapon was found under the pointer." },
                { "LockedFormat", "Focused on {0}." },
                { "TargetLostFormat", "Focus target lost: {0}" },
                { "FocusLockedLog", "A focus target was selected." },
                { "FocusTargetLostLog", "The current focus target was lost." },
                { "GameUiHiddenFormat", "Game UI hidden. Press {0} to reopen the free-camera menu." },
                { "GameUiHiddenRestoreFormat", "Game UI hidden. Press {0} to restore it, or {1} to open free-camera settings." },
                { "TerrainPoint", "terrain point" },
                { "AircraftPart", "aircraft part" },
                { "Missile", "missile" },
                { "Bomb", "bomb" },
                { "Self", "self" },
                { "OwnAircraft", "your aircraft" },
                { "OwnPlayer", "your player" },
                { "GameTarget", "game target" },
                { "LaserTarget", "laser target" },
                { "PartFormat", "part: {0}" },
                { "MissileFormat", "missile: {0}" },
                { "BombFormat", "bomb: {0}" },
                { "ConfigLanguage", "Interface language. Supported values: English and SimplifiedChinese." },
                { "ConfigEnabled", "Whether free camera can be enabled. The ScrollLock menu can always be opened." },
                { "ConfigCinematicMode", "Enable camera position movement (default true; saved across sessions). False selects general mode: position stops immediately and movement keys return to the game. Mouse look, focus and FOV are unaffected." },
                { "ConfigNormalSpeed", "Normal movement speed in metres per second. Default: 200." },
                { "ConfigFastSpeed", "Fast movement speed in metres per second. Default: 2000." },
                { "ConfigNormalAcceleration", "Maximum change of camera position velocity while normal mode is active, in metres per second squared (0 to 1000000; default 80). Applies to starts, braking, turns and switching into normal mode. Set to 0 to disable this mode's acceleration limit. Does not affect rotation, focus or FOV." },
                { "ConfigFastAcceleration", "Maximum change of camera position velocity while fast mode is active, in metres per second squared (0 to 1000000; default 800). Applies to starts, braking, turns and switching into fast mode. Set to 0 to disable this mode's acceleration limit. Does not affect rotation, focus or FOV." },
                { "ConfigMovementSmoothing", "Shared easing time for camera position velocity near its target. The active mode's acceleration still limits starts, braking and turns when this is 0. Set that mode's acceleration and movement smoothing to 0 for immediate movement." },
                { "ConfigLookSensitivity", "Base rotation angle per pixel while dragging with the left mouse button at 60-degree FOV." },
                { "ConfigLookSmoothing", "Smoothing time while left-drag look follows its target angle. Set to 0 for immediate rotation." },
                { "ConfigInvertLookY", "Invert the vertical mouse-look direction." },
                { "ConfigScaleLookWithFov", "Scale mouse-look sensitivity using the perspective ratio of the current rendered FOV." },
                { "ConfigDragThreshold", "Start rotating and clear the current focus after the left mouse button moves beyond this pixel distance." },
                { "ConfigFovSensitivity", "Sensitivity of mouse-wheel FOV adjustment, or reference-area adjustment while automatic FOV is active." },
                { "ConfigFovSmoothing", "Shared smoothing time for manual and automatic FOV, including while any target is focused. Independent of focus rotation smoothing. Set to 0 for immediate zoom changes; non-zero values allow temporary framing drift while automatic FOV catches up." },
                { "ConfigMaximumFov", "Maximum FOV allowed by free camera. Minimum FOV is fixed at 0.1 degrees." },
                { "ConfigFocusDistance", "Maximum distance in metres for middle-click focus selection." },
                { "ConfigFocusSmoothing", "Rotation smoothing time for terrain-point focus. Moving targets always align to their current raw position every rendered frame and ignore this setting. This does not affect FOV zoom smoothing." },
                { "ConfigAutoHideUi", "Automatically hide the game UI when entering free camera." },
                { "ConfigShowStatus", "Show free-camera status messages while the game UI is visible." },
                { "ConfigShowQuickMenu", "Show the draggable free-camera shortcut in flight scenes." },
                { "ConfigQuickX", "Horizontal shortcut position within the movable screen area, from 0 to 1. Normally saved automatically after dragging." },
                { "ConfigQuickY", "Vertical shortcut position within the movable screen area, from 0 to 1. Normally saved automatically after dragging." },
                { "ConfigToggleCamera", "Enter or exit free camera." },
                { "ConfigToggleMenu", "Show or hide the standalone free-camera menu." },
                { "ConfigToggleCinematicMode", "Toggle cinematic movement mode while free camera is active and no menu or text input owns the keyboard. Default: the main keyboard 0 (Alpha0), not Keypad0. The selected mode is saved locally." },
                { "ConfigMoveForward", "Move the free camera forward." },
                { "ConfigMoveBackward", "Move the free camera backward." },
                { "ConfigMoveLeft", "Move the free camera left." },
                { "ConfigMoveRight", "Move the free camera right." },
                { "ConfigMoveUp", "Move the free camera up." },
                { "ConfigMoveDown", "Move the free camera down." },
                { "ConfigToggleSpeed", "Toggle normal and fast movement speed." },
                { "ConfigLockSelf", "Focus the free camera on the local player's current aircraft or avatar." },
                { "ConfigFocusSelectedTarget", "Focus the free camera on the game target currently selected by the local player's targeting system." },
                { "ConfigToggleAutoFov", "Toggle automatic FOV. Default: the main keyboard Equals key, not KeypadPlus. The mode waits when there is no valid focus target; the wheel adjusts desired reference area while active." },
                { "ConfigKeyRevision", "Internal key-binding migration revision. Do not edit manually." },
                { "ConfigMovementSpeedRevision", "Internal movement-speed migration revision. Do not edit manually." }
            };

        private static readonly Dictionary<string, string> SimplifiedChinese =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "WindowTitle", "SP2 自由相机" },
                { "Language", "语言" },
                { "InterfaceLanguage", "界面语言" },
                { "LanguageChanged", "界面语言已切换。" },
                { "Status", "状态" },
                { "Camera", "相机：" },
                { "FreeCamera", "自由相机" },
                { "GameCamera", "游戏相机" },
                { "Speed", "速度：" },
                { "Fast", "快速" },
                { "Normal", "普通" },
                { "FocusTarget", "锁定目标：" },
                { "Focus", "锁定：" },
                { "None", "无" },
                { "EnterFreeCamera", "进入自由相机" },
                { "ExitFreeCamera", "退出自由相机" },
                { "LockSelf", "锁定自己" },
                { "FocusSelectedTarget", "锁定当前游戏目标" },
                { "ClearFocus", "解除目标锁定" },
                { "ClearLock", "解除锁定" },
                { "CinematicModeToggle", "运镜模式（记住开关状态）" },
                { "EnableCinematicMode", "开启运镜模式" },
                { "DisableCinematicMode", "切换一般模式（固定位置）" },
                { "CinematicModeOn", "运镜模式：允许移动相机位置" },
                { "CinematicModeOff", "一般模式：相机位置固定，移动键交还游戏" },
                { "CinematicModeHelp", "默认开启，状态长期保存在本地。开启时移动键和快捷按钮控制相机位移；关闭后立即停止位移，移动键交还游戏。两种模式均保留鼠标观察、目标锁定与 FOV 功能。默认在自由相机中使用主键盘 0 切换。" },
                { "AutoFovToggle", "自动 FOV（仅锁定有效目标时生效）" },
                { "EnableAutoFov", "开启自动 FOV" },
                { "DisableAutoFov", "关闭自动 FOV" },
                { "AutoFovOff", "自动 FOV：关闭" },
                { "AutoFovWaiting", "自动 FOV：等待目标" },
                { "AutoFovActiveFormat", "自动 FOV：生效中 | 期望参考面积：{0}%" },
                { "AutoFovEnabled", "已开启自动 FOV，锁定有效目标时生效。" },
                { "AutoFovDisabled", "已关闭自动 FOV，保留当前 FOV 并恢复手动缩放。" },
                { "AutoFovHelp", "以锁定点处的虚拟单位球为参考，与真实目标尺寸无关。滚轮调整相对于初始构图（100%）的期望投影面积。复用 FOV 平滑时间，平滑期间允许比例暂时偏离。没有目标时滚轮正常调整 FOV；到达限制时不提示。" },
                { "HideGameUi", "隐藏游戏 UI" },
                { "ShowGameUi", "显示游戏 UI" },
                { "CloseMenu", "关闭此菜单" },
                { "BehaviorSettings", "行为设置" },
                { "EnableFreeCamera", "允许启用自由相机" },
                { "AutoHideUi", "进入自由相机时自动隐藏游戏 UI" },
                { "InvertLookY", "反转左键拖拽的垂直观察方向" },
                { "ScaleLookWithFov", "按当前实际 FOV 的透视比例缩放观察灵敏度" },
                { "ShowStatusMessages", "游戏 UI 可见时显示状态提示" },
                { "ShowQuickMenu", "在飞行场景显示可拖动快捷入口" },
                { "NumericSettings", "数值设置" },
                { "NormalSpeed", "普通移动速度 (m/s)" },
                { "FastSpeed", "快速移动速度 (m/s)" },
                { "NormalAcceleration", "普通模式位置加速度 (m/s²)" },
                { "FastAcceleration", "快速模式位置加速度 (m/s²)" },
                { "MovementAccelerationHelp", "按当前模式控制起步、刹停和移动方向变化，切换模式时保留当前速度。数值越小滑行越长；0 表示关闭该模式的加速度限制。移动平滑时间用于柔化收尾。" },
                { "MovementSmoothingTime", "位置移动平滑时间 (秒)" },
                { "LookSensitivity", "左键观察基准灵敏度 (60° FOV)" },
                { "LookSmoothingTime", "左键观察平滑时间 (秒)" },
                { "DragThreshold", "左键拖拽阈值 (像素)" },
                { "FovScrollSensitivity", "滚轮缩放 / 构图灵敏度" },
                { "FovSmoothingTime", "FOV 平滑时间 (秒，锁定目标时仍生效)" },
                { "MaximumFov", "最大 FOV (度)" },
                { "FocusMaximumDistance", "目标射线距离 (m)" },
                { "FocusSmoothingTime", "地形点锁定平滑时间 (秒)" },
                { "NumericHelp", "当前模式的位置加速度与移动平滑时间同时设为 0 时即时移动。朝向与 FOV 平滑相互独立。最小 FOV 固定为 0.1°。" },
                { "ApplyValues", "应用数值" },
                { "ReloadValues", "从当前配置重新读取" },
                { "ReloadedValues", "已重新读取当前配置。" },
                { "InvalidNumericValues", "数值格式无效，请检查输入。" },
                { "AppliedValues", "数值配置已应用。" },
                { "CurrentBindings", "当前键位" },
                { "BindingsHelp", "键位可在 BepInEx/config/local.sp2.freecamera.cfg 中修改。" },
                { "BindingToggleCamera", "进入/退出自由相机" },
                { "BindingToggleMenu", "打开/关闭此菜单" },
                { "BindingToggleCinematicMode", "运镜/一般模式切换" },
                { "BindingMoveForward", "向前移动" },
                { "BindingMoveBackward", "向后移动" },
                { "BindingMoveLeft", "向左移动" },
                { "BindingMoveRight", "向右移动" },
                { "BindingMoveUp", "向上移动" },
                { "BindingMoveDown", "向下移动" },
                { "BindingToggleSpeed", "普通/快速速度切换" },
                { "BindingLockSelf", "锁定自己" },
                { "BindingFocusSelectedTarget", "锁定当前游戏目标" },
                { "BindingToggleAutoFov", "自动 FOV 模式切换" },
                { "DuplicateKeyWarning", "提示：键位 {0} 被多个自由相机功能共用。" },
                { "FixedMouseControls", "固定鼠标操作" },
                { "MouseLeft", "鼠标左键拖拽：平滑自由观察并解除当前目标锁定" },
                { "MouseMiddle", "鼠标中键点按：从真实鼠标位置锁定地形、飞机部件或已离架的导弹与炸弹" },
                { "MouseWheel", "鼠标滚轮：平滑调整 FOV；自动 FOV 生效时改为调整期望参考面积" },
                { "MouseRight", "鼠标右键：相机操作不绑定（插件菜单区域仍会拦截点击）" },
                { "HideUiHelp", "隐藏/显示 UI：{0}（游戏原生 Screenshot Mode）；打开自由相机设置：{1}" },
                { "HideUiShortcutHint", "隐藏/显示 UI：{0}  |  打开设置：{1}" },
                { "Unbound", "未绑定" },
                { "InputHelp", "运镜模式接管移动键（默认 W/A/S/D/Q/E），一般模式将它们交还游戏。自由相机启用期间，模式键（默认 0 和 =）仍在载具和人物键盘映射中临时禁用；鼠标、手柄及右键载具操控不受影响。" },
                { "CameraLauncher", "相机" },
                { "CollapseCamera", "收起相机" },
                { "QuickTitle", "自由相机快捷控制" },
                { "SwitchNormalSpeed", "切换普通速度" },
                { "SwitchFastSpeed", "切换快速速度" },
                { "HoldButtonsToMove", "按住按钮移动相机" },
                { "Forward", "前" },
                { "Backward", "后" },
                { "Left", "左" },
                { "Right", "右" },
                { "Up", "上" },
                { "Down", "下" },
                { "FullSettings", "完整设置" },
                { "Collapse", "收起" },
                { "Ready", "就绪" },
                { "FreeCameraRunning", "自由相机运行中" },
                { "FreeCameraDisabled", "自由相机已在配置中禁用。" },
                { "SceneChangedExit", "场景已切换，自由相机已安全退出。" },
                { "GameCameraChangedExit", "游戏已切换到其他相机，自由相机已退出。" },
                { "OnlyInFlight", "自由相机只能在飞行场景中启用。" },
                { "VrNotSupported", "当前版本暂不支持在 VR 模式启用自由相机。" },
                { "FlightCameraNotReady", "飞行相机尚未初始化，请稍后重试。" },
                { "FreeCameraEnabled", "自由相机已启用。" },
                { "EnableFailed", "自由相机启用失败，已恢复原相机。" },
                { "UiBusy", "当前游戏界面正在处理输入，暂时不能切换自由相机。" },
                { "FreeCameraClosed", "自由相机已关闭。" },
                { "SwitchedFast", "已切换为快速移动。" },
                { "SwitchedNormal", "已切换为普通移动。" },
                { "EnableFirst", "请先启用自由相机。" },
                { "LocalPlayerNotReady", "当前本地玩家对象尚未就绪。" },
                { "NoSelectedGameTarget", "当前没有选中的游戏目标。" },
                { "FirstPersonFocusLocked", "第一人称目标锁定已启用；手动观察可解除，退格可回正。" },
                { "FirstPersonFocusLost", "第一人称锁定目标已丢失，已恢复原生相机控制。" },
                { "FirstPersonPickMiss", "未命中可锁定的地形、飞机部件或动态地面目标。" },
                { "ConfigFirstPersonFocus", "在原生第一人称相机中启用中键拾取、当前目标锁定键和退格回正。武器吊舱和 VR 不生效；看向座舱优先，但允许原生回正。与自由相机的移动和滚转设置独立。" },
                { "NoControllableUi", "当前没有可控制的飞行 UI。" },
                { "GameUiShown", "游戏 UI 已显示。" },
                { "TargetUnavailable", "目标当前不可用。" },
                { "FocusCleared", "已解除目标锁定。" },
                { "SelectionOutsideViewport", "鼠标位置不在相机画面内，无法选择目标。" },
                { "RayMiss", "鼠标方向未命中可锁定的地形、飞机部件、动态地面目标或离架武器。" },
                { "LockedFormat", "已锁定 {0}。" },
                { "TargetLostFormat", "目标已丢失: {0}" },
                { "FocusLockedLog", "已选择锁定目标。" },
                { "FocusTargetLostLog", "当前锁定目标已丢失。" },
                { "GameUiHiddenFormat", "游戏 UI 已隐藏；按 {0} 可重新打开自由相机菜单。" },
                { "GameUiHiddenRestoreFormat", "游戏 UI 已隐藏；按 {0} 恢复，或按 {1} 打开自由相机设置。" },
                { "TerrainPoint", "地形点" },
                { "AircraftPart", "飞机部件" },
                { "Missile", "导弹" },
                { "Bomb", "炸弹" },
                { "Self", "自己" },
                { "OwnAircraft", "自己的载具" },
                { "OwnPlayer", "自己的玩家" },
                { "GameTarget", "游戏目标" },
                { "LaserTarget", "激光目标" },
                { "PartFormat", "部件: {0}" },
                { "MissileFormat", "导弹: {0}" },
                { "BombFormat", "炸弹: {0}" },
                { "ConfigLanguage", "界面语言。支持 English 和 SimplifiedChinese。" },
                { "ConfigEnabled", "是否允许启用自由相机。ScrollLock 菜单始终可以打开。" },
                { "ConfigCinematicMode", "是否允许相机位置移动，默认 true，跨会话保存。设为 false 进入一般模式：立即停止位移并将移动键交还游戏。鼠标观察、目标锁定与 FOV 不受影响。" },
                { "ConfigNormalSpeed", "普通移动速度，单位为米/秒。默认值：200。" },
                { "ConfigFastSpeed", "快速移动速度，单位为米/秒。默认值：2000。" },
                { "ConfigNormalAcceleration", "普通模式下相机自身位置移动速度的最大变化率，单位为米/秒²，范围 0 到 1000000，默认 80。控制起步、刹停、移动转向及切入普通模式后的速度过渡。设为 0 关闭该模式的加速度限制。不影响相机朝向、目标锁定或 FOV。" },
                { "ConfigFastAcceleration", "快速模式下相机自身位置移动速度的最大变化率，单位为米/秒²，范围 0 到 1000000，默认 800。控制起步、刹停、移动转向及切入快速模式后的速度过渡。设为 0 关闭该模式的加速度限制。不影响相机朝向、目标锁定或 FOV。" },
                { "ConfigMovementSmoothing", "两种模式共用的位置移动速度缓入缓出时间。设为 0 后起步、刹停和移动转向仍受当前模式的加速度限制。当前模式的加速度与移动平滑时间同时为 0 时即时移动。" },
                { "ConfigLookSensitivity", "60 度 FOV 下按住鼠标左键拖拽时，每像素旋转的基准角度。" },
                { "ConfigLookSmoothing", "左键自由观察追随目标角度的平滑时间。设为 0 时立即旋转。" },
                { "ConfigInvertLookY", "反转鼠标垂直观察方向。" },
                { "ConfigScaleLookWithFov", "按当前实际 FOV 的透视比例缩放鼠标观察灵敏度。" },
                { "ConfigDragThreshold", "左键移动超过该像素距离后才开始旋转相机并解除目标锁定。" },
                { "ConfigFovSensitivity", "鼠标滚轮调整 FOV 的灵敏度；自动 FOV 生效时用于调整期望参考面积。" },
                { "ConfigFovSmoothing", "手动与自动 FOV 共用的平滑时间，锁定任何目标时仍生效，与目标朝向平滑独立。设为 0 时缩放立即变化；非零时自动 FOV 追赶过程中允许构图比例暂时偏离。" },
                { "ConfigMaximumFov", "自由相机允许的最大 FOV。最小 FOV 固定为 0.1 度。" },
                { "ConfigFocusDistance", "鼠标中键射线能够锁定目标的最大距离，单位为米。" },
                { "ConfigFocusSmoothing", "锁定地形点时的相机旋转平滑时间。移动目标始终在每个画面帧直接对准当前原始位置，不受此设置影响。该设置不会影响 FOV 缩放平滑。" },
                { "ConfigAutoHideUi", "进入自由相机时自动隐藏游戏 UI。" },
                { "ConfigShowStatus", "游戏 UI 可见时显示自由相机状态消息。" },
                { "ConfigShowQuickMenu", "在飞行场景显示可拖动的自由相机快捷入口。" },
                { "ConfigQuickX", "快捷入口在屏幕可移动区域内的水平位置，范围 0 到 1。通常由拖拽自动保存。" },
                { "ConfigQuickY", "快捷入口在屏幕可移动区域内的垂直位置，范围 0 到 1。通常由拖拽自动保存。" },
                { "ConfigToggleCamera", "进入或退出自由相机。" },
                { "ConfigToggleMenu", "显示或隐藏独立自由相机菜单。" },
                { "ConfigToggleCinematicMode", "自由相机启用且键盘未被菜单或文字输入占用时，切换运镜模式。默认主键盘数字 0（Alpha0），不是小键盘 Keypad0。选择的模式保存在本地。" },
                { "ConfigMoveForward", "自由相机向前移动。" },
                { "ConfigMoveBackward", "自由相机向后移动。" },
                { "ConfigMoveLeft", "自由相机向左移动。" },
                { "ConfigMoveRight", "自由相机向右移动。" },
                { "ConfigMoveUp", "自由相机向上移动。" },
                { "ConfigMoveDown", "自由相机向下移动。" },
                { "ConfigToggleSpeed", "切换普通与快速移动速度。" },
                { "ConfigLockSelf", "将自由相机锁定到本地玩家当前载具或角色。" },
                { "ConfigFocusSelectedTarget", "将自由相机锁定到本地玩家目标系统当前选中的游戏目标。" },
                { "ConfigToggleAutoFov", "切换自动 FOV。默认使用主键盘 Equals 键，而非小键盘加号。无有效锁定目标时待命，生效时滚轮调整期望参考面积。" },
                { "ConfigKeyRevision", "内部键位迁移版本。请勿手动修改。" },
                { "ConfigMovementSpeedRevision", "内部移动速度迁移版本。请勿手动修改。" }
            };

        private static readonly Dictionary<string, string> KeyByEnglish = CreateReverseMap(English);
        private static readonly Dictionary<string, string> KeyBySimplifiedChinese =
            CreateReverseMap(SimplifiedChinese);

        private static ConfigEntry<string> _language;

        internal static bool IsSimplifiedChinese
        {
            get
            {
                return _language != null && string.Equals(
                    NormalizeLanguageCode(_language.Value),
                    SimplifiedChineseCode,
                    StringComparison.Ordinal);
            }
        }

        internal static int LanguageIndex
        {
            get { return IsSimplifiedChinese ? 1 : 0; }
        }

        internal static string CurrentLanguageName
        {
            get { return IsSimplifiedChinese ? "简体中文" : "English"; }
        }

        internal static void Initialize(ConfigEntry<string> language)
        {
            _language = language;
            if (_language == null)
            {
                return;
            }

            string normalized = NormalizeLanguageCode(_language.Value);
            if (!string.Equals(_language.Value, normalized, StringComparison.Ordinal))
            {
                _language.Value = normalized;
            }
        }

        internal static void SelectLanguage(int index)
        {
            if (_language != null)
            {
                _language.Value = index == 1 ? SimplifiedChineseCode : EnglishCode;
            }
        }

        internal static string Text(string key)
        {
            string value;
            Dictionary<string, string> selected = IsSimplifiedChinese
                ? SimplifiedChinese
                : English;
            if (selected.TryGetValue(key, out value))
            {
                return value;
            }

            return English.TryGetValue(key, out value) ? value : key;
        }

        internal static string Format(string key, params object[] arguments)
        {
            return string.Format(Text(key), arguments);
        }

        internal static string LocalizeDynamic(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string key;
            if (KeyByEnglish.TryGetValue(value, out key) ||
                KeyBySimplifiedChinese.TryGetValue(value, out key))
            {
                return Text(key);
            }

            string payload;
            if (TryExtract(value, "已锁定 ", "。", out payload) ||
                TryExtract(value, "Focused on ", ".", out payload))
            {
                return Format("LockedFormat", LocalizeFocusName(payload));
            }

            if (TryExtract(value, "目标已丢失: ", string.Empty, out payload) ||
                TryExtract(value, "Focus target lost: ", string.Empty, out payload))
            {
                return Format("TargetLostFormat", LocalizeFocusName(payload));
            }

            if (TryExtract(
                    value,
                    "游戏 UI 已隐藏；按 ",
                    " 可重新打开自由相机菜单。",
                    out payload) ||
                TryExtract(
                    value,
                    "Game UI hidden. Press ",
                    " to reopen the free-camera menu.",
                    out payload))
            {
                return Format("GameUiHiddenFormat", payload);
            }

            return LocalizeFocusName(value);
        }

        internal static string LocalizeFocusName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Text("None");
            }

            string key;
            if (KeyByEnglish.TryGetValue(value, out key) ||
                KeyBySimplifiedChinese.TryGetValue(value, out key))
            {
                return Text(key);
            }

            string payload;
            if (TryExtract(value, "部件: ", string.Empty, out payload) ||
                TryExtract(value, "part: ", string.Empty, out payload))
            {
                return Format("PartFormat", payload);
            }

            if (TryExtract(value, "导弹: ", string.Empty, out payload) ||
                TryExtract(value, "missile: ", string.Empty, out payload))
            {
                return Format("MissileFormat", payload);
            }

            if (TryExtract(value, "炸弹: ", string.Empty, out payload) ||
                TryExtract(value, "bomb: ", string.Empty, out payload))
            {
                return Format("BombFormat", payload);
            }

            return value;
        }

        private static string NormalizeLanguageCode(string value)
        {
            if (string.Equals(value, SimplifiedChineseCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Chinese", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "zh-CN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "简体中文", StringComparison.Ordinal))
            {
                return SimplifiedChineseCode;
            }

            return EnglishCode;
        }

        private static Dictionary<string, string> CreateReverseMap(
            Dictionary<string, string> source)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in source)
            {
                if (!result.ContainsKey(pair.Value))
                {
                    result.Add(pair.Value, pair.Key);
                }
            }

            return result;
        }

        private static bool TryExtract(
            string value,
            string prefix,
            string suffix,
            out string payload)
        {
            payload = null;
            if (!value.StartsWith(prefix, StringComparison.Ordinal) ||
                !value.EndsWith(suffix, StringComparison.Ordinal) ||
                value.Length < prefix.Length + suffix.Length)
            {
                return false;
            }

            payload = value.Substring(
                prefix.Length,
                value.Length - prefix.Length - suffix.Length);
            return true;
        }
    }

}
