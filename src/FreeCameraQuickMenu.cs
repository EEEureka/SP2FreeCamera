using Assets.Scripts;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Cameras;
using UnityEngine;

namespace SP2FreeCamera
{
    internal sealed class FreeCameraQuickMenu
    {
        private const int WindowId = 73102642;
        private const float LauncherWidth = 72f;
        private const float LauncherHeight = 40f;
        private const float LauncherMargin = 2f;
        private const float LauncherDragThresholdPixels = 5f;
        private const float PanelWidth = 284f;
        private const float PanelHeight = 450f;
        private const float PanelScrollbarReserve = 20f;
        private const float ButtonGap = 4f;

        private readonly FreeCameraRuntime _runtime;
        private readonly Plugin _settings;

        private Rect _launcherRect = new Rect(18f, 96f, LauncherWidth, LauncherHeight);
        private Rect _panelRect = new Rect(18f, 140f, PanelWidth, PanelHeight);
        private bool _expanded;
        private bool _languageDropdownOpen;
        private Vector3 _movementAxes;
        private Vector2 _panelScrollPosition;
        private bool _launcherPositionInitialized;
        private int _launcherScreenWidth = -1;
        private int _launcherScreenHeight = -1;
        private bool _launcherPointerCaptured;
        private bool _launcherDragging;
        private Vector2 _launcherPressPosition;
        private Vector2 _launcherStartPosition;

        private GUIStyle _windowStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _smallButtonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _statusStyle;

        internal FreeCameraQuickMenu(FreeCameraRuntime runtime)
        {
            _runtime = runtime;
            _settings = runtime.Settings;
        }

        internal bool LauncherVisible
        {
            get
            {
                return _settings != null && _settings.ShowQuickMenu.Value &&
                    !_runtime.MenuVisible &&
                    _runtime.IsGameUiVisible() &&
                    global::Assets.Scripts.Game.Instance != null &&
                    FlightSceneScript.Instance != null &&
                    CameraManagerScript.Instance != null;
            }
        }

        internal bool Expanded
        {
            get { return LauncherVisible && _expanded; }
        }

        internal bool PointerCaptured
        {
            get { return _launcherPointerCaptured; }
        }

        internal Rect LauncherRect
        {
            get
            {
                EnsureLauncherPosition();
                ClampRectsToScreen();
                return _launcherRect;
            }
        }

        internal Rect PanelRect
        {
            get
            {
                ClampRectsToScreen();
                return _panelRect;
            }
        }

        internal Vector3 MovementAxes
        {
            get
            {
                if (!Expanded || !_runtime.Active || !_runtime.CinematicModeEnabled ||
                    !Application.isFocused ||
                    !Input.GetMouseButton(0))
                {
                    return Vector3.zero;
                }

                return _movementAxes;
            }
        }

        internal void UpdateState()
        {
            EnsureLauncherPosition();

            if (!LauncherVisible || !Application.isFocused)
            {
                _movementAxes = Vector3.zero;
                CancelLauncherInteraction(_launcherDragging);
            }
            else
            {
                UpdateLauncherInteraction();
                if (!Input.GetMouseButton(0))
                {
                    _movementAxes = Vector3.zero;
                }
            }

            if (!LauncherVisible)
            {
                _expanded = false;
            }
        }

        internal void ResetMovementInput()
        {
            _movementAxes = Vector3.zero;
        }

        internal void Draw()
        {
            if (!LauncherVisible)
            {
                Collapse();
                return;
            }

            EnsureStyles();
            EnsureLauncherPosition();
            ClampRectsToScreen();

            GUI.Box(
                _launcherRect,
                Localization.Text(_expanded ? "CollapseCamera" : "CameraLauncher"),
                _smallButtonStyle);

            if (_expanded)
            {
                _panelRect = GUI.Window(
                    WindowId,
                    _panelRect,
                    DrawPanel,
                    Localization.Text("QuickTitle"),
                    _windowStyle);
                ClampRectsToScreen();
            }

            ConsumeCurrentPointerEvent();
        }

        internal void Collapse()
        {
            _expanded = false;
            _languageDropdownOpen = false;
            _movementAxes = Vector3.zero;
            CancelLauncherInteraction(_launcherDragging);
        }

