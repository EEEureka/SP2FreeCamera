using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace SP2FreeCamera
{
    internal sealed class FreeCameraMenu
    {
        private const int WindowId = 73102641;
        private const float WindowWidth = 520f;
        private const float WindowHeight = 700f;

        private readonly FreeCameraRuntime _runtime;
        private readonly Plugin _settings;

        private Vector2 _scrollPosition;
        private string _numericStatus;
        private bool _languageDropdownOpen;

        private string _normalSpeedInput;
        private string _fastSpeedInput;
        private string _normalAccelerationInput;
        private string _fastAccelerationInput;
        private string _movementSmoothingTimeInput;
        private string _lookSensitivityInput;
        private string _lookSmoothingTimeInput;
        private string _dragThresholdInput;
        private string _fovScrollSensitivityInput;
        private string _fovSmoothingTimeInput;
        private string _maximumFovInput;
        private string _focusMaximumDistanceInput;
        private string _focusSmoothingTimeInput;

        private GUIStyle _windowStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _toggleStyle;

        internal FreeCameraMenu(FreeCameraRuntime runtime)
        {
            _runtime = runtime;
            _settings = runtime.Settings;
            WindowRect = new Rect(30f, 30f, WindowWidth, WindowHeight);
            RefreshNumericInputs();
        }

        internal Rect WindowRect { get; private set; }

        internal void Draw()
        {
            if (!_runtime.MenuVisible)
            {
                return;
            }

            EnsureStyles();
            ClampWindowToScreen();
            WindowRect = GUI.Window(
                WindowId,
                WindowRect,
                DrawWindow,
                Localization.Text("WindowTitle"),
                _windowStyle);
            ClampWindowToScreen();
            ConsumeCurrentPointerEvent();
        }

        private void DrawWindow(int windowId)
        {
            float contentHeight = Mathf.Max(1f, WindowRect.height - 54f);
            float contentWidth = GetContentWidth();
            float pairedButtonWidth = GetPairedButtonWidth();
            _scrollPosition.x = 0f;
            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                GUIStyle.none,
                GUI.skin.verticalScrollbar,
                GUILayout.Height(contentHeight));
            GUILayout.BeginVertical(GUILayout.Width(contentWidth), GUILayout.ExpandWidth(false));

            DrawLanguageSelector();

            GUILayout.Space(10f);
            GUILayout.Label(Localization.Text("Status"), _sectionStyle);
            GUILayout.Label(Localization.LocalizeDynamic(_runtime.StatusText), _statusStyle);
            GUILayout.Label(
                Localization.Text("Camera") +
                    Localization.Text(_runtime.Active ? "FreeCamera" : "GameCamera") +
                "    " + Localization.Text("Speed") +
                    Localization.Text(_runtime.FastMode ? "Fast" : "Normal") +
                "    FOV: " + FormatFovState(),
                _labelStyle);
            GUILayout.Label(
                Localization.Text("FocusTarget") +
                    Localization.LocalizeFocusName(_runtime.FocusTargetName),
                _labelStyle);
            GUILayout.Label(_runtime.AutoFovStatusText, _statusStyle);
            GUILayout.Label(_runtime.CinematicModeStatusText, _statusStyle);

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
            GUILayout.Space(8f);
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
            GUILayout.Space(8f);
            if (GUILayout.Button(
                Localization.Text("ClearFocus"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.ClearFocusTarget();
            }
            GUI.enabled = previousGuiEnabled;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            if (GUILayout.Button(
                Localization.Text(_runtime.IsGameUiVisible() ? "HideGameUi" : "ShowGameUi"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.SetGameUiVisible(!_runtime.IsGameUiVisible(), true);
            }
            GUILayout.Space(8f);
            if (GUILayout.Button(
                Localization.Text("CloseMenu"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                _runtime.SetMenuVisible(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label(Localization.Text("BehaviorSettings"), _sectionStyle);
            _settings.Enabled.Value = GUILayout.Toggle(
                _settings.Enabled.Value,
                Localization.Text("EnableFreeCamera"),
                _toggleStyle);
            _runtime.SetCinematicModeEnabled(GUILayout.Toggle(
                _runtime.CinematicModeEnabled,
                Localization.Text("CinematicModeToggle"),
                _toggleStyle));
            GUILayout.Label(Localization.Text("CinematicModeHelp"), _statusStyle);
            _runtime.SetAutoFovEnabled(GUILayout.Toggle(
                _runtime.AutoFovEnabled,
                Localization.Text("AutoFovToggle"),
                _toggleStyle));
            GUILayout.Label(Localization.Text("AutoFovHelp"), _statusStyle);
            _settings.AutoHideUi.Value = GUILayout.Toggle(
                _settings.AutoHideUi.Value,
                Localization.Text("AutoHideUi"),
                _toggleStyle);
            _settings.InvertLookY.Value = GUILayout.Toggle(
                _settings.InvertLookY.Value,
                Localization.Text("InvertLookY"),
                _toggleStyle);
            _settings.ScaleLookSensitivityWithFov.Value = GUILayout.Toggle(
                _settings.ScaleLookSensitivityWithFov.Value,
                Localization.Text("ScaleLookWithFov"),
                _toggleStyle);
            _settings.ShowStatusMessages.Value = GUILayout.Toggle(
                _settings.ShowStatusMessages.Value,
                Localization.Text("ShowStatusMessages"),
                _toggleStyle);
            _settings.ShowQuickMenu.Value = GUILayout.Toggle(
                _settings.ShowQuickMenu.Value,
                Localization.Text("ShowQuickMenu"),
                _toggleStyle);

            GUILayout.Space(10f);
            GUILayout.Label(Localization.Text("NumericSettings"), _sectionStyle);
            DrawNumericRow(Localization.Text("NormalSpeed"), ref _normalSpeedInput);
            DrawNumericRow(Localization.Text("FastSpeed"), ref _fastSpeedInput);
            DrawNumericRow(Localization.Text("NormalAcceleration"), ref _normalAccelerationInput);
            DrawNumericRow(Localization.Text("FastAcceleration"), ref _fastAccelerationInput);
            GUILayout.Label(Localization.Text("MovementAccelerationHelp"), _statusStyle);
            DrawNumericRow(Localization.Text("MovementSmoothingTime"), ref _movementSmoothingTimeInput);
            DrawNumericRow(Localization.Text("LookSensitivity"), ref _lookSensitivityInput);
            DrawNumericRow(Localization.Text("LookSmoothingTime"), ref _lookSmoothingTimeInput);
            DrawNumericRow(Localization.Text("DragThreshold"), ref _dragThresholdInput);
            DrawNumericRow(Localization.Text("FovScrollSensitivity"), ref _fovScrollSensitivityInput);
            DrawNumericRow(Localization.Text("FovSmoothingTime"), ref _fovSmoothingTimeInput);
            DrawNumericRow(Localization.Text("MaximumFov"), ref _maximumFovInput);
            DrawNumericRow(Localization.Text("FocusMaximumDistance"), ref _focusMaximumDistanceInput);
            DrawNumericRow(Localization.Text("FocusSmoothingTime"), ref _focusSmoothingTimeInput);
            GUILayout.Label(
                Localization.Text("NumericHelp"),
                _statusStyle);

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            if (GUILayout.Button(
                Localization.Text("ApplyValues"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                ApplyNumericInputs();
            }
            GUILayout.Space(8f);
            if (GUILayout.Button(
                Localization.Text("ReloadValues"),
                _buttonStyle,
                GUILayout.Width(pairedButtonWidth)))
            {
                RefreshNumericInputs();
                _numericStatus = Localization.Text("ReloadedValues");
            }
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_numericStatus))
            {
                GUILayout.Label(Localization.LocalizeDynamic(_numericStatus), _statusStyle);
            }

            GUILayout.Space(10f);
            GUILayout.Label(Localization.Text("CurrentBindings"), _sectionStyle);
            GUILayout.Label(Localization.Text("BindingsHelp"), _statusStyle);
            DrawKeyBinding(Localization.Text("BindingToggleCamera"), _settings.ToggleCameraKey);
            DrawKeyBinding(Localization.Text("BindingToggleMenu"), _settings.ToggleMenuKey);
            DrawKeyBinding(Localization.Text("BindingToggleCinematicMode"), _settings.ToggleCinematicModeKey);
            DrawKeyBinding(Localization.Text("BindingMoveForward"), _settings.MoveForwardKey);
            DrawKeyBinding(Localization.Text("BindingMoveBackward"), _settings.MoveBackwardKey);
            DrawKeyBinding(Localization.Text("BindingMoveLeft"), _settings.MoveLeftKey);
            DrawKeyBinding(Localization.Text("BindingMoveRight"), _settings.MoveRightKey);
            DrawKeyBinding(Localization.Text("BindingMoveUp"), _settings.MoveUpKey);
            DrawKeyBinding(Localization.Text("BindingMoveDown"), _settings.MoveDownKey);
            DrawKeyBinding(Localization.Text("BindingToggleSpeed"), _settings.ToggleSpeedKey);
            DrawKeyBinding(Localization.Text("BindingLockSelf"), _settings.LockSelfKey);
            DrawKeyBinding(
                Localization.Text("BindingFocusSelectedTarget"),
                _settings.FocusSelectedTargetKey);
            DrawKeyBinding(Localization.Text("BindingToggleAutoFov"), _settings.ToggleAutoFovKey);

            string duplicateWarning = FindDuplicateKeyWarning();
            if (!string.IsNullOrEmpty(duplicateWarning))
            {
                GUILayout.Label(duplicateWarning, _statusStyle);
            }

            GUILayout.Space(10f);
            GUILayout.Label(Localization.Text("FixedMouseControls"), _sectionStyle);
            GUILayout.Label(Localization.Text("MouseLeft"), _labelStyle);
            GUILayout.Label(Localization.Text("MouseMiddle"), _labelStyle);
            GUILayout.Label(Localization.Text("MouseWheel"), _labelStyle);
            GUILayout.Label(Localization.Text("MouseRight"), _labelStyle);
            GUILayout.Label(
                Localization.Format(
                    "HideUiHelp",
                    _runtime.GetGameUiToggleBindingText(),
                    _settings.ToggleMenuKey.Value),
                _labelStyle);
            GUILayout.Label(
                Localization.Text("InputHelp"),
                _statusStyle);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, WindowRect.width, 24f));
        }

        private void DrawLanguageSelector()
        {
            float labelWidth;
            float valueWidth;
            GetColumnWidths(out labelWidth, out valueWidth);

            GUILayout.Label(Localization.Text("Language"), _sectionStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                Localization.Text("InterfaceLanguage"),
                _labelStyle,
                GUILayout.Width(labelWidth));
            if (GUILayout.Button(
                Localization.CurrentLanguageName + "  v",
                _buttonStyle,
                GUILayout.Width(valueWidth)))
            {
                _languageDropdownOpen = !_languageDropdownOpen;
            }
            GUILayout.EndHorizontal();

            if (!_languageDropdownOpen)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(labelWidth);
            GUILayout.BeginVertical(GUILayout.Width(valueWidth));
            if (GUILayout.Button("English", _buttonStyle))
            {
                Localization.SelectLanguage(0);
                _numericStatus = Localization.Text("LanguageChanged");
                _languageDropdownOpen = false;
            }
            if (GUILayout.Button("简体中文", _buttonStyle))
            {
                Localization.SelectLanguage(1);
                _numericStatus = Localization.Text("LanguageChanged");
                _languageDropdownOpen = false;
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawNumericRow(string label, ref string value)
        {
            float labelWidth;
            float valueWidth;
            GetColumnWidths(out labelWidth, out valueWidth);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(labelWidth));
            value = GUILayout.TextField(
                value ?? string.Empty,
                _textFieldStyle,
                GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void DrawKeyBinding(string label, ConfigEntry<KeyCode> entry)
        {
            float labelWidth;
            float valueWidth;
            GetColumnWidths(out labelWidth, out valueWidth);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(labelWidth));
            GUILayout.Label(entry.Value.ToString(), _labelStyle, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void GetColumnWidths(out float labelWidth, out float valueWidth)
        {
            // Reserve room for the window padding, scrollbar and GUILayout spacing so
            // the complete settings menu remains usable at its 320-pixel minimum width.
            float contentWidth = GetContentWidth();
            valueWidth = Mathf.Clamp(contentWidth * 0.36f, 64f, 175f);
            valueWidth = Mathf.Min(valueWidth, Mathf.Max(1f, contentWidth * 0.48f));
            labelWidth = Mathf.Max(1f, contentWidth - valueWidth - 8f);
        }

        private float GetContentWidth()
        {
            return Mathf.Max(1f, WindowRect.width - 52f);
        }

        private float GetPairedButtonWidth()
        {
            return Mathf.Max(1f, (GetContentWidth() - 8f) * 0.5f);
        }

        private void ApplyNumericInputs()
        {
            float normalSpeed;
            float fastSpeed;
            float normalAcceleration;
            float fastAcceleration;
            float movementSmoothingTime;
            float lookSensitivity;
            float lookSmoothingTime;
            float dragThreshold;
            float fovScrollSensitivity;
            float fovSmoothingTime;
            float maximumFov;
            float focusMaximumDistance;
            float focusSmoothingTime;

            if (!TryParseFloat(_normalSpeedInput, out normalSpeed) ||
                !TryParseFloat(_fastSpeedInput, out fastSpeed) ||
                !TryParseFloat(_normalAccelerationInput, out normalAcceleration) ||
                !TryParseFloat(_fastAccelerationInput, out fastAcceleration) ||
                !TryParseFloat(_movementSmoothingTimeInput, out movementSmoothingTime) ||
                !TryParseFloat(_lookSensitivityInput, out lookSensitivity) ||
                !TryParseFloat(_lookSmoothingTimeInput, out lookSmoothingTime) ||
                !TryParseFloat(_dragThresholdInput, out dragThreshold) ||
                !TryParseFloat(_fovScrollSensitivityInput, out fovScrollSensitivity) ||
                !TryParseFloat(_fovSmoothingTimeInput, out fovSmoothingTime) ||
                !TryParseFloat(_maximumFovInput, out maximumFov) ||
                !TryParseFloat(_focusMaximumDistanceInput, out focusMaximumDistance) ||
                !TryParseFloat(_focusSmoothingTimeInput, out focusSmoothingTime))
            {
                _numericStatus = Localization.Text("InvalidNumericValues");
                return;
            }

            _settings.NormalSpeed.Value = Mathf.Clamp(normalSpeed, 0.1f, 100000f);
            _settings.FastSpeed.Value = Mathf.Clamp(fastSpeed, 0.1f, 100000f);
            _settings.NormalAcceleration.Value = Mathf.Clamp(
                normalAcceleration, 0f, Plugin.MaximumMovementAcceleration);
            _settings.FastAcceleration.Value = Mathf.Clamp(
                fastAcceleration, 0f, Plugin.MaximumMovementAcceleration);
            _settings.MovementSmoothingTime.Value = Mathf.Clamp(movementSmoothingTime, 0f, 2f);
            _settings.LookSensitivity.Value = Mathf.Clamp(lookSensitivity, 0.001f, 10f);
            _settings.LookSmoothingTime.Value = Mathf.Clamp(lookSmoothingTime, 0f, 1f);
            _settings.DragThresholdPixels.Value = Mathf.Clamp(dragThreshold, 0f, 50f);
            _settings.FovScrollSensitivity.Value = Mathf.Clamp(fovScrollSensitivity, 0.01f, 10f);
            _settings.FovSmoothingTime.Value = Mathf.Clamp(fovSmoothingTime, 0f, 2f);
            _settings.MaximumFov.Value = Mathf.Clamp(maximumFov, FreeCameraController.MinimumFov, 179f);
            _settings.FocusMaximumDistance.Value = Mathf.Clamp(focusMaximumDistance, 10f, 1000000f);
            _settings.FocusSmoothingTime.Value = Mathf.Clamp(focusSmoothingTime, 0f, 0.5f);
            RefreshNumericInputs();
            _numericStatus = Localization.Text("AppliedValues");
        }

        private void RefreshNumericInputs()
        {
            _normalSpeedInput = FormatNumber(_settings.NormalSpeed.Value);
            _fastSpeedInput = FormatNumber(_settings.FastSpeed.Value);
            _normalAccelerationInput = _settings.NormalAcceleration.Value.ToString(
                "G", CultureInfo.InvariantCulture);
            _fastAccelerationInput = _settings.FastAcceleration.Value.ToString(
                "G", CultureInfo.InvariantCulture);
            _movementSmoothingTimeInput = FormatNumber(_settings.MovementSmoothingTime.Value);
            _lookSensitivityInput = FormatNumber(_settings.LookSensitivity.Value);
            _lookSmoothingTimeInput = FormatNumber(_settings.LookSmoothingTime.Value);
            _dragThresholdInput = FormatNumber(_settings.DragThresholdPixels.Value);
            _fovScrollSensitivityInput = FormatNumber(_settings.FovScrollSensitivity.Value);
            _fovSmoothingTimeInput = FormatNumber(_settings.FovSmoothingTime.Value);
            _maximumFovInput = FormatNumber(_settings.MaximumFov.Value);
            _focusMaximumDistanceInput = FormatNumber(_settings.FocusMaximumDistance.Value);
            _focusSmoothingTimeInput = FormatNumber(_settings.FocusSmoothingTime.Value);
        }

        private string FindDuplicateKeyWarning()
        {
            ConfigEntry<KeyCode>[] entries = GetKeyEntries();
            Dictionary<KeyCode, int> counts = new Dictionary<KeyCode, int>();
            for (int i = 0; i < entries.Length; i++)
            {
                KeyCode key = entries[i].Value;
                int count;
                counts.TryGetValue(key, out count);
                counts[key] = count + 1;
            }

            foreach (KeyValuePair<KeyCode, int> pair in counts)
            {
                if (pair.Key != KeyCode.None && pair.Value > 1)
                {
                    return Localization.Format("DuplicateKeyWarning", pair.Key);
                }
            }

            return null;
        }

        private ConfigEntry<KeyCode>[] GetKeyEntries()
        {
            return new[]
            {
                _settings.ToggleCameraKey,
                _settings.ToggleMenuKey,
                _settings.ToggleCinematicModeKey,
                _settings.MoveForwardKey,
                _settings.MoveBackwardKey,
                _settings.MoveLeftKey,
                _settings.MoveRightKey,
                _settings.MoveUpKey,
                _settings.MoveDownKey,
                _settings.ToggleSpeedKey,
                _settings.LockSelfKey,
                _settings.FocusSelectedTargetKey,
                _settings.ToggleAutoFovKey
            };
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
            {
                return;
            }

            FreeCameraGuiTheme.EnsureStyles();
            _windowStyle = FreeCameraGuiTheme.WindowStyle;
            _sectionStyle = FreeCameraGuiTheme.SectionHeaderStyle;
            _labelStyle = FreeCameraGuiTheme.LabelStyle;
            _statusStyle = FreeCameraGuiTheme.StatusStyle;
            _buttonStyle = FreeCameraGuiTheme.ButtonStyle;
            _textFieldStyle = FreeCameraGuiTheme.TextFieldStyle;
            _toggleStyle = FreeCameraGuiTheme.ToggleStyle;
        }

        private void ClampWindowToScreen()
        {
            float width = Mathf.Max(1f, Mathf.Min(WindowWidth, Screen.width - 10f));
            float height = Mathf.Max(1f, Mathf.Min(WindowHeight, Screen.height - 10f));
            Rect rect = WindowRect;
            rect.width = width;
            rect.height = height;
            rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, Screen.width - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Screen.height - rect.height));
            WindowRect = rect;
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
            if (pointerEvent && WindowRect.Contains(current.mousePosition))
            {
                current.Use();
            }
        }

        private static bool TryParseFloat(string text, out float value)
        {
            bool parsed = float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value) || float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out value);
            return parsed && NumericUtility.IsFinite(value);
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatFov(float value)
        {
            return value > 0f ? value.ToString("0.###", CultureInfo.InvariantCulture) + "°" : "--";
        }

        private string FormatFovState()
        {
            float current = _runtime.CurrentFov;
            float target = _runtime.TargetFov;
            if (_runtime.Active && Mathf.Abs(current - target) > 0.001f)
            {
                return FormatFov(current) + " → " + FormatFov(target);
            }

            return FormatFov(current);
        }
    }
}
