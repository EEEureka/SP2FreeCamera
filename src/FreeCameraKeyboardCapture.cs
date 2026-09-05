using System;
using System.Collections.Generic;
using Assets.Scripts.Input;
using BepInEx.Logging;
using Rewired;
using UnityEngine;

namespace SP2FreeCamera
{
    /// <summary>
    /// Temporarily owns unmodified camera keys (movement and the auto-FOV toggle)
    /// that can also control the local craft or character. Controller maps
    /// themselves are never disabled.
    /// </summary>
    internal sealed class FreeCameraKeyboardCapture : IDisposable
    {
        private const string CraftCategoryName = "Craft";
        private const string CharacterCategoryName = "Character";
        private const float RefreshIntervalSeconds = 1f;

        private readonly ManualLogSource _logger;
        private readonly List<CapturedBinding> _capturedBindings = new List<CapturedBinding>(16);
        private readonly List<ControllerMap> _controllerMapBuffer = new List<ControllerMap>(8);
        private readonly List<ActionElementMap> _elementMapBuffer = new List<ActionElementMap>(32);
        private readonly HashSet<KeyCode> _movementKeys = new HashSet<KeyCode>();

        private bool _captureActive;
        private bool _isReconciling;
        private bool _controlsChangedSubscribed;
        private bool _initializedSubscribed;
        private bool _preShutDownSubscribed;
        private bool _captureWarningLogged;
        private float _nextRefreshTime;

        internal FreeCameraKeyboardCapture(ManualLogSource logger = null)
        {
            _logger = logger;
        }

        internal bool IsCaptured
        {
            get { return _captureActive; }
        }

        /// <summary>
        /// Starts (or refreshes) keyboard capture. Calling this repeatedly is safe.
        /// The game's controls-changed callback keeps replaced maps captured while
        /// free camera mode remains active.
        /// </summary>
        internal void Capture(params KeyCode[] movementKeys)
        {
            _movementKeys.Clear();
            if (movementKeys != null)
            {
                for (int i = 0; i < movementKeys.Length; i++)
                {
                    if (movementKeys[i] != KeyCode.None)
                    {
                        _movementKeys.Add(movementKeys[i]);
                    }
                }
            }

            _captureActive = true;
            EnsureEventSubscriptions();
            ReconcileCurrentBindings();
            ScheduleRefresh();
        }

        /// <summary>
        /// Cheap periodic reconciliation hook for callers with a regular update
        /// path. The map scan itself runs at most once per second.
        /// </summary>
        internal void Refresh()
        {
            if (!_captureActive)
            {
                return;
            }

            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            EnsureEventSubscriptions();
            ReconcileCurrentBindings();
            ScheduleRefresh();
        }

        /// <summary>
        /// Restores every captured element's exact pre-capture enabled state.
        /// Restoration is best-effort per element so one stale Rewired object cannot
        /// prevent the remaining bindings from being restored.
        /// </summary>
        internal void Restore()
        {
            if (!_captureActive &&
                _capturedBindings.Count == 0 &&
                !_controlsChangedSubscribed &&
                !_initializedSubscribed &&
                !_preShutDownSubscribed)
            {
                return;
            }

            _captureActive = false;
            RemoveEventSubscriptions();

            Exception firstFailure = null;
            for (int i = _capturedBindings.Count - 1; i >= 0; i--)
            {
                CapturedBinding captured = _capturedBindings[i];
                try
                {
                    // ActionElementMap.enabled is the only Rewired state changed by
                    // this class. Keeping the direct object reference also makes the
                    // restore independent of controller-map replacement or reordering.
                    captured.ElementMap.enabled = captured.WasEnabled;
                }
                catch (Exception ex)
                {
                    if (firstFailure == null)
                    {
                        firstFailure = ex;
                    }
                }
            }

            _capturedBindings.Clear();
            _controllerMapBuffer.Clear();
            _elementMapBuffer.Clear();
            _movementKeys.Clear();
            _captureWarningLogged = false;
            _nextRefreshTime = 0f;

            if (firstFailure != null)
            {
                LogWarning("One or more free-camera keyboard bindings could not be restored.", firstFailure);
            }
        }

        public void Dispose()
        {
            Restore();
        }

        private void EnsureEventSubscriptions()
        {
            try
            {
                if (!_controlsChangedSubscribed)
                {
                    InputWrapper.OnControlsChanged += OnControlsChanged;
                    _controlsChangedSubscribed = true;
                }

                if (!_initializedSubscribed)
                {
                    ReInput.InitializedEvent += OnRewiredInitialized;
                    _initializedSubscribed = true;
                }

                if (!_preShutDownSubscribed)
                {
                    ReInput.PreShutDownEvent += OnRewiredPreShutDown;
                    _preShutDownSubscribed = true;
                }
            }
            catch (Exception ex)
            {
                LogCaptureWarning("Free-camera keyboard capture could not subscribe to Rewired events.", ex);
            }
        }