        private void UpdateLauncherInteraction()
        {
            Vector3 mouseScreenPosition = Input.mousePosition;
            Vector2 mouseGuiPosition = new Vector2(
                mouseScreenPosition.x,
                Screen.height - mouseScreenPosition.y);

            if (!_launcherPointerCaptured)
            {
                if (Input.GetMouseButtonDown(0) && _launcherRect.Contains(mouseGuiPosition))
                {
                    _launcherPointerCaptured = true;
                    _launcherDragging = false;
                    _launcherPressPosition = mouseGuiPosition;
                    _launcherStartPosition = _launcherRect.position;
                    _movementAxes = Vector3.zero;
                }

                return;
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 delta = mouseGuiPosition - _launcherPressPosition;
                if (!_launcherDragging &&
                    delta.sqrMagnitude >= LauncherDragThresholdPixels * LauncherDragThresholdPixels)
                {
                    _launcherDragging = true;
                    _expanded = false;
                    _languageDropdownOpen = false;
                    _movementAxes = Vector3.zero;
                }

                if (_launcherDragging)
                {
                    _launcherRect.position = _launcherStartPosition + delta;
                    ClampRectsToScreen();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                bool wasDragging = _launcherDragging;
                bool clicked = !wasDragging && _launcherRect.Contains(mouseGuiPosition);
                CancelLauncherInteraction(wasDragging);

                if (clicked)
                {
                    _expanded = !_expanded;
                    if (!_expanded)
                    {
                        _languageDropdownOpen = false;
                    }
                    _movementAxes = Vector3.zero;
                    if (_expanded)
                    {
                        PositionPanelNearLauncher();
                    }
                }

                return;
            }

            if (!Input.GetMouseButton(0))
            {
                CancelLauncherInteraction(_launcherDragging);
            }
        }

        private void CancelLauncherInteraction(bool savePosition)
        {
            if (savePosition)
            {
                SaveLauncherPosition();
            }

            _launcherPointerCaptured = false;
            _launcherDragging = false;
        }

        private void EnsureLauncherPosition()
        {
            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);
            if (_launcherPositionInitialized &&
                _launcherScreenWidth == screenWidth &&
                _launcherScreenHeight == screenHeight)
            {
                return;
            }

            _launcherPointerCaptured = false;
            _launcherDragging = false;
            _launcherScreenWidth = screenWidth;
            _launcherScreenHeight = screenHeight;
            _launcherPositionInitialized = true;

            _launcherRect.width = Mathf.Max(
                1f,
                Mathf.Min(LauncherWidth, screenWidth - LauncherMargin * 2f));
            _launcherRect.height = LauncherHeight;

            float normalizedX = NumericUtility.ClampFinite(
                _settings.QuickLauncherNormalizedX.Value,
                0.0087f,
                0f,
                1f);
            float normalizedY = NumericUtility.ClampFinite(
                _settings.QuickLauncherNormalizedY.Value,
                0.0904f,
                0f,
                1f);
            float travelX = Mathf.Max(
                0f,
                screenWidth - _launcherRect.width - LauncherMargin * 2f);
            float travelY = Mathf.Max(
                0f,
                screenHeight - _launcherRect.height - LauncherMargin * 2f);
            _launcherRect.x = LauncherMargin + normalizedX * travelX;
            _launcherRect.y = LauncherMargin + normalizedY * travelY;
            ClampRectsToScreen();
        }

        private void SaveLauncherPosition()
        {
            ClampRectsToScreen();
            float travelX = Mathf.Max(
                0f,
                Screen.width - _launcherRect.width - LauncherMargin * 2f);
            float travelY = Mathf.Max(
                0f,
                Screen.height - _launcherRect.height - LauncherMargin * 2f);
            float normalizedX = travelX > 0f
                ? Mathf.Clamp01((_launcherRect.x - LauncherMargin) / travelX)
                : 0f;
            float normalizedY = travelY > 0f
                ? Mathf.Clamp01((_launcherRect.y - LauncherMargin) / travelY)
                : 0f;

            _settings.QuickLauncherNormalizedX.Value = normalizedX;
            _settings.QuickLauncherNormalizedY.Value = normalizedY;
        }

        private void PositionPanelNearLauncher()
        {
            ClampRectsToScreen();
            _panelRect.x = _launcherRect.x;
            float below = _launcherRect.y + _launcherRect.height + 8f;
            _panelRect.y = below + _panelRect.height <= Screen.height - LauncherMargin
                ? below
                : _launcherRect.y - _panelRect.height - 8f;
            ClampRectsToScreen();
        }

