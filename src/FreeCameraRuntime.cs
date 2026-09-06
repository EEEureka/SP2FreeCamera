using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Craft;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Cameras;
using Assets.Scripts.Flight.Combat;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.Flight.UI.Targeting;
using Assets.Scripts.Input;
using Assets.Scripts.Scenes.Startup;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SP2FreeCamera
{
    internal sealed class FreeCameraRuntime : MonoBehaviour
    {
        private Plugin _settings;
        private FreeCameraMenu _menu;
        private FreeCameraQuickMenu _quickMenu;
        private FreeCameraKeyboardCapture _keyboardCapture;
        private FreeCameraMenuInputBlocker _menuInputBlocker;
        private CameraManagerScript _cameraManager;
        private CameraController _previousController;
        private FreeCameraController _freeCameraController;
        private FlightUIScript _flightUi;
        private readonly List<RaycastResult> _pointerRaycastResults =
            new List<RaycastResult>(8);
        private readonly List<MonoBehaviour> _pointerHitBehaviours =
            new List<MonoBehaviour>(8);
        private EventSystem _pointerEventSystem;
        private PointerEventData _pointerEventData;

        private bool _active;
        private bool _controllerRegistered;
        private bool _fastMode;
        private bool _autoFovEnabled;
        private bool _menuVisible;
        private bool _menuPointerCaptured;
        private bool _shuttingDown;
        private bool _applicationQuitting;

        private float _savedFov;
        private bool _savedUiVisible;
        private bool _savedUiHidden;
        private bool _uiChangedByPlugin;
        private bool _lastObservedUiVisible;
        private bool _hasObservedUiVisibility;
        private FlightUIScript _observedFlightUi;

        private bool _cameraCursorSnapshotValid;
        private CursorLockMode _cameraCursorLockMode;
        private bool _cameraCursorVisible;
        private bool _menuCursorSnapshotValid;
        private CursorLockMode _menuCursorLockMode;
        private bool _menuCursorVisible;
        private CameraManagerScript _menuCursorOwnerManager;
        private CameraController _menuCursorOwnerController;
        private int _menuCursorOwnerSceneHandle = -1;

        private string _statusText = "就绪";
        private float _statusExpiresAt;
        private string _gameUiToggleBindingText;
        private float _nextGameUiToggleBindingRefreshTime;

        internal static FreeCameraRuntime Instance { get; private set; }

        internal static bool IsFreeCameraActive
        {
            get { return Instance != null && Instance._active; }
        }

        internal Plugin Settings
        {
            get { return _settings; }
        }

        internal bool Active
        {
            get { return _active; }
        }

        internal bool FastMode
        {
            get { return _fastMode; }
        }

        internal bool AutoFovEnabled
        {
            get { return _autoFovEnabled; }
        }

        internal string AutoFovStatusText
        {
            get
            {
                if (!_autoFovEnabled)
                {
                    return Localization.Text("AutoFovOff");
                }
                if (_freeCameraController == null || !_freeCameraController.AutoFovActive)
                {
                    return Localization.Text("AutoFovWaiting");
                }
                string percent = (_freeCameraController.AutoFovAreaRatio * 100).ToString(
                    "G4", System.Globalization.CultureInfo.InvariantCulture);
                return Localization.Format("AutoFovActiveFormat", percent);
            }
        }

        internal bool MenuVisible
        {
            get { return _menuVisible; }
        }

        internal bool IsMenuPointerBlockingInput
        {
            get
            {
                return _menuPointerCaptured ||
                    (_quickMenu != null && _quickMenu.PointerCaptured) ||
                    IsScreenPointInsidePluginUi(Input.mousePosition);
            }
        }

        internal string StatusText
        {
            get
            {
                if (_statusExpiresAt > 0f && Time.unscaledTime > _statusExpiresAt)
                {
                    return _active ? "自由相机运行中" : "就绪";
                }

                return _statusText;
            }
        }

        internal float CurrentFov
        {
            get
            {
                if (_freeCameraController != null)
                {
                    return _freeCameraController.CurrentFov;
                }

                CameraManagerScript manager = CameraManagerScript.Instance;
                return manager != null && manager.MainCamera != null
                    ? manager.MainCamera.fieldOfView
                    : 0f;
            }
        }

        internal float TargetFov
        {
            get
            {
                return _freeCameraController != null
                    ? _freeCameraController.TargetFov
                    : CurrentFov;
            }
        }

        internal Vector3 QuickMovementAxes
        {
            get
            {
                return _quickMenu != null
                    ? _quickMenu.MovementAxes
                    : Vector3.zero;
            }
        }

        internal string FocusTargetName
        {
            get
            {
                return _freeCameraController != null
                    ? _freeCameraController.FocusTargetName
                    : "无";
            }
        }

        internal void Initialize(Plugin settings)
        {
            _settings = settings;
            Instance = this;
            _menu = new FreeCameraMenu(this);
            _quickMenu = new FreeCameraQuickMenu(this);
            _keyboardCapture = new FreeCameraKeyboardCapture(Plugin.Log);
            _menuInputBlocker = gameObject.AddComponent<FreeCameraMenuInputBlocker>();
            _menuInputBlocker.Initialize(this);
        }

        private void Update()
        {
            if (_shuttingDown || _settings == null)
            {
                return;
            }

            ValidateInactiveMenuCursorOwner();
            if (_quickMenu != null)
            {
                _quickMenu.UpdateState();
            }

            if (Input.GetKeyDown(_settings.ToggleMenuKey.Value))
            {
                ToggleMenu();
            }

            UpdateMenuPointerCapture();

            if (Input.GetKeyDown(_settings.ToggleCameraKey.Value))
            {
                if (_active)
                {
                    ExitFreeCamera();
                }
                else if (CanEnterFromHotkey() && _settings.Enabled.Value)
                {
                    EnterFreeCamera();
                }
                else if (CanEnterFromHotkey())
                {
                    Notify("自由相机已在配置中禁用。", true);
                }
            }

            if (!_active)
            {
                SynchronizeFlightUiState();
                return;
            }

            CameraManagerScript currentManager = CameraManagerScript.Instance;
            if (currentManager == null || currentManager != _cameraManager)
            {
                StopSession(false, false, false, "场景已切换，自由相机已安全退出。", true);
                return;
            }

            if (_cameraManager.Controller != _freeCameraController)
            {
                StopSession(false, false, false, "游戏已切换到其他相机，自由相机已退出。", true);
                return;
            }

            if (_keyboardCapture != null)
            {
                _keyboardCapture.Refresh();
            }

            bool canProcessKeyboardInput = CanProcessKeyboardInput();
            KeyCode toggleAutoFovKey = _settings.ToggleAutoFovKey.Value;
            if (toggleAutoFovKey != KeyCode.None && canProcessKeyboardInput &&
                Input.GetKeyDown(toggleAutoFovKey))
            {
                SetAutoFovEnabled(!_autoFovEnabled);
            }
            KeyCode lockSelfKey = _settings.LockSelfKey.Value;
            if (lockSelfKey != KeyCode.None && canProcessKeyboardInput &&
                Input.GetKeyDown(lockSelfKey))
            {
                LockLocalPlayerTarget();
            }
            else
            {
                KeyCode focusSelectedTargetKey = _settings.FocusSelectedTargetKey.Value;
                if (focusSelectedTargetKey != KeyCode.None && canProcessKeyboardInput &&
                    Input.GetKeyDown(focusSelectedTargetKey))
                {
                    FocusCurrentSelectedTarget();
                }
            }

            SynchronizeFlightUiState();
            _freeCameraController.ProcessFrame(Time.unscaledDeltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                return;
            }

            _menuPointerCaptured = false;
            if (_quickMenu != null)
            {
                _quickMenu.Collapse();
            }
            if (_freeCameraController != null)
            {
                _freeCameraController.ResetInputSmoothing();
            }
        }

        private void OnGUI()
        {
            if (_shuttingDown || _menu == null || _quickMenu == null)
            {
                return;
            }

            _quickMenu.Draw();
            _menu.Draw();
        }

        internal bool EnterFreeCamera()
        {
            if (_active)
            {
                return true;
            }

            if (_settings == null || !_settings.Enabled.Value)
            {
                Notify("自由相机已在配置中禁用。", true);
                return false;
            }

            if (global::Assets.Scripts.Game.Instance == null || FlightSceneScript.Instance == null)
            {
                Notify("自由相机只能在飞行场景中启用。", true);
                return false;
            }

            if (global::Assets.Scripts.Game.Instance.XRDeviceManager != null &&
                global::Assets.Scripts.Game.Instance.XRDeviceManager.HmdActive)
            {
                Notify("当前版本暂不支持在 VR 模式启用自由相机。", true);
                return false;
            }

            CameraManagerScript manager = CameraManagerScript.Instance;
            FlightUIScript flightUi = FlightSceneScript.Instance.FlightUI;
            if (manager == null || manager.MainCamera == null || manager.CameraTransform == null ||
                manager.Controller == null || flightUi == null)
            {
                Notify("飞行相机尚未初始化，请稍后重试。", true);
                return false;
            }

            CaptureCameraCursorState();

            _cameraManager = manager;
            _previousController = manager.Controller;
            _flightUi = flightUi;
            _savedFov = manager.MainCamera.fieldOfView;
            _savedUiVisible = flightUi.Visible;
            _savedUiHidden = FlightUIScript.UIHidden;
            _lastObservedUiVisible = flightUi.Visible;
            _hasObservedUiVisibility = true;
            _observedFlightUi = flightUi;
            FlightUIScript.UIHidden = !flightUi.Visible;
            _uiChangedByPlugin = false;
            _fastMode = false;

            FreeCameraController controller = new FreeCameraController(manager, this, _savedFov);
            _freeCameraController = controller;
            _active = true;

            try
            {
                // Native direct-view switching assumes the current controller is present in
                // this list. Registering prevents its search loop from hanging while freecam
                // is selected, and also makes next/previous camera keys behave normally.
                manager.AddCamera(controller);
                _controllerRegistered = true;
                manager.SwitchToCamera(controller);
                if (manager.Controller != controller)
                {
                    throw new System.InvalidOperationException("Camera manager rejected the free camera controller.");
                }

                if (_keyboardCapture != null)
                {
                    _keyboardCapture.Capture(
                        _settings.MoveForwardKey.Value,
                        _settings.MoveBackwardKey.Value,
                        _settings.MoveLeftKey.Value,
                        _settings.MoveRightKey.Value,
                        _settings.MoveUpKey.Value,
                        _settings.MoveDownKey.Value,
                        _settings.ToggleAutoFovKey.Value);
                }

                if (_settings.AutoHideUi.Value)
                {
                    SetGameUiVisible(false, true);
                }

                ForcePointerAvailable();
                Notify("自由相机已启用。", false);
                return true;
            }
            catch (System.Exception exception)
            {
                Plugin.Log.LogError(
                    "Failed to enter free camera: " + LogPrivacy.ExceptionSummary(exception));
                StopSession(true, true, true, "自由相机启用失败，已恢复原相机。", true);
                return false;
            }
        }

        internal bool EnterFreeCameraFromMenu()
        {
            if (!CanEnterFromUserAction(true))
            {
                Notify("当前游戏界面正在处理输入，暂时不能切换自由相机。", true);
                return false;
            }

            return EnterFreeCamera();
        }

        internal void ExitFreeCamera()
        {
            StopSession(true, true, true, "自由相机已关闭。", false);
        }

        internal void ToggleFastMode()
        {
            _fastMode = !_fastMode;
            Notify(_fastMode ? "已切换为快速移动。" : "已切换为普通移动。", false);
        }

        internal void SetAutoFovEnabled(bool enabled)
        {
            if (_autoFovEnabled == enabled)
            {
                return;
            }
            _autoFovEnabled = enabled;
            if (_freeCameraController != null)
            {
                _freeCameraController.SetAutoFovEnabled(enabled);
            }
            Notify(Localization.Text(enabled ? "AutoFovEnabled" : "AutoFovDisabled"), false);
        }

        internal void ClearFocusTarget()
        {
            if (_freeCameraController != null)
            {
                _freeCameraController.ClearFocusTarget(true);
            }
        }

        internal void LockLocalPlayerTarget()
        {
            if (!_active || _freeCameraController == null)
            {
                Notify("请先启用自由相机。", true);
                return;
            }

            FlightSceneScript scene = FlightSceneScript.Instance;
            FlightScenePlayer player = scene != null ? scene.LocalPlayer : null;
            if (player == null || player.IsUnloaded)
            {
                Notify("当前本地玩家对象尚未就绪。", true);
                return;
            }

            _freeCameraController.SetFocusTarget(FocusTarget.CreateSelf(player));
        }

        internal void FocusCurrentSelectedTarget()
        {
            if (!_active || _freeCameraController == null)
            {
                Notify(Localization.Text("EnableFirst"), true);
                return;
            }

            AircraftScript aircraft = null;
            TargetingScript targetingUi = _flightUi != null
                ? _flightUi.TargetingSystem
                : null;
            if (targetingUi != null)
            {
                aircraft = targetingUi.Aircraft;
            }

            if (aircraft == null)
            {
                FlightSceneScript scene = FlightSceneScript.Instance;
                FlightScenePlayer player = scene != null ? scene.LocalPlayer : null;
                aircraft = player != null ? player.CurrentOrPreviousAircraft : null;
            }

            Target target = aircraft != null && aircraft.TargetingSystem != null
                ? aircraft.TargetingSystem.CurrentTarget
                : null;
            if (target == null)
            {
                Notify(Localization.Text("NoSelectedGameTarget"), true);
                return;
            }

            FocusTarget focusTarget = FocusTarget.CreateGameTarget(target);
            if (focusTarget == null)
            {
                Notify(Localization.Text("TargetUnavailable"), true);
                return;
            }

            _freeCameraController.SetFocusTarget(focusTarget);
        }

        internal bool CanProcessKeyboardInput()
        {
            if (!_active || _menuVisible || !Application.isFocused ||
                global::Assets.Scripts.Game.Instance == null ||
                global::Assets.Scripts.Game.Instance.UserInterface == null)
            {
                return false;
            }

            return global::Assets.Scripts.Game.Instance.UserInterface.AllowKeyboardInputs;
        }

        internal bool CanProcessPointerInput()
        {
            if (!CanProcessKeyboardInput() || _flightUi == null)
            {
                return false;
            }

            Vector3 mousePosition = Input.mousePosition;
            if (mousePosition.x < 0f || mousePosition.y < 0f ||
                mousePosition.x > Screen.width || mousePosition.y > Screen.height)
            {
                return false;
            }

            if (ShouldBlockScreenInput(mousePosition))
            {
                return false;
            }

            // Use this frame's frontmost UI hit for both target boxes and the
            // game-view surface. UIInfo.IsInteracting and hover callbacks can
            // lag behind a moving pointer/target box by a frame; trusting them
            // here would cancel an accepted drag when entering OR leaving it.
            bool? cameraSurface = GetPointerCameraSurface(mousePosition);
            if (cameraSurface.HasValue)
            {
                return cameraSurface.Value;
            }

            global::Assets.Scripts.Game game = global::Assets.Scripts.Game.Instance;
            return game.UIInfo != null && !game.UIInfo.IsInteracting &&
                (!_flightUi.Visible || _flightUi.IsPointerInsideGameView);
        }

        internal bool CanProcessFocusSelectionInput()
        {
            // Picking, left-drag look and wheel zoom share the same UI boundary.
            // Target boxes are transparent to camera input, not to all game UI.
            return CanProcessPointerInput();
        }

        private bool? GetPointerCameraSurface(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return null;
            }

            if (_pointerEventSystem != eventSystem || _pointerEventData == null)
            {
                _pointerEventSystem = eventSystem;
                _pointerEventData = new PointerEventData(eventSystem);
            }
            _pointerEventData.Reset();
            _pointerEventData.position = screenPosition;
            _pointerRaycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _pointerRaycastResults);
            for (int i = 0; i < _pointerRaycastResults.Count; i++)
            {
                RaycastResult hit = _pointerRaycastResults[i];
                GameObject hitObject = hit.gameObject;
                if (hitObject == null || !(hit.module is GraphicRaycaster))
                {
                    // A physics raycast against the rendered world is not UI.
                    continue;
                }

                _pointerHitBehaviours.Clear();
                hitObject.GetComponentsInParent(false, _pointerHitBehaviours);
                for (int j = 0; j < _pointerHitBehaviours.Count; j++)
                {
                    if (_pointerHitBehaviours[j] is ITargetBox)
                    {
                        return true;
                    }
                }

                // Use the nearest input handler, so a real button/panel in front
                // of the game view keeps its input instead of leaking through.
                GameObject inputHandler =
                    ExecuteEvents.GetEventHandler<IPointerDownHandler>(hitObject);
                return inputHandler != null &&
                    inputHandler.GetComponent<FlightScreenInputScript>() != null;
            }

            return null;
        }

        internal void SetGameUiVisible(bool visible, bool closeMenuWhenHidden)
        {
            FlightUIScript flightUi = _flightUi;
            if (flightUi == null && FlightSceneScript.Instance != null)
            {
                flightUi = FlightSceneScript.Instance.FlightUI;
            }

            if (flightUi == null)
            {
                Notify("当前没有可控制的飞行 UI。", true);
                return;
            }

            if (_active)
            {
                _uiChangedByPlugin = true;
            }

            flightUi.Visible = visible;
            FlightUIScript.UIHidden = !visible;
            _lastObservedUiVisible = visible;
            _hasObservedUiVisibility = true;
            _observedFlightUi = flightUi;

            if (!visible && closeMenuWhenHidden && _menuVisible)
            {
                SetMenuVisible(false);
            }

            Notify(
                visible
                    ? "游戏 UI 已显示。"
                    : Localization.Format(
                        "GameUiHiddenRestoreFormat",
                        GetGameUiToggleBindingText(),
                        _settings.ToggleMenuKey.Value),
                false);
        }

        internal string GetGameUiToggleBindingText()
        {
            float now = Time.unscaledTime;
            if (!string.IsNullOrEmpty(_gameUiToggleBindingText) &&
                now < _nextGameUiToggleBindingRefreshTime)
            {
                return _gameUiToggleBindingText;
            }

            _nextGameUiToggleBindingRefreshTime = now + 1f;
            try
            {
                IGameInput screenshotMode = GameInputs.Instance != null
                    ? GameInputs.Instance.ScreenshotMode
                    : null;
                if (screenshotMode == null)
                {
                    _gameUiToggleBindingText = Localization.Text("Unbound");
                    return _gameUiToggleBindingText;
                }

                string binding = screenshotMode.GetKeyboardPrimaryBindingText();
                if (string.IsNullOrWhiteSpace(binding))
                {
                    binding = screenshotMode.GetKeyboardSecondaryBindingText();
                }
                if (string.IsNullOrWhiteSpace(binding))
                {
                    binding = screenshotMode.GetFirstBindingText();
                }

                _gameUiToggleBindingText = string.IsNullOrWhiteSpace(binding)
                    ? Localization.Text("Unbound")
                    : binding;
                return _gameUiToggleBindingText;
            }
            catch (System.Exception)
            {
                // The input maps can be rebuilt while a scene is loading. The next
                // GUI pass retries and will display the current binding once ready.
                _gameUiToggleBindingText = Localization.Text("Unbound");
                return _gameUiToggleBindingText;
            }
        }

        internal bool IsGameUiVisible()
        {
            FlightUIScript flightUi = _flightUi;
            if (flightUi == null && FlightSceneScript.Instance != null)
            {
                flightUi = FlightSceneScript.Instance.FlightUI;
            }

            return flightUi == null || flightUi.Visible;
        }

        internal void ToggleMenu()
        {
            SetMenuVisible(!_menuVisible);
        }

        internal void SetMenuVisible(bool visible)
        {
            if (_menuVisible == visible)
            {
                return;
            }

            _menuVisible = visible;
            _menuPointerCaptured = false;

            if (visible)
            {
                if (_freeCameraController != null)
                {
                    _freeCameraController.ResetInputSmoothing();
                }

                if (_quickMenu != null)
                {
                    _quickMenu.Collapse();
                }

                if (!_active)
                {
                    _menuCursorLockMode = Cursor.lockState;
                    _menuCursorVisible = Cursor.visible;
                    _menuCursorSnapshotValid = true;
                    _menuCursorOwnerManager = CameraManagerScript.Instance;
                    _menuCursorOwnerController = _menuCursorOwnerManager != null
                        ? _menuCursorOwnerManager.Controller
                        : null;
                    _menuCursorOwnerSceneHandle = SceneManager.GetActiveScene().handle;
                }

                ForcePointerAvailable();
            }
            else
            {
                if (_active)
                {
                    ForcePointerAvailable();
                }
                else
                {
                    RestoreMenuCursorState();
                }
            }
        }

        internal bool TryGetInputBlockerRect(int index, out Rect rect)
        {
            rect = default(Rect);
            if (_shuttingDown)
            {
                return false;
            }

            if (index == 0 && _menuVisible && _menu != null)
            {
                rect = _menu.WindowRect;
                return true;
            }

            if (index == 1 && _quickMenu != null && _quickMenu.LauncherVisible)
            {
                rect = _quickMenu.LauncherRect;
                return true;
            }

            if (index == 2 && _quickMenu != null && _quickMenu.Expanded)
            {
                rect = _quickMenu.PanelRect;
                return true;
            }

            return false;
        }

        internal bool IsScreenPointInsidePluginUi(Vector2 screenPosition)
        {
            Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Rect rect;
            for (int i = 0; i < 3; i++)
            {
                if (TryGetInputBlockerRect(i, out rect) && rect.Contains(guiPosition))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool ShouldBlockScreenInput(Vector2 screenPosition)
        {
            return _menuPointerCaptured ||
                (_quickMenu != null && _quickMenu.PointerCaptured) ||
                IsScreenPointInsidePluginUi(screenPosition);
        }

        internal void Notify(string message, bool warning)
        {
            Notify(message, warning, null);
        }

        internal void Notify(string message, bool warning, string safeLogMessage)
        {
            message = Localization.LocalizeDynamic(message);
            string logMessage = string.IsNullOrEmpty(safeLogMessage)
                ? message
                : Localization.LocalizeDynamic(safeLogMessage);
            logMessage = LogPrivacy.Sanitize(logMessage);
            _statusText = message;
            _statusExpiresAt = Time.unscaledTime + 4f;

            if (warning)
            {
                Plugin.Log.LogWarning(logMessage);
            }
            else
            {
                Plugin.Log.LogInfo(logMessage);
            }

            if (_settings == null || !_settings.ShowStatusMessages.Value)
            {
                return;
            }

            FlightUIScript flightUi = _flightUi;
            if (flightUi == null && FlightSceneScript.Instance != null)
            {
                flightUi = FlightSceneScript.Instance.FlightUI;
            }

            if (flightUi != null && flightUi.Visible)
            {
                flightUi.ShowMessage(message, 2f, warning);
            }
        }

        internal void PrepareForApplicationQuit()
        {
            _applicationQuitting = true;
            if (_keyboardCapture != null)
            {
                _keyboardCapture.Restore();
            }
            _shuttingDown = true;
            _active = false;
            _menuVisible = false;
            _menuPointerCaptured = false;
            if (_quickMenu != null)
            {
                _quickMenu.Collapse();
            }
            _freeCameraController = null;
            _controllerRegistered = false;
            _previousController = null;
            _cameraManager = null;
            _flightUi = null;
            _observedFlightUi = null;
            _hasObservedUiVisibility = false;
            _cameraCursorSnapshotValid = false;
            DiscardMenuCursorSnapshot();
            Instance = null;
        }

        internal void Shutdown(bool restoreState)
        {
            if (_shuttingDown && _applicationQuitting)
            {
                return;
            }

            _shuttingDown = true;
            if (_keyboardCapture != null)
            {
                _keyboardCapture.Restore();
            }
            if (_active || _freeCameraController != null)
            {
                StopSession(restoreState, restoreState, restoreState, null, false);
            }
            else if (restoreState)
            {
                RestoreMenuCursorState();
            }

            _menuVisible = false;
            _menuPointerCaptured = false;
            if (_quickMenu != null)
            {
                _quickMenu.Collapse();
            }
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }

            _keyboardCapture = null;
        }

        private void StopSession(
            bool restoreCamera,
            bool restoreFov,
            bool restoreCursorState,
            string message,
            bool warning)
        {
            FreeCameraController controller = _freeCameraController;
            CameraManagerScript manager = _cameraManager;
            CameraController previousController = _previousController;
            FlightUIScript flightUi = _flightUi;
            bool ownedCurrentCamera = manager != null && manager.Controller == controller;

            _active = false;
            if (_keyboardCapture != null)
            {
                _keyboardCapture.Restore();
            }
            if (controller != null)
            {
                controller.ResetInputSmoothing();
            }

            if (restoreCamera && ownedCurrentCamera)
            {
                try
                {
                    if (previousController != null && previousController.IsActive)
                    {
                        manager.SwitchToCamera(previousController);
                    }
                    else
                    {
                        manager.SwitchToDefaultCamera();
                    }

                    if (restoreFov && manager.MainCamera != null)
                    {
                        manager.SetCameraFov(_savedFov);
                    }
                }
                catch (System.Exception exception)
                {
                    Plugin.Log.LogWarning(
                        "Failed to restore the previous camera cleanly: " +
                        LogPrivacy.ExceptionSummary(exception));
                }
            }

            if (_controllerRegistered && manager != null)
            {
                try
                {
                    manager.UnregisterCustomCameraVantage(controller);
                }
                catch (System.Exception exception)
                {
                    Plugin.Log.LogWarning(
                        "Failed to unregister the free camera controller: " +
                        LogPrivacy.ExceptionSummary(exception));
                }
            }

            if (controller != null)
            {
                try
                {
                    controller.OnDestroy();
                }
                catch (System.Exception exception)
                {
                    Plugin.Log.LogWarning(
                        "Free camera cleanup failed: " + LogPrivacy.ExceptionSummary(exception));
                }
            }

            FlightSceneScript currentScene = FlightSceneScript.Instance;
            FlightUIScript currentFlightUi = currentScene != null ? currentScene.FlightUI : null;
            bool ownsCurrentFlightUi = flightUi != null && currentFlightUi == flightUi;
            try
            {
                if (!ownsCurrentFlightUi)
                {
                    if (currentFlightUi != null)
                    {
                        FlightUIScript.UIHidden = !currentFlightUi.Visible;
                    }

                    _observedFlightUi = null;
                    _hasObservedUiVisibility = false;
                }
                else if (_uiChangedByPlugin)
                {
                    FlightUIScript.UIHidden = _savedUiHidden;
                    flightUi.Visible = _savedUiVisible;
                }
                else
                {
                    FlightUIScript.UIHidden = !flightUi.Visible;
                }

                if (ownsCurrentFlightUi)
                {
                    _observedFlightUi = flightUi;
                    _lastObservedUiVisible = flightUi.Visible;
                    _hasObservedUiVisibility = true;
                }
            }
            catch (System.Exception exception)
            {
                FlightUIScript.UIHidden = currentFlightUi != null
                    ? !currentFlightUi.Visible
                    : _savedUiHidden;
                _observedFlightUi = null;
                _hasObservedUiVisibility = false;
                Plugin.Log.LogWarning(
                    "Flight UI cleanup failed: " + LogPrivacy.ExceptionSummary(exception));
            }

            _freeCameraController = null;
            _controllerRegistered = false;
            _previousController = null;
            _cameraManager = null;
            _flightUi = null;
            _uiChangedByPlugin = false;
            _fastMode = false;
            if (_quickMenu != null)
            {
                _quickMenu.Collapse();
            }

            if (!restoreCursorState)
            {
                _menuVisible = false;
                _menuPointerCaptured = false;
                _cameraCursorSnapshotValid = false;
                DiscardMenuCursorSnapshot();
                UpdateCurrentGameCursor();
            }
            else if (_menuVisible)
            {
                if (_cameraCursorSnapshotValid)
                {
                    _menuCursorLockMode = _cameraCursorLockMode;
                    _menuCursorVisible = _cameraCursorVisible;
                    _menuCursorSnapshotValid = true;
                    _menuCursorOwnerManager = CameraManagerScript.Instance;
                    _menuCursorOwnerController = _menuCursorOwnerManager != null
                        ? _menuCursorOwnerManager.Controller
                        : null;
                    _menuCursorOwnerSceneHandle = SceneManager.GetActiveScene().handle;
                }
                ForcePointerAvailable();
            }
            else
            {
                RestoreCameraCursorState();
            }

            _cameraCursorSnapshotValid = false;

            if (!string.IsNullOrEmpty(message))
            {
                Notify(message, warning);
            }
        }

        private void SynchronizeFlightUiState()
        {
            FlightSceneScript scene = FlightSceneScript.Instance;
            FlightUIScript flightUi = scene != null ? scene.FlightUI : null;
            if (flightUi == null)
            {
                _observedFlightUi = null;
                _hasObservedUiVisibility = false;
                return;
            }

            bool visible = flightUi.Visible;
            if (_observedFlightUi != flightUi || !_hasObservedUiVisibility)
            {
                _observedFlightUi = flightUi;
                _lastObservedUiVisible = visible;
                _hasObservedUiVisibility = true;
                FlightUIScript.UIHidden = !visible;
                return;
            }

            if (visible == _lastObservedUiVisible)
            {
                return;
            }

            _lastObservedUiVisible = visible;
            FlightUIScript.UIHidden = !visible;
            if (!visible && _menuVisible)
            {
                SetMenuVisible(false);
            }
        }

        private void UpdateMenuPointerCapture()
        {
            bool hasPluginUi = _menuVisible ||
                (_quickMenu != null && _quickMenu.LauncherVisible);
            if (!hasPluginUi || !Application.isFocused)
            {
                _menuPointerCaptured = false;
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
                Input.GetMouseButtonDown(2))
            {
                _menuPointerCaptured = IsScreenPointInsidePluginUi(Input.mousePosition);
            }

            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) &&
                !Input.GetMouseButton(2))
            {
                _menuPointerCaptured = false;
            }
        }

        private void CaptureCameraCursorState()
        {
            if (_menuVisible && _menuCursorSnapshotValid)
            {
                _cameraCursorLockMode = _menuCursorLockMode;
                _cameraCursorVisible = _menuCursorVisible;
                DiscardMenuCursorSnapshot();
            }
            else
            {
                _cameraCursorLockMode = Cursor.lockState;
                _cameraCursorVisible = Cursor.visible;
            }

            _cameraCursorSnapshotValid = true;
        }

        private void RestoreCameraCursorState()
        {
            if (!_cameraCursorSnapshotValid)
            {
                return;
            }

            Cursor.lockState = _cameraCursorLockMode;
            Cursor.visible = _cameraCursorVisible;
        }

        private void RestoreMenuCursorState()
        {
            if (!_menuCursorSnapshotValid)
            {
                return;
            }

            if (!IsMenuCursorOwnerCurrent())
            {
                DiscardMenuCursorSnapshot();
                UpdateCurrentGameCursor();
                return;
            }

            Cursor.lockState = _menuCursorLockMode;
            Cursor.visible = _menuCursorVisible;
            DiscardMenuCursorSnapshot();
        }

        private void ValidateInactiveMenuCursorOwner()
        {
            if (_active || !_menuVisible || !_menuCursorSnapshotValid ||
                IsMenuCursorOwnerCurrent())
            {
                return;
            }

            _menuVisible = false;
            _menuPointerCaptured = false;
            DiscardMenuCursorSnapshot();
            UpdateCurrentGameCursor();
        }

        private bool IsMenuCursorOwnerCurrent()
        {
            CameraManagerScript currentManager = CameraManagerScript.Instance;
            CameraController currentController = currentManager != null
                ? currentManager.Controller
                : null;
            return currentManager == _menuCursorOwnerManager &&
                currentController == _menuCursorOwnerController &&
                SceneManager.GetActiveScene().handle == _menuCursorOwnerSceneHandle;
        }

        private void DiscardMenuCursorSnapshot()
        {
            _menuCursorSnapshotValid = false;
            _menuCursorOwnerManager = null;
            _menuCursorOwnerController = null;
            _menuCursorOwnerSceneHandle = -1;
        }

        private bool CanEnterFromHotkey()
        {
            return CanEnterFromUserAction(false);
        }

        private bool CanEnterFromUserAction(bool allowSettingsMenu)
        {
            global::Assets.Scripts.UI.UserInterface userInterface =
                global::Assets.Scripts.Game.Instance != null
                    ? global::Assets.Scripts.Game.Instance.UserInterface
                    : null;
            if ((!allowSettingsMenu && _menuVisible) || !Application.isFocused ||
                userInterface == null)
            {
                return false;
            }

            bool allowKeyboardInputs = allowSettingsMenu
                ? !userInterface.AnyDialogsOpen &&
                    !userInterface.IsTextInputFocused &&
                    !SimplePlanesDevConsoleScript.IsConsoleOpen
                : userInterface.AllowKeyboardInputs;
            if (!allowKeyboardInputs)
            {
                return false;
            }

            return global::Assets.Scripts.Game.Instance.UIInfo == null ||
                !global::Assets.Scripts.Game.Instance.UIInfo.IsInteracting;
        }

        private static void UpdateCurrentGameCursor()
        {
            CameraManagerScript currentManager = CameraManagerScript.Instance;
            CameraController currentController = currentManager != null
                ? currentManager.Controller
                : null;
            if (currentController == null)
            {
                return;
            }

            try
            {
                currentController.UpdateCursor();
            }
            catch (System.Exception exception)
            {
                Plugin.Log.LogWarning(
                    "Current game camera cursor refresh failed: " +
                    LogPrivacy.ExceptionSummary(exception));
            }
        }

        private static void ForcePointerAvailable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