        private void RemoveEventSubscriptions()
        {
            if (_controlsChangedSubscribed)
            {
                try
                {
                    InputWrapper.OnControlsChanged -= OnControlsChanged;
                }
                catch (Exception ex)
                {
                    LogWarning("Could not unsubscribe the free-camera controls-changed callback.", ex);
                }
                finally
                {
                    _controlsChangedSubscribed = false;
                }
            }

            if (_initializedSubscribed)
            {
                try
                {
                    ReInput.InitializedEvent -= OnRewiredInitialized;
                }
                catch (Exception ex)
                {
                    LogWarning("Could not unsubscribe the free-camera Rewired initialization callback.", ex);
                }
                finally
                {
                    _initializedSubscribed = false;
                }
            }

            if (_preShutDownSubscribed)
            {
                try
                {
                    ReInput.PreShutDownEvent -= OnRewiredPreShutDown;
                }
                catch (Exception ex)
                {
                    LogWarning("Could not unsubscribe the free-camera Rewired shutdown callback.", ex);
                }
                finally
                {
                    _preShutDownSubscribed = false;
                }
            }
        }

        private void OnControlsChanged(object sender, EventArgs eventArgs)
        {
            if (_captureActive)
            {
                ReconcileCurrentBindings();
                ScheduleRefresh();
            }
        }

        private void OnRewiredInitialized()
        {
            if (_captureActive)
            {
                ReconcileCurrentBindings();
                ScheduleRefresh();
            }
        }

        private void OnRewiredPreShutDown()
        {
            // Restore while the current Rewired objects are still alive. Restore is
            // exception-contained and safe to call again from plugin shutdown.
            Restore();
        }

        private void ReconcileCurrentBindings()
        {
            if (!_captureActive || _isReconciling || !ReInput.isReady)
            {
                return;
            }

            _isReconciling = true;
            try
            {
                Player player = ReInput.players.GetPlayer(0);
                if (player == null)
                {
                    return;
                }

                CaptureCategory(player, CraftCategoryName);
                CaptureCategory(player, CharacterCategoryName);
                _captureWarningLogged = false;
            }
            catch (Exception ex)
            {
                // A controller-map collection can be replaced while it is being
                // inspected. The next Rewired update retries without losing the
                // snapshots already taken in this pass.
                LogCaptureWarning("Free-camera keyboard bindings could not be fully captured; retrying shortly.", ex);
            }
            finally
            {
                _controllerMapBuffer.Clear();
                _elementMapBuffer.Clear();
                _isReconciling = false;
            }
        }

        private void CaptureCategory(Player player, string categoryName)
        {
            _controllerMapBuffer.Clear();

            int categoryId = ReInput.mapping.GetMapCategoryId(categoryName);
            if (categoryId < 0)
            {
                return;
            }

            player.controllers.maps.GetAllMapsInCategory(
                categoryId,
                ControllerType.Keyboard,
                _controllerMapBuffer);

            for (int mapIndex = 0; mapIndex < _controllerMapBuffer.Count; mapIndex++)
            {
                ControllerMap controllerMap = _controllerMapBuffer[mapIndex];
                if (controllerMap == null || controllerMap.controllerType != ControllerType.Keyboard)
                {
                    continue;
                }

                // Do not trust only the query overload: retain the canonical category
                // check so compatibility shims cannot broaden capture to another map.
                InputMapCategory category = ReInput.mapping.GetMapCategory(controllerMap.categoryId);
                if (category == null || !string.Equals(category.name, categoryName, StringComparison.Ordinal))
                {
                    continue;
                }

                _elementMapBuffer.Clear();
                controllerMap.GetElementMaps(false, _elementMapBuffer);

                for (int elementIndex = 0; elementIndex < _elementMapBuffer.Count; elementIndex++)
                {
                    ActionElementMap elementMap = _elementMapBuffer[elementIndex];
                    if (!IsCapturedKey(elementMap))
                    {
                        continue;
                    }

                    CapturedBinding captured = FindCapturedBinding(elementMap);
                    if (captured == null)
                    {
                        captured = new CapturedBinding(elementMap, elementMap.enabled);
                        _capturedBindings.Add(captured);
                    }

                    // Re-assert the capture on every reconciliation. This covers maps
                    // re-enabled by the game without changing their object identity.
                    if (elementMap.enabled)
                    {
                        elementMap.enabled = false;
                    }
                }
            }
        }

        private CapturedBinding FindCapturedBinding(ActionElementMap elementMap)
        {
            for (int i = 0; i < _capturedBindings.Count; i++)
            {
                if (ReferenceEquals(_capturedBindings[i].ElementMap, elementMap))
                {
                    return _capturedBindings[i];
                }
            }

            return null;
        }

        private bool IsCapturedKey(ActionElementMap elementMap)
        {
            if (elementMap == null || elementMap.hasModifiers)
            {
                return false;
            }

            return _movementKeys.Contains(elementMap.keyCode);
        }

        private void LogCaptureWarning(string message, Exception exception)
        {
            if (_captureWarningLogged)
            {
                return;
            }

            _captureWarningLogged = true;
            LogWarning(message, exception);
        }

        private void ScheduleRefresh()
        {
            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
        }

        private void LogWarning(string message, Exception exception)
        {
            if (_logger == null)
            {
                return;
            }

            try
            {
                _logger.LogWarning(
                    LogPrivacy.Sanitize(message) + " " + LogPrivacy.ExceptionSummary(exception));
            }
            catch
            {
                // Logging must never interfere with input restoration.
            }
        }

        private sealed class CapturedBinding
        {
            internal CapturedBinding(ActionElementMap elementMap, bool wasEnabled)
            {
                ElementMap = elementMap;
                WasEnabled = wasEnabled;
            }

            internal ActionElementMap ElementMap { get; private set; }

            internal bool WasEnabled { get; private set; }
        }
    }
}