        private void DrawPanel(int windowId)
        {
            float scrollHeight = Mathf.Max(1f, _panelRect.height - 38f);
            float contentWidth = GetPanelContentWidth();
            float pairedButtonWidth = Mathf.Max(1f, (contentWidth - ButtonGap) * 0.5f);
            _panelScrollPosition.x = 0f;
            _panelScrollPosition = GUILayout.BeginScrollView(
                _panelScrollPosition,
                GUIStyle.none,
                GUI.skin.verticalScrollbar,
                GUILayout.Height(scrollHeight));
            GUILayout.BeginVertical(GUILayout.Width(contentWidth), GUILayout.ExpandWidth(false));

            DrawLanguageSelector(contentWidth);
            GUILayout.Space(4f);

            GUILayout.Label(
                Localization.Text(_runtime.Active ? "FreeCamera" : "GameCamera") +
                "  |  " + Localization.Text(_runtime.FastMode ? "Fast" : "Normal") +
                "  |  FOV " + FormatFov(_runtime.CurrentFov),
                _statusStyle,
                GUILayout.Width(contentWidth));
            GUILayout.Label(
                Localization.Text("Focus") +
                    Localization.LocalizeFocusName(_runtime.FocusTargetName),
                _statusStyle,
                GUILayout.Width(contentWidth));
            GUILayout.Label(_runtime.AutoFovStatusText, _statusStyle, GUILayout.Width(contentWidth));
            GUILayout.Label(_runtime.CinematicModeStatusText, _statusStyle, GUILayout.Width(contentWidth));
            if (GUILayout.Button(
                Localization.Text(_runtime.CinematicModeEnabled ? "DisableCinematicMode" : "EnableCinematicMode"),
                _buttonStyle,
                GUILayout.Width(contentWidth)))
            {
                _runtime.SetCinematicModeEnabled(!_runtime.CinematicModeEnabled);
            }
            if (GUILayout.Button(
                Localization.Text(_runtime.AutoFovEnabled ? "DisableAutoFov" : "EnableAutoFov"),
                _buttonStyle,
                GUILayout.Width(contentWidth)))
            {
                _runtime.SetAutoFovEnabled(!_runtime.AutoFovEnabled);
            }

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            if (GUILayout.Button(
                Localization.Text(_runtime.Active ? "ExitFreeCamera" : "EnterFreeCamera"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                if (_runtime.Active)
                {
                    _runtime.ExitFreeCamera();
                }
                else
                {
                    _runtime.EnterFreeCameraFromMenu();
                }
            }

            bool previousGuiEnabled = GUI.enabled;
            GUI.enabled = previousGuiEnabled && _runtime.Active;
            GUILayout.Space(ButtonGap);
            if (GUILayout.Button(
                Localization.Text("LockSelf"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.LockLocalPlayerTarget();
            }
            GUI.enabled = previousGuiEnabled;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            previousGuiEnabled = GUI.enabled;
            GUI.enabled = previousGuiEnabled && _runtime.Active;
            if (GUILayout.Button(
                Localization.Text("FocusSelectedTarget"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.FocusCurrentSelectedTarget();
            }
            GUILayout.Space(ButtonGap);
            if (GUILayout.Button(
                Localization.Text("ClearLock"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.ClearFocusTarget();
            }
            GUI.enabled = previousGuiEnabled;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            previousGuiEnabled = GUI.enabled;
            GUI.enabled = previousGuiEnabled && _runtime.Active && _runtime.CinematicModeEnabled;
            if (GUILayout.Button(
                Localization.Text(_runtime.FastMode ? "SwitchNormalSpeed" : "SwitchFastSpeed"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.ToggleFastMode();
            }
            GUI.enabled = previousGuiEnabled;
            GUILayout.Space(ButtonGap);
            if (GUILayout.Button(
                Localization.Text(_runtime.IsGameUiVisible() ? "HideGameUi" : "ShowGameUi"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.SetGameUiVisible(!_runtime.IsGameUiVisible(), false);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                Localization.Format(
                    "HideUiShortcutHint",
                    _runtime.GetGameUiToggleBindingText(),
                    _settings.ToggleMenuKey.Value),
                _labelStyle,
                GUILayout.Width(contentWidth));

            GUILayout.Space(5f);
            GUILayout.Label(Localization.Text("HoldButtonsToMove"), _labelStyle);
            Vector3 nextMovement = Vector3.zero;
            previousGuiEnabled = GUI.enabled;
            GUI.enabled = previousGuiEnabled && _runtime.Active && _runtime.CinematicModeEnabled;

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            if (GUILayout.RepeatButton(
                Localization.Text("Forward"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                nextMovement.z += 1f;
            }
            GUILayout.Space(ButtonGap);
            if (GUILayout.RepeatButton(
                Localization.Text("Backward"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                nextMovement.z -= 1f;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            if (GUILayout.RepeatButton(
                Localization.Text("Left"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                nextMovement.x -= 1f;
            }
            GUILayout.Space(ButtonGap);
            if (GUILayout.RepeatButton(
                Localization.Text("Right"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                nextMovement.x += 1f;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            if (GUILayout.RepeatButton(
                Localization.Text("Up"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                nextMovement.y += 1f;
            }
            GUILayout.Space(ButtonGap);
            if (GUILayout.RepeatButton(
                Localization.Text("Down"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                nextMovement.y -= 1f;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = previousGuiEnabled;

            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                _movementAxes = nextMovement;
            }

            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            if (GUILayout.Button(
                Localization.Text("FullSettings"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                Collapse();
                _runtime.SetMenuVisible(true);
            }
            GUILayout.Space(ButtonGap);
            if (GUILayout.Button(
                Localization.Text("Collapse"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                Collapse();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, _panelRect.width, 24f));
        }

        private void DrawLanguageSelector(float contentWidth)
        {
            float labelWidth = Mathf.Min(82f, Mathf.Max(1f, contentWidth * 0.3f));
            float buttonWidth = Mathf.Max(1f, contentWidth - labelWidth - ButtonGap);
            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            GUILayout.Label(
                Localization.Text("Language"),
                _labelStyle,
                GUILayout.Width(labelWidth));
            GUILayout.Space(ButtonGap);
            if (GUILayout.Button(
                Localization.CurrentLanguageName + "  v",
                _buttonStyle,
                GUILayout.Width(buttonWidth)))
            {
                _languageDropdownOpen = !_languageDropdownOpen;
            }
            GUILayout.EndHorizontal();

            if (!_languageDropdownOpen)
            {
                return;
            }

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            GUILayout.Space(labelWidth + ButtonGap);
            GUILayout.BeginVertical(GUILayout.Width(buttonWidth));
            string englishLabel = (Localization.LanguageIndex == 0 ? "✓ " : string.Empty) +
                "English";
            if (GUILayout.Button(
                englishLabel,
                _buttonStyle,
                GUILayout.Width(buttonWidth)))
            {
                SelectLanguage(0);
            }

            string chineseLabel = (Localization.LanguageIndex == 1 ? "✓ " : string.Empty) +
                "简体中文";
            if (GUILayout.Button(
                chineseLabel,
                _buttonStyle,
                GUILayout.Width(buttonWidth)))
            {
                SelectLanguage(1);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private float GetPanelContentWidth()
        {
            int horizontalPadding = _windowStyle != null
                ? _windowStyle.padding.horizontal
                : 28;
            return Mathf.Max(
                1f,
                _panelRect.width - horizontalPadding - PanelScrollbarReserve);
        }

        private void SelectLanguage(int index)
        {
            bool changed = Localization.LanguageIndex != index;
            Localization.SelectLanguage(index);
            _languageDropdownOpen = false;
            if (changed)
            {
                _runtime.Notify(Localization.Text("LanguageChanged"), false);
            }
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
            {
                return;
            }

            FreeCameraGuiTheme.EnsureStyles();
            _windowStyle = FreeCameraGuiTheme.WindowStyle;
            _buttonStyle = new GUIStyle(FreeCameraGuiTheme.ButtonStyle)
            {
                margin = new RectOffset(0, 0, 2, 2)
            };
            _smallButtonStyle = FreeCameraGuiTheme.SmallButtonStyle;
            _labelStyle = FreeCameraGuiTheme.CenteredLabelStyle;
            _statusStyle = FreeCameraGuiTheme.StatusStyle;
        }

        private void ClampRectsToScreen()
        {
            _launcherRect.width = Mathf.Max(
                1f,
                Mathf.Min(LauncherWidth, Screen.width - LauncherMargin * 2f));
            _launcherRect.height = LauncherHeight;
            _launcherRect.x = Mathf.Clamp(
                _launcherRect.x,
                LauncherMargin,
                Mathf.Max(LauncherMargin, Screen.width - _launcherRect.width - LauncherMargin));
            _launcherRect.y = Mathf.Clamp(
                _launcherRect.y,
                LauncherMargin,
                Mathf.Max(LauncherMargin, Screen.height - _launcherRect.height - LauncherMargin));

            _panelRect.width = Mathf.Min(PanelWidth, Mathf.Max(1f, Screen.width - 4f));
            _panelRect.height = Mathf.Min(PanelHeight, Mathf.Max(1f, Screen.height - 4f));
            _panelRect.x = Mathf.Clamp(_panelRect.x, 2f, Mathf.Max(2f, Screen.width - _panelRect.width - 2f));
            _panelRect.y = Mathf.Clamp(_panelRect.y, 2f, Mathf.Max(2f, Screen.height - _panelRect.height - 2f));
        }

        private void ConsumeCurrentPointerEvent()
        {
            Event current = Event.current;
            if (current == null || current.type == EventType.Layout || current.type == EventType.Repaint)
            {
                return;
            }

            bool pointerEvent = current.isMouse || current.type == EventType.ScrollWheel ||
                current.type == EventType.MouseDrag || current.type == EventType.MouseDown ||
                current.type == EventType.MouseUp;
            if (pointerEvent &&
                (_launcherPointerCaptured ||
                    _launcherRect.Contains(current.mousePosition) ||
                    (_expanded && _panelRect.Contains(current.mousePosition))))
            {
                current.Use();
            }
        }

        private static string FormatFov(float value)
        {
            return value > 0f ? value.ToString("0.###") + "°" : "--";
        }
    }
}
