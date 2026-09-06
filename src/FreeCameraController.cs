using System;
using Assets.Scripts;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Combat;
using Assets.Scripts.Flight.Cameras;
using Assets.Scripts.Flight.WorldObjects.Vehicles.Land;
using Assets.Scripts.Flight.WorldObjects.Vehicles.Sea;
using Assets.Scripts.Multiplayer.ActivityFramework.Activities.MechInvasion;
using UnityEngine;

namespace SP2FreeCamera
{
    internal sealed class FreeCameraController : CameraController
    {
        internal const float MinimumFov = FovMath.MinimumFov;

        private const float LostTargetGraceSeconds = 0.2f;
        private const float TargetJumpResetDistance = 100f;
        private const float ReleasedWeaponEdgeTolerancePixels = 8f;
        private const float MaximumFallbackPredictionSeconds = 0.12f;
        private const float MaximumTrackingDeltaTime = 0.05f;
        private const float TrackingResetDeltaTime = 0.25f;
        private const float FocusSoftErrorPixels = 8f;
        private const float FocusHardErrorPixels = 32f;
        private const float ProjectionEpsilon = 0.000001f;
        private const double SampleTimeEpsilon = 0.000001;
        private const double SamplePositionEpsilonSquared = 0.00000001;
        private const float SampleVelocityEpsilonSquared = 0.000001f;

        private readonly FreeCameraRuntime _runtime;
        private readonly AutoFovState _autoFov = new AutoFovState();

        private FocusTarget _focusTarget;
        private Vector3d _lastFocusGlobalPosition;
        private bool _hasLastFocusGlobalPosition;
        private Vector3d _focusSampleGlobalPosition;
        private Vector3 _focusSampleVelocity;
        private bool _focusSampleHasDirectVelocity;
        private bool _focusSamplePredictsFromFixedTime;
        private double _focusSampleTime;
        private Vector3d _focusCorrection;
        private Vector3d _renderedFocusGlobalPosition;
        private bool _hasFocusTrackingState;
        private int _focusMotionSourceId;
        private bool _focusTrackingSuspended;
        private int _lastCameraRenderFrame = -1;
        private float _lostTargetElapsed;
        private float _yaw;
        private float _pitch;
        private float _roll;
        private float _targetYaw;
        private float _targetPitch;
        private float _targetRoll;
        private bool _lookSmoothingActive;
        private Vector3 _smoothedMoveVelocity;
        private float _currentFov;
        private float _targetFov;
        private float _pendingFovScroll;
        private bool _customProjectionActive;
        private bool _leftPressAccepted;
        private bool _leftDragging;
        private Vector2 _leftPressPosition;
        private Vector2 _lastMousePosition;

        internal FreeCameraController(
            CameraManagerScript cameraManager,
            FreeCameraRuntime runtime,
            float initialFov)
            : base(cameraManager)
        {
            _runtime = runtime;
            _autoFov.SetEnabled(runtime.AutoFovEnabled);
            _currentFov = initialFov;
            _targetFov = initialFov;
            Name = Localization.Text("FreeCamera");
            SyncLookAnglesFromCamera();
        }

        internal bool HasFocusTarget
        {
            get { return _focusTarget != null; }
        }

        internal string FocusTargetName
        {
            get { return _focusTarget != null ? _focusTarget.DisplayName : "无"; }
        }

        internal float CurrentFov
        {
            get { return _currentFov; }
        }

        internal float TargetFov
        {
            get { return _targetFov; }
        }

        internal bool AutoFovActive
        {
            get { return _autoFov.Active; }
        }

        internal double AutoFovAreaRatio
        {
            get { return _autoFov.AreaRatio; }
        }

        internal void SetAutoFovEnabled(bool enabled)
        {
            if (_autoFov.Enabled == enabled)
            {
                return;
            }

            // Never return to an old manual zoom value on handoff. Toggling an
            // armed mode without a target must not interrupt manual zoom either.
            if (_autoFov.Active || (enabled && _focusTarget != null))
            {
                _targetFov = _currentFov;
            }
            _autoFov.SetEnabled(enabled);
        }

        private void ResetAutoFovReference()
        {
            if (_autoFov.Enabled)
            {
                _targetFov = _currentFov;
            }
            _autoFov.ResetTarget();
        }

        internal void ProcessFrame(float unscaledDeltaTime)
        {
            if (!IsSelected || CameraManager == null || CameraTransform == null)
            {
                ResetInputSmoothing();
                return;
            }

            bool allowKeyboardInput = _runtime.CanProcessKeyboardInput();
            bool allowPointerInput = _runtime.CanProcessPointerInput();
            string localizedName = Localization.Text("FreeCamera");
            if (!string.Equals(Name, localizedName, StringComparison.Ordinal))
            {
                Name = localizedName;
            }

            ProcessMovement(unscaledDeltaTime, allowKeyboardInput);
            ProcessFocusSelectionInput();

            if (allowPointerInput)
            {
                ProcessPointerInput();
            }
            else
            {
                ResetPointerState();
                CancelLookSmoothing();
            }

            UpdateSmoothedLook(unscaledDeltaTime);
            UpdateUnlockedFocalPosition();
        }

        internal void SetFocusTarget(FocusTarget focusTarget)
        {
            Vector3 focusPosition;
            if (focusTarget == null ||
                !focusTarget.TryGetFloatingOriginPosition(out focusPosition) ||
                !IsFinite(focusPosition))
            {
                _runtime.Notify("目标当前不可用。", true);
                return;
            }

            Vector3d focusGlobalPosition = ToGlobalPosition(focusPosition);
            if (!IsFinite(focusGlobalPosition))
            {
                _runtime.Notify("目标当前不可用。", true);
                return;
            }

            _focusTarget = focusTarget;
            ResetAutoFovReference();
            _lastFocusGlobalPosition = focusGlobalPosition;
            _hasLastFocusGlobalPosition = true;
            ResetFocusTrackingState();
            _lostTargetElapsed = 0f;
            // Acquiring a focus target must not interrupt an ongoing dolly move.
            // Position velocity continues through the same acceleration path.
            CancelLookSmoothing();
            _runtime.Notify(
                "已锁定 " + focusTarget.DisplayName + "。",
                false,
                Localization.Text("FocusLockedLog"));
        }

        internal void ClearFocusTarget(bool notify)
        {
            if (_focusTarget == null)
            {
                return;
            }

            _focusTarget = null;
            ResetAutoFovReference();
            _hasLastFocusGlobalPosition = false;
            ResetFocusTrackingState();
            _lostTargetElapsed = 0f;
            SyncLookAnglesFromCamera();
            if (notify)
            {
                _runtime.Notify("已解除目标锁定。", false);
            }
        }

        private void ProcessFocusSelectionInput()
        {
            if (!Input.GetMouseButtonDown(2) || !_runtime.CanProcessFocusSelectionInput())
            {
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            // Middle-click focus selection intentionally bypasses the game's general
            // UIInfo.IsInteracting gate. HUD target boxes can cover the rendered part,
            // while dialogs, text entry and this plugin's UI remain protected by the
            // dedicated focus-selection gate.
            TrySelectFocusTarget(mousePosition);
        }

        internal void ResetPointerState()
        {
            _leftPressAccepted = false;
            _leftDragging = false;
        }

        internal void ResetInputSmoothing()
        {
            _smoothedMoveVelocity = Vector3.zero;
            _pendingFovScroll = 0f;
            ResetPointerState();
            CancelLookSmoothing();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            IsActive = true;
            _lastCameraRenderFrame = -1;
            ResetAutoFovReference();
            _smoothedMoveVelocity = Vector3.zero;
            ResetFocusTrackingState();
            SyncLookAnglesFromCamera(resetRoll: true);
            SetAppliedFov(_currentFov);
            UpdateCursor();
        }

        public override void OnDeselected()
        {
            ResetInputSmoothing();
            ResetFocusTrackingState();
            ResetCustomProjection();
            base.OnDeselected();
        }

        public override void OnDestroy()
        {
            ResetCustomProjection();
            _focusTarget = null;
            _hasLastFocusGlobalPosition = false;
            ResetFocusTrackingState();
            ResetInputSmoothing();
            base.OnDestroy();
        }

        public override void UpdateCursor()
        {
            if (IsSelected)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public override void Update(int frameCount)
        {
            if (!IsSelected)
            {
                return;
            }

            UpdateUnlockedFocalPosition();
        }

        public override void LateUpdate()
        {
            MaintainCustomProjection();

            if (!IsSelected || CameraManager == null || CameraTransform == null)
            {
                return;
            }

            int frameCount = Time.frameCount;
            if (_lastCameraRenderFrame == frameCount)
            {
                UpdateFloatingOriginFocus();
                return;
            }
            _lastCameraRenderFrame = frameCount;

            Vector3d focusGlobalPosition;
            bool hasFreshFocus = UpdateFocusForRender(out focusGlobalPosition);
            // Rotation and zoom share the SAME fresh pose after camera movement.
            // The game also updates cameras on physics ticks: smooth FOV only
            // here, once per rendered frame, never in Update(int frameCount).
            UpdateFrameFov(Time.unscaledDeltaTime, hasFreshFocus, focusGlobalPosition);
            MaintainCustomProjection();
        }

        private bool UpdateFocusForRender(out Vector3d resolvedGlobalPosition)
        {
            resolvedGlobalPosition = Vector3d.zero;
            if (_focusTarget == null)
            {
                return false;
            }

            Vector3 focusPosition;
            bool resolved = _focusTarget.TryGetFloatingOriginPosition(out focusPosition);
            if (resolved)
            {
                Vector3d focusGlobalPosition = ToGlobalPosition(focusPosition);
                if (!IsFinite(focusGlobalPosition))
                {
                    resolved = false;
                }
                else
                {
                    _lastFocusGlobalPosition = focusGlobalPosition;
                    resolvedGlobalPosition = focusGlobalPosition;
                    _hasLastFocusGlobalPosition = true;
                    _lostTargetElapsed = 0f;

                    if (_focusTarget.Kind == FocusTargetKind.Terrain)
                    {
                        TrackTerrainPosition(focusPosition, false);
                    }
                    else
                    {
                        // Moving targets use their latest rendered-frame pose directly.
                        // Focus smoothing remains available only for fixed terrain points.
                        TrackImmediateDynamicPosition(focusGlobalPosition);
                    }
                    return true;
                }
            }

            bool suspendLostTimeout = PauseManager.Paused || !Application.isFocused;
            float lostDeltaTime = suspendLostTimeout ? 0f : Time.unscaledDeltaTime;
            if (!NumericUtility.IsFinite(lostDeltaTime) || lostDeltaTime < 0f)
            {
                lostDeltaTime = LostTargetGraceSeconds + 1f;
            }
            _lostTargetElapsed += lostDeltaTime;
            _focusTrackingSuspended = true;
            if (_hasLastFocusGlobalPosition && _lostTargetElapsed <= LostTargetGraceSeconds)
            {
                Vector3d frozenFocus = _hasFocusTrackingState
                    ? _renderedFocusGlobalPosition
                    : _lastFocusGlobalPosition;
                TrackImmediateGlobalPosition(frozenFocus);
                return false;
            }

            string lostName = _focusTarget.DisplayName;
            _focusTarget = null;
            ResetAutoFovReference();
            _hasLastFocusGlobalPosition = false;
            ResetFocusTrackingState();
            _lostTargetElapsed = 0f;
            SyncLookAnglesFromCamera();
            _runtime.Notify(
                "目标已丢失: " + lostName,
                true,
                Localization.Text("FocusTargetLostLog"));
            return false;
        }

        private void ProcessMovement(float unscaledDeltaTime, bool allowInput)
        {
            if (!allowInput)
            {
                _smoothedMoveVelocity = Vector3.zero;
                return;
            }

            if (!NumericUtility.IsFinite(unscaledDeltaTime) ||
                unscaledDeltaTime <= 0f || unscaledDeltaTime > 0.25f)
            {
                _smoothedMoveVelocity = Vector3.zero;
                return;
            }

            Plugin settings = _runtime.Settings;
            Vector3 direction = Vector3.zero;

            if (Input.GetKey(settings.MoveForwardKey.Value))
            {
                direction += CameraTransform.forward;
            }
            if (Input.GetKey(settings.MoveBackwardKey.Value))
            {
                direction -= CameraTransform.forward;
            }
            if (Input.GetKey(settings.MoveRightKey.Value))
            {
                direction += CameraTransform.right;
            }
            if (Input.GetKey(settings.MoveLeftKey.Value))
            {
                direction -= CameraTransform.right;
            }
            if (Input.GetKey(settings.MoveUpKey.Value))
            {
                direction += CameraTransform.up;
            }
            if (Input.GetKey(settings.MoveDownKey.Value))
            {
                direction -= CameraTransform.up;
            }

            Vector3 quickMovement = _runtime.QuickMovementAxes;
            if (quickMovement.sqrMagnitude > 0.0001f)
            {
                direction += CameraTransform.forward * quickMovement.z;
                direction += CameraTransform.right * quickMovement.x;
                direction += CameraTransform.up * quickMovement.y;
            }

            if (Input.GetKeyDown(settings.ToggleSpeedKey.Value))
            {
                _runtime.ToggleFastMode();
            }

            bool fastMode = _runtime.FastMode;
            Vector3 targetVelocity = Vector3.zero;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                float speed = fastMode
                    ? NumericUtility.ClampFinite(
                        settings.FastSpeed.Value,
                        Plugin.DefaultFastSpeed,
                        0.1f,
                        100000f)
                    : NumericUtility.ClampFinite(
                        settings.NormalSpeed.Value,
                        Plugin.DefaultNormalSpeed,
                        0.1f,
                        100000f);
                targetVelocity = direction * speed;
            }

            if (!IsFinite(targetVelocity) || !IsFinite(_smoothedMoveVelocity))
            {
                _smoothedMoveVelocity = Vector3.zero;
                return;
            }

            if (targetVelocity.sqrMagnitude <= 0.000001f &&
                _smoothedMoveVelocity.sqrMagnitude <= 0.000001f)
            {
                _smoothedMoveVelocity = Vector3.zero;
                return;
            }

            float smoothingTime = NumericUtility.ClampFinite(
                settings.MovementSmoothingTime.Value,
                0.08f,
                0f,
                2f);
            // Use the active mode for both acceleration and braking. Switching
            // modes preserves world velocity and only changes the requested rate.
            float acceleration = fastMode
                ? NumericUtility.ClampFinite(
                    settings.FastAcceleration.Value,
                    Plugin.DefaultFastAcceleration,
                    0f,
                    Plugin.MaximumMovementAcceleration)
                : NumericUtility.ClampFinite(
                    settings.NormalAcceleration.Value,
                    Plugin.DefaultNormalAcceleration,
                    0f,
                    Plugin.MaximumMovementAcceleration);
            Vector3 velocityDelta = targetVelocity - _smoothedMoveVelocity;
            double velocityBlend;
            double displacementBlendTime;
            MovementIntegrator.Calculate(
                velocityDelta.magnitude,
                acceleration,
                smoothingTime,
                unscaledDeltaTime,
                out velocityBlend,
                out displacementBlendTime);
            Vector3 displacement =
                _smoothedMoveVelocity * unscaledDeltaTime +
                velocityDelta * (float)displacementBlendTime;
            _smoothedMoveVelocity += velocityDelta * (float)velocityBlend;

            if (targetVelocity.sqrMagnitude <= 0.000001f &&
                _smoothedMoveVelocity.sqrMagnitude <= 0.000001f)
            {
                _smoothedMoveVelocity = Vector3.zero;
            }

            if (IsFinite(displacement))
            {
                ApplyCameraDisplacement(displacement);
            }
            else
            {
                _smoothedMoveVelocity = Vector3.zero;
            }
        }

        private void ProcessPointerInput()
        {
            Vector2 mousePosition = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                _leftPressAccepted = true;
                _leftDragging = false;
                _leftPressPosition = mousePosition;
                _lastMousePosition = mousePosition;
            }

            if (_leftPressAccepted && Input.GetMouseButton(0))
            {
                Vector2 delta = mousePosition - _lastMousePosition;
                float dragDistance = Vector2.Distance(_leftPressPosition, mousePosition);
                float threshold = NumericUtility.ClampFinite(
                    _runtime.Settings.DragThresholdPixels.Value,
                    4f,
                    0f,
                    50f);
                if (!_leftDragging && dragDistance >= threshold)
                {
                    _leftDragging = true;
                    ClearFocusTarget(false);
                    SyncLookAnglesFromCamera();
                }

                if (_leftDragging && delta.sqrMagnitude > 0f)
                {
                    ApplyLookDelta(delta);
                }

                _lastMousePosition = mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                ResetPointerState();
            }

            float scroll = Input.mouseScrollDelta.y;
            if (NumericUtility.IsFinite(scroll) && Mathf.Abs(scroll) > 0.0001f)
            {
                // Resolve manual versus auto zoom only after this frame's focus
                // sample is known. This also handles acquire/clear + scroll in
                // the same frame without consuming wheel input twice.
                _pendingFovScroll += scroll;
            }
        }

        private void ApplyLookDelta(Vector2 delta)
        {
            float sensitivity = NumericUtility.ClampFinite(
                _runtime.Settings.LookSensitivity.Value,
                0.15f,
                0.001f,
                10f);
            if (_runtime.Settings.ScaleLookSensitivityWithFov.Value)
            {
                float appliedFov = NumericUtility.ClampFinite(
                    CurrentFov,
                    60f,
                    MinimumFov,
                    179f);
                float fovScale = Mathf.Tan(appliedFov * Mathf.Deg2Rad * 0.5f) /
                    Mathf.Tan(30f * Mathf.Deg2Rad);
                sensitivity *= NumericUtility.ClampFinite(fovScale, 1f, 0.001f, 2f);
            }

            _targetYaw += delta.x * sensitivity;
            float verticalSign = _runtime.Settings.InvertLookY.Value ? 1f : -1f;
            _targetPitch += delta.y * sensitivity * verticalSign;
            _targetPitch = Mathf.Clamp(_targetPitch, -89.9f, 89.9f);
            _targetRoll = 0f;
            _lookSmoothingActive = true;
        }

        private void UpdateSmoothedLook(float unscaledDeltaTime)
        {
            if (!_lookSmoothingActive || _focusTarget != null || CameraTransform == null)
            {
                return;
            }

            if (!NumericUtility.IsFinite(unscaledDeltaTime) || unscaledDeltaTime <= 0f)
            {
                return;
            }

            if (unscaledDeltaTime > 0.25f)
            {
                CancelLookSmoothing();
                return;
            }

            _targetYaw = NumericUtility.IsFinite(_targetYaw) ? _targetYaw : _yaw;
            _targetPitch = NumericUtility.ClampFinite(_targetPitch, _pitch, -89.9f, 89.9f);
            _targetRoll = NumericUtility.ClampFinite(_targetRoll, _roll, -180f, 180f);
            float smoothingTime = NumericUtility.ClampFinite(
                _runtime.Settings.LookSmoothingTime.Value,
                0.06f,
                0f,
                1f);
            if (smoothingTime <= 0.0001f)
            {
                _yaw = _targetYaw;
                _pitch = _targetPitch;
                _roll = _targetRoll;
                _lookSmoothingActive = false;
            }
            else
            {
                float blend = 1f - Mathf.Exp(-unscaledDeltaTime / smoothingTime);
                _yaw += (_targetYaw - _yaw) * blend;
                _pitch += (_targetPitch - _pitch) * blend;
                _roll += (_targetRoll - _roll) * blend;

                if (Mathf.Abs(_targetYaw - _yaw) <= 0.0001f)
                {
                    _yaw = _targetYaw;
                }
                if (Mathf.Abs(_targetPitch - _pitch) <= 0.0001f)
                {
                    _pitch = _targetPitch;
                }
                if (Mathf.Abs(_targetRoll - _roll) <= 0.0001f)
                {
                    _roll = _targetRoll;
                }

                if (_yaw == _targetYaw && _pitch == _targetPitch && _roll == _targetRoll)
                {
                    _lookSmoothingActive = false;
                }
            }

            CameraTransform.rotation = Quaternion.Euler(_pitch, _yaw, _roll);
        }

        private void CancelLookSmoothing()
        {
            SyncLookAnglesFromCamera();
        }

        private void TrySelectFocusTarget(Vector2 screenPosition)
        {
            Camera camera = CameraManager.MainCamera;
            if (camera == null)
            {
                return;
            }

            Ray ray;
            if (!TryCreateSelectionRay(camera, screenPosition, out ray))
            {
                _runtime.Notify("鼠标位置不在相机画面内，无法选择目标。", false);
                return;
            }

            float maximumDistance = NumericUtility.ClampFinite(
                _runtime.Settings.FocusMaximumDistance.Value,
                100000f,
                10f,
                1000000f);
            int layerMask =
                (1 << Layers.DefaultLayer) |
                (1 << Layers.CarLayer) |
                (1 << Layers.AircraftInteractable) |
                (1 << Layers.TerrainLayer) |
                (1 << Layers.AircraftLayer) |
                (1 << Layers.CarrierDeck) |
                (1 << Layers.AircraftCollisionOnly) |
                (1 << Layers.AircraftCollisionNone) |
                (1 << Layers.RemoteAircraftLayer);

            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                maximumDistance,
                layerMask,
                QueryTriggerInteraction.Collide);

            float nearestPartDistance = float.PositiveInfinity;
            float nearestDynamicGroundTargetDistance = float.PositiveInfinity;
            float nearestTerrainDistance = float.PositiveInfinity;
            FocusTarget nearestPartTarget = null;
            FocusTarget nearestDynamicGroundTarget = null;
            FocusTarget nearestTerrainTarget = null;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                int hitLayer = hit.collider.gameObject.layer;
                if (hit.collider.isTrigger && hitLayer != Layers.AircraftInteractable)
                {
                    continue;
                }

                PartScript part = hit.collider.GetComponentInParent<PartScript>();
                if (part != null && hit.distance < nearestPartDistance)
                {
                    nearestPartTarget = FocusTarget.CreatePart(part, hit.collider, hit.point);
                    nearestPartDistance = hit.distance;
                    continue;
                }

                FocusTarget dynamicGroundTarget = TryCreateDynamicGroundTarget(
                    hit.collider,
                    hit.point);
                if (dynamicGroundTarget != null &&
                    hit.distance < nearestDynamicGroundTargetDistance)
                {
                    nearestDynamicGroundTarget = dynamicGroundTarget;
                    nearestDynamicGroundTargetDistance = hit.distance;
                    continue;
                }

                if (hitLayer == Layers.TerrainLayer && hit.distance < nearestTerrainDistance)
                {
                    nearestTerrainTarget = FocusTarget.CreateTerrain(hit.point);
                    nearestTerrainDistance = hit.distance;
                }
            }

            float nearestOccluderDistance = Mathf.Min(
                nearestPartDistance,
                Mathf.Min(nearestDynamicGroundTargetDistance, nearestTerrainDistance));
            ReleasedWeaponSelection releasedWeapon;
            if (ReleasedWeaponSelectionHelper.TryFindNearScreenPoint(
                    camera,
                    screenPosition,
                    maximumDistance,
                    ReleasedWeaponEdgeTolerancePixels,
                    nearestOccluderDistance,
                    out releasedWeapon))
            {
                string weaponName = Localization.Text(
                    releasedWeapon.Kind == ReleasedWeaponKind.Missile ? "Missile" : "Bomb");
                FocusTarget weaponTarget = FocusTarget.CreateTransform(
                    releasedWeapon.AnchorTransform,
                    releasedWeapon.WorldPosition,
                    weaponName);
                if (weaponTarget != null)
                {
                    SetFocusTarget(weaponTarget);
                    return;
                }
            }

            if (nearestPartTarget != null &&
                nearestPartDistance <= nearestDynamicGroundTargetDistance &&
                nearestPartDistance <= nearestTerrainDistance)
            {
                SetFocusTarget(nearestPartTarget);
                return;
            }

            if (nearestDynamicGroundTarget != null &&
                nearestDynamicGroundTargetDistance <= nearestTerrainDistance)
            {
                SetFocusTarget(nearestDynamicGroundTarget);
                return;
            }

            if (nearestTerrainTarget != null)
            {
                SetFocusTarget(nearestTerrainTarget);
                return;
            }

            _runtime.Notify("鼠标方向未命中可锁定的地形、飞机部件、动态地面目标或离架武器。", false);
        }

        private bool TryCreateSelectionRay(Camera camera, Vector2 screenPosition, out Ray ray)
        {
            ray = default(Ray);
            if (camera == null)
            {
                return false;
            }

            // Keep Unity's native behavior for the regular-FOV path. The explicit
            // unprojection below is needed only while rendering through our custom
            // sub-one-degree projection matrix, where ScreenPointToRay can still use
            // the camera's logical 1-degree fieldOfView.
            if (!_customProjectionActive)
            {
                ray = camera.ScreenPointToRay(screenPosition);
                return IsFinite(ray.origin) && IsFinite(ray.direction) &&
                    ray.direction.sqrMagnitude > 0.000001f;
            }

            Rect pixelRect = camera.pixelRect;
            if (!NumericUtility.IsFinite(pixelRect.x) ||
                !NumericUtility.IsFinite(pixelRect.y) ||
                !NumericUtility.IsFinite(pixelRect.width) ||
                !NumericUtility.IsFinite(pixelRect.height) ||
                pixelRect.width <= 0f || pixelRect.height <= 0f ||
                !pixelRect.Contains(screenPosition))
            {
                return false;
            }

            float normalizedX = (screenPosition.x - pixelRect.xMin) / pixelRect.width;
            float normalizedY = (screenPosition.y - pixelRect.yMin) / pixelRect.height;
            float clipX = normalizedX * 2f - 1f;
            float clipY = normalizedY * 2f - 1f;

            Matrix4x4 viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
            Matrix4x4 inverseViewProjection = viewProjection.inverse;
            Vector3 endpointA;
            Vector3 endpointB;
            if (!TryUnprojectClipPoint(
                    inverseViewProjection,
                    new Vector4(clipX, clipY, -1f, 1f),
                    out endpointA) ||
                !TryUnprojectClipPoint(
                    inverseViewProjection,
                    new Vector4(clipX, clipY, 1f, 1f),
                    out endpointB))
            {
                return false;
            }

            Vector3 cameraPosition = camera.transform.position;
            float distanceA = (endpointA - cameraPosition).sqrMagnitude;
            float distanceB = (endpointB - cameraPosition).sqrMagnitude;
            Vector3 nearPoint = distanceA <= distanceB ? endpointA : endpointB;
            Vector3 farPoint = distanceA <= distanceB ? endpointB : endpointA;
            Vector3 direction = farPoint - nearPoint;

            // Comparing endpoint distance instead of assuming a particular clip-space
            // Z convention keeps the near-plane origin correct on reversed-Z backends.
            if (!IsFinite(nearPoint) || !IsFinite(direction) ||
                direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            direction.Normalize();
            if (Vector3.Dot(direction, camera.transform.forward) <= 0f)
            {
                return false;
            }

            ray = new Ray(nearPoint, direction);
            return true;
        }

        private static bool TryUnprojectClipPoint(
            Matrix4x4 inverseViewProjection,
            Vector4 clipPoint,
            out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            Vector4 homogeneousWorld = inverseViewProjection * clipPoint;
            if (!NumericUtility.IsFinite(homogeneousWorld.x) ||
                !NumericUtility.IsFinite(homogeneousWorld.y) ||
                !NumericUtility.IsFinite(homogeneousWorld.z) ||
                !NumericUtility.IsFinite(homogeneousWorld.w) ||
                Mathf.Abs(homogeneousWorld.w) <= 0.0000001f)
            {
                return false;
            }

            float reciprocalW = 1f / homogeneousWorld.w;
            worldPoint = new Vector3(
                homogeneousWorld.x * reciprocalW,
                homogeneousWorld.y * reciprocalW,
                homogeneousWorld.z * reciprocalW);
            return IsFinite(worldPoint);
        }

        private void TrackDynamicPosition(
            Vector3 rawFocusPosition,
            Vector3d rawFocusGlobalPosition,
            Vector3 pointVelocity,
            bool hasDirectVelocity,
            bool predictsFromFixedTime,
            int motionSourceId,
            bool forceReset)
        {
            if (!IsFinite(pointVelocity))
            {
                pointVelocity = Vector3.zero;
                hasDirectVelocity = false;
                predictsFromFixedTime = false;
            }

            float smoothingTime = NumericUtility.ClampFinite(
                _runtime.Settings.FocusSmoothingTime.Value,
                0.08f,
                0f,
                0.5f);
            float frameDeltaTime = Time.unscaledDeltaTime;
            bool invalidFrameDelta = !NumericUtility.IsFinite(frameDeltaTime) ||
                frameDeltaTime < 0f || frameDeltaTime > TrackingResetDeltaTime;
            bool trackingSuspended = PauseManager.Paused || !Application.isFocused;
            double now = Time.timeAsDouble;
            if (!IsFinite(now))
            {
                now = 0.0;
                invalidFrameDelta = true;
            }

            bool wasTrackingSuspended = _focusTrackingSuspended;
            if (trackingSuspended)
            {
                if (!_hasFocusTrackingState ||
                    !IsFinite(_renderedFocusGlobalPosition))
                {
                    InitializeFocusTracking(
                        rawFocusGlobalPosition,
                        Vector3.zero,
                        false,
                        false,
                        now,
                        motionSourceId,
                        now);
                }
                _focusTrackingSuspended = true;
                TrackGuardedDynamicPosition(
                    _renderedFocusGlobalPosition,
                    rawFocusGlobalPosition,
                    rawFocusPosition);
                return;
            }

            _focusTrackingSuspended = false;
            bool reset = forceReset || invalidFrameDelta || wasTrackingSuspended ||
                !_hasFocusTrackingState ||
                _focusMotionSourceId != motionSourceId ||
                _focusSampleHasDirectVelocity != hasDirectVelocity ||
                _focusSamplePredictsFromFixedTime != predictsFromFixedTime;

            if (smoothingTime <= 0.0001f)
            {
                InitializeFocusTracking(
                    rawFocusGlobalPosition,
                    Vector3.zero,
                    false,
                    false,
                    now,
                    motionSourceId,
                    now);
                _renderedFocusGlobalPosition = rawFocusGlobalPosition;
                TrackGuardedDynamicPosition(
                    rawFocusGlobalPosition,
                    rawFocusGlobalPosition,
                    rawFocusPosition);
                return;
            }

            double sampleTime = GetFocusSampleTime(
                now,
                hasDirectVelocity,
                predictsFromFixedTime);
            if (reset)
            {
                InitializeFocusTracking(
                    rawFocusGlobalPosition,
                    pointVelocity,
                    hasDirectVelocity,
                    predictsFromFixedTime,
                    sampleTime,
                    motionSourceId,
                    now);
            }
            else
            {
                UpdateFocusSample(
                    rawFocusGlobalPosition,
                    pointVelocity,
                    hasDirectVelocity,
                    predictsFromFixedTime,
                    sampleTime,
                    motionSourceId,
                    now);
            }

            ExpireFallbackPrediction(now);
            if (!trackingSuspended && !invalidFrameDelta)
            {
                float smoothingDeltaTime = Mathf.Min(frameDeltaTime, MaximumTrackingDeltaTime);
                double decay = Math.Exp(-smoothingDeltaTime / smoothingTime);
                _focusCorrection *= decay;
            }

            Vector3d baseFocusPosition = PredictFocusBaseAt(now);
            Vector3d renderedFocusPosition = baseFocusPosition + _focusCorrection;
            if (!IsFinite(renderedFocusPosition))
            {
                InitializeFocusTracking(
                    rawFocusGlobalPosition,
                pointVelocity,
                hasDirectVelocity,
                predictsFromFixedTime,
                sampleTime,
                    motionSourceId,
                    now);
                renderedFocusPosition = rawFocusGlobalPosition;
            }

            _renderedFocusGlobalPosition = renderedFocusPosition;
            TrackGuardedDynamicPosition(
                renderedFocusPosition,
                rawFocusGlobalPosition,
                rawFocusPosition);
        }

        private void ExpireFallbackPrediction(double now)
        {
            if (!_hasFocusTrackingState || _focusSampleHasDirectVelocity ||
                _focusSampleVelocity.sqrMagnitude <= SampleVelocityEpsilonSquared)
            {
                return;
            }

            double sampleAge = now - _focusSampleTime;
            if (!IsFinite(sampleAge) ||
                sampleAge <= MaximumFallbackPredictionSeconds)
            {
                return;
            }

            Vector3d previousBaseNow = PredictFocusBaseAt(now);
            if (!IsFinite(previousBaseNow))
            {
                _focusSampleVelocity = Vector3.zero;
                return;
            }

            // A transform-only target has no authoritative stop signal. After a
            // short dropout, preserve the current virtual point and let the normal
            // correction decay return it smoothly to the last observed raw point.
            _focusCorrection += previousBaseNow - _focusSampleGlobalPosition;
            _focusSampleVelocity = Vector3.zero;
        }

        private void UpdateFocusSample(
            Vector3d rawFocusGlobalPosition,
            Vector3 pointVelocity,
            bool hasDirectVelocity,
            bool predictsFromFixedTime,
            double sampleTime,
            int motionSourceId,
            double now)
        {
            Vector3d rawDelta = rawFocusGlobalPosition - _focusSampleGlobalPosition;
            bool rawPositionChanged = rawDelta.sqrMagnitude > SamplePositionEpsilonSquared;
            bool velocityChanged = hasDirectVelocity &&
                (pointVelocity - _focusSampleVelocity).sqrMagnitude >
                SampleVelocityEpsilonSquared;
            bool sampleTimeAdvanced = sampleTime > _focusSampleTime + SampleTimeEpsilon;
            if (!rawPositionChanged && !velocityChanged &&
                !(hasDirectVelocity && predictsFromFixedTime && sampleTimeAdvanced))
            {
                return;
            }

            Vector3 nextVelocity = pointVelocity;
            if (!hasDirectVelocity)
            {
                nextVelocity = _focusSampleVelocity;
                double observationDeltaTime = now - _focusSampleTime;
                if (rawPositionChanged &&
                    observationDeltaTime > SampleTimeEpsilon &&
                    observationDeltaTime <= TrackingResetDeltaTime)
                {
                    Vector3 measuredVelocity = (Vector3)(rawDelta / observationDeltaTime);
                    if (IsFinite(measuredVelocity))
                    {
                        nextVelocity = Vector3.Lerp(
                            _focusSampleVelocity,
                            measuredVelocity,
                            0.65f);
                    }
                }
                sampleTime = now;
            }
            else if (sampleTime <= _focusSampleTime + SampleTimeEpsilon &&
                (rawPositionChanged || velocityChanged))
            {
                // Some tracked transforms are updated outside FixedUpdate even though
                // their parent body exposes a velocity. Timestamp those observations
                // at the current simulation time instead of predicting them twice.
                sampleTime = now;
            }

            Vector3d previousBaseNow = PredictFocusBaseAt(now, true);
            Vector3d nextBaseNow = PredictFocusBase(
                rawFocusGlobalPosition,
                nextVelocity,
                hasDirectVelocity,
                predictsFromFixedTime,
                sampleTime,
                now,
                true);
            Vector3d residual = nextBaseNow - previousBaseNow;
            if (!IsFinite(residual) ||
                residual.sqrMagnitude >
                    TargetJumpResetDistance * TargetJumpResetDistance)
            {
                if (!hasDirectVelocity)
                {
                    nextVelocity = Vector3.zero;
                }
                InitializeFocusTracking(
                    rawFocusGlobalPosition,
                    nextVelocity,
                    hasDirectVelocity,
                    predictsFromFixedTime,
                    sampleTime,
                    motionSourceId,
                    now);
                return;
            }

            // Preserve the virtual point when a fresh physics/network sample corrects
            // the base trajectory. Only this offset is eased out on later render frames.
            _focusCorrection += previousBaseNow - nextBaseNow;
            _focusSampleGlobalPosition = rawFocusGlobalPosition;
            _focusSampleVelocity = nextVelocity;
            _focusSampleHasDirectVelocity = hasDirectVelocity;
            _focusSamplePredictsFromFixedTime = predictsFromFixedTime;
            _focusSampleTime = sampleTime;
            _focusMotionSourceId = motionSourceId;
        }

        private void InitializeFocusTracking(
            Vector3d rawFocusGlobalPosition,
            Vector3 pointVelocity,
            bool hasDirectVelocity,
            bool predictsFromFixedTime,
            double sampleTime,
            int motionSourceId,
            double now)
        {
            _focusSampleGlobalPosition = rawFocusGlobalPosition;
            _focusSampleVelocity = IsFinite(pointVelocity) ? pointVelocity : Vector3.zero;
            _focusSampleHasDirectVelocity = hasDirectVelocity && IsFinite(pointVelocity);
            _focusSamplePredictsFromFixedTime =
                _focusSampleHasDirectVelocity && predictsFromFixedTime;
            _focusSampleTime = sampleTime;
            _focusMotionSourceId = motionSourceId;
            _hasFocusTrackingState = true;

            Vector3d baseFocusPosition = PredictFocusBase(
                _focusSampleGlobalPosition,
                _focusSampleVelocity,
                _focusSampleHasDirectVelocity,
                _focusSamplePredictsFromFixedTime,
                _focusSampleTime,
                now,
                false);
            _focusCorrection = rawFocusGlobalPosition - baseFocusPosition;
            _renderedFocusGlobalPosition = rawFocusGlobalPosition;
        }

        private Vector3d PredictFocusBaseAt(double now)
        {
            return PredictFocusBaseAt(now, false);
        }

        private Vector3d PredictFocusBaseAt(double now, bool compareFreshSample)
        {
            return PredictFocusBase(
                _focusSampleGlobalPosition,
                _focusSampleVelocity,
                _focusSampleHasDirectVelocity,
                _focusSamplePredictsFromFixedTime,
                _focusSampleTime,
                now,
                compareFreshSample);
        }

        private static Vector3d PredictFocusBase(
            Vector3d samplePosition,
            Vector3 velocity,
            bool hasDirectVelocity,
            bool predictsFromFixedTime,
            double sampleTime,
            double now,
            bool compareFreshSample)
        {
            double elapsed = now - sampleTime;
            if (!IsFinite(elapsed) || elapsed <= 0.0 || !IsFinite(velocity))
            {
                return samplePosition;
            }

            double maximumPrediction;
            if (hasDirectVelocity)
            {
                if (compareFreshSample)
                {
                    // Continue the old constant-velocity trajectory only while
                    // comparing it with a fresh observation. This prevents normal
                    // render-interpolated motion from being mistaken for a correction;
                    // final rendering below still uses zero extra horizon for such poses.
                    maximumPrediction = TrackingResetDeltaTime;
                }
                else if (!predictsFromFixedTime)
                {
                    maximumPrediction = 0.0;
                }
                else
                {
                    float fixedDeltaTime = NumericUtility.ClampFinite(
                        Time.fixedDeltaTime,
                        0.02f,
                        0.0001f,
                        0.1f);
                    maximumPrediction = fixedDeltaTime * 1.5f;
                }
            }
            else
            {
                maximumPrediction = MaximumFallbackPredictionSeconds;
            }

            elapsed = Math.Min(elapsed, maximumPrediction);
            return samplePosition + new Vector3d(velocity) * elapsed;
        }

        private static double GetFocusSampleTime(
            double now,
            bool hasDirectVelocity,
            bool predictsFromFixedTime)
        {
            if (!hasDirectVelocity || !predictsFromFixedTime)
            {
                return now;
            }

            double fixedTime = Time.fixedTimeAsDouble;
            if (!IsFinite(fixedTime) || fixedTime < 0.0 || fixedTime > now)
            {
                return now;
            }
            return fixedTime;
        }

        private void TrackTerrainPosition(Vector3 focusPosition, bool snap)
        {
            Vector3 direction = focusPosition - CameraTransform.position;
            Quaternion desiredRotation;
            if (!TryCreateFocusRotation(direction, CameraTransform.up, out desiredRotation))
            {
                return;
            }

            float smoothingTime = NumericUtility.ClampFinite(
                _runtime.Settings.FocusSmoothingTime.Value,
                0.08f,
                0f,
                0.5f);
            if (snap || smoothingTime <= 0.0001f)
            {
                CameraTransform.rotation = desiredRotation;
            }
            else
            {
                float deltaTime = NumericUtility.ClampFinite(
                    Time.unscaledDeltaTime,
                    0f,
                    0f,
                    MaximumTrackingDeltaTime);
                float blend = 1f - Mathf.Exp(-deltaTime / smoothingTime);
                CameraTransform.rotation = Quaternion.Slerp(
                    CameraTransform.rotation,
                    desiredRotation,
                    blend);
            }

            UpdateFloatingOriginFocus();
        }

        private void TrackImmediateGlobalPosition(Vector3d focusGlobalPosition)
        {
            Vector3d cameraGlobalPosition = ToGlobalPosition(CameraTransform.position);
            Vector3 direction = (Vector3)(focusGlobalPosition - cameraGlobalPosition);
            Quaternion desiredRotation;
            if (!TryCreateFocusRotation(direction, CameraTransform.up, out desiredRotation))
            {
                return;
            }

            // Moving-target focus owns rotation only. It never changes the
            // separately configured FOV smoothing path.
            CameraTransform.rotation = desiredRotation;

            UpdateFloatingOriginFocus();
        }

        private void TrackImmediateDynamicPosition(Vector3d focusGlobalPosition)
        {
            _renderedFocusGlobalPosition = focusGlobalPosition;
            _hasFocusTrackingState = true;
            _focusTrackingSuspended = false;
            TrackImmediateGlobalPosition(focusGlobalPosition);
        }

        private void TrackGuardedDynamicPosition(
            Vector3d renderedFocusGlobalPosition,
            Vector3d rawFocusGlobalPosition,
            Vector3 rawFocusPosition)
        {
            Vector3d cameraGlobalPosition = ToGlobalPosition(CameraTransform.position);
            Vector3 renderedDirection =
                (Vector3)(renderedFocusGlobalPosition - cameraGlobalPosition);
            Vector3 rawDirection = (Vector3)(rawFocusGlobalPosition - cameraGlobalPosition);
            Vector3 referenceUp = CameraTransform.up;
            Quaternion renderedRotation;
            Quaternion rawRotation;
            if (!TryCreateFocusRotation(renderedDirection, referenceUp, out renderedRotation) ||
                !TryCreateFocusRotation(rawDirection, referenceUp, out rawRotation))
            {
                return;
            }

            CameraTransform.rotation = renderedRotation;
            Camera camera = CameraManager != null ? CameraManager.MainCamera : null;
            Vector2 projectedPosition;
            if (camera == null ||
                !TryProjectWithCurrentMatrix(camera, rawFocusPosition, out projectedPosition))
            {
                CameraTransform.rotation = rawRotation;
                UpdateFloatingOriginFocus();
                return;
            }

            Rect pixelRect = camera.pixelRect;
            Vector2 center = pixelRect.center;
            float pixelError = Vector2.Distance(projectedPosition, center);
            if (!NumericUtility.IsFinite(pixelError))
            {
                CameraTransform.rotation = rawRotation;
                UpdateFloatingOriginFocus();
                return;
            }

            if (pixelError > FocusSoftErrorPixels)
            {
                float normalizedError = Mathf.InverseLerp(
                    FocusSoftErrorPixels,
                    FocusHardErrorPixels,
                    pixelError);
                float guardBlend = normalizedError * normalizedError *
                    (3f - 2f * normalizedError);
                if (pixelError >= FocusHardErrorPixels)
                {
                    guardBlend = 1f;
                }

                Vector3 guardedDirection = Vector3.Slerp(
                    renderedDirection.normalized,
                    rawDirection.normalized,
                    guardBlend);
                Quaternion guardedRotation;
                if (TryCreateFocusRotation(
                    guardedDirection,
                    referenceUp,
                    out guardedRotation))
                {
                    CameraTransform.rotation = guardedRotation;
                }
                else
                {
                    CameraTransform.rotation = rawRotation;
                }
            }

            UpdateFloatingOriginFocus();
        }

        private static bool TryProjectWithCurrentMatrix(
            Camera camera,
            Vector3 worldPosition,
            out Vector2 screenPosition)
        {
            Vector3 viewPosition = camera.worldToCameraMatrix.MultiplyPoint(worldPosition);
            if (!IsFinite(viewPosition) || viewPosition.z >= -ProjectionEpsilon)
            {
                screenPosition = default(Vector2);
                return false;
            }

            Vector4 clipPosition = camera.projectionMatrix * new Vector4(
                viewPosition.x,
                viewPosition.y,
                viewPosition.z,
                1f);
            if (!NumericUtility.IsFinite(clipPosition.x) ||
                !NumericUtility.IsFinite(clipPosition.y) ||
                !NumericUtility.IsFinite(clipPosition.w) ||
                clipPosition.w <= ProjectionEpsilon)
            {
                screenPosition = default(Vector2);
                return false;
            }

            float inverseW = 1f / clipPosition.w;
            float normalizedX = clipPosition.x * inverseW;
            float normalizedY = clipPosition.y * inverseW;
            if (!NumericUtility.IsFinite(normalizedX) ||
                !NumericUtility.IsFinite(normalizedY))
            {
                screenPosition = default(Vector2);
                return false;
            }

            Rect pixelRect = camera.pixelRect;
            screenPosition = new Vector2(
                pixelRect.xMin + (normalizedX + 1f) * 0.5f * pixelRect.width,
                pixelRect.yMin + (normalizedY + 1f) * 0.5f * pixelRect.height);
            return IsFinite(screenPosition);
        }

        private static bool TryCreateFocusRotation(
            Vector3 direction,
            Vector3 referenceUp,
            out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            Vector3 forward = direction.normalized;
            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.999f)
            {
                up = Vector3.ProjectOnPlane(referenceUp, forward);
                if (up.sqrMagnitude <= 0.000001f)
                {
                    up = Vector3.ProjectOnPlane(Vector3.forward, forward);
                }
                if (up.sqrMagnitude <= 0.000001f)
                {
                    up = Vector3.right;
                }
                up.Normalize();
            }

            rotation = Quaternion.LookRotation(forward, up);
            return true;
        }

        private void ApplyCameraDisplacement(Vector3 displacement)
        {
            if (CameraTransform == null || !IsFinite(displacement))
            {
                return;
            }

            CameraTransform.position += displacement;
            UpdateFloatingOriginFocus();
        }

        private static FocusTarget TryCreateDynamicGroundTarget(
            Collider collider,
            Vector3 hitPosition)
        {
            if (collider == null)
            {
                return null;
            }

            GroundTarget target = null;
            SinkableShipScript ship = collider.GetComponentInParent<SinkableShipScript>();
            if (ship != null)
            {
                target = ship.Target;
            }

            if (target == null)
            {
                SimpleGroundVehicleScript vehicle =
                    collider.GetComponentInParent<SimpleGroundVehicleScript>();
                if (vehicle != null)
                {
                    target = vehicle.Target;
                }
            }

            if (target == null)
            {
                MechScript mech = collider.GetComponentInParent<MechScript>();
                if (mech != null)
                {
                    target = mech.Target;
                }
            }

            if (target == null || target.IsDead)
            {
                return null;
            }

            string displayName = string.IsNullOrEmpty(target.Name)
                ? Localization.Text("GameTarget")
                : target.Name;
            return FocusTarget.CreateDynamicGroundTarget(
                collider.transform,
                hitPosition,
                displayName);
        }

        private void UpdateUnlockedFocalPosition()
        {
            if (_focusTarget == null && CameraManager != null && CameraManager.CameraFocalPosition != null)
            {
                UpdateFloatingOriginFocus();
            }
        }

        private void UpdateFrameFov(
            float unscaledDeltaTime, bool hasFreshFocus, Vector3d focusGlobalPosition)
        {
            float scroll = _pendingFovScroll;
            _pendingFovScroll = 0f;
            float maximumFov = NumericUtility.ClampFinite(
                _runtime.Settings.MaximumFov.Value, 120f, MinimumFov, 179f);
            float sensitivity = NumericUtility.ClampFinite(
                _runtime.Settings.FovScrollSensitivity.Value, 1f, 0.01f, 10f);

            double distance = double.NaN;
            if (hasFreshFocus)
            {
                Vector3d relative = focusGlobalPosition - ToGlobalPosition(CameraTransform.position);
                distance = Math.Sqrt(relative.x * relative.x +
                    relative.y * relative.y + relative.z * relative.z);
            }

            Camera camera = CameraManager.MainCamera;
            bool wasActive = _autoFov.Active;
            double automaticTarget;
            if (_autoFov.TryGetTargetFov(
                distance, camera != null ? camera.aspect : double.NaN,
                _currentFov, maximumFov, scroll, sensitivity, out automaticTarget))
            {
                _targetFov = (float)automaticTarget;
            }
            else
            {
                if (wasActive)
                {
                    // Freeze stale auto-zoom even during the lost-target grace
                    // period, but keep the mode armed and allow manual scrolling.
                    _targetFov = _currentFov;
                }
                if (NumericUtility.IsFinite(scroll) && Mathf.Abs(scroll) > 0.0001f)
                {
                    _targetFov = NumericUtility.ClampFinite(
                        _targetFov * Mathf.Pow(0.9f, scroll * sensitivity),
                        _currentFov, MinimumFov, maximumFov);
                }
            }

            UpdateSmoothedFov(unscaledDeltaTime);
        }

        private void UpdateSmoothedFov(float unscaledDeltaTime)
        {
            // FOV smoothing is independent of focus mode. Dynamic targets snap
            // rotation; manual AND automatic zoom use this menu setting once.
            float maximumFov = NumericUtility.ClampFinite(
                _runtime.Settings.MaximumFov.Value,
                120f,
                MinimumFov,
                179f);
            _targetFov = NumericUtility.ClampFinite(
                _targetFov,
                _currentFov,
                MinimumFov,
                maximumFov);
            _currentFov = NumericUtility.ClampFinite(
                _currentFov,
                _targetFov,
                MinimumFov,
                maximumFov);

            float smoothingTime = NumericUtility.ClampFinite(
                _runtime.Settings.FovSmoothingTime.Value,
                Plugin.DefaultFovSmoothingTime,
                0f,
                2f);
            SetAppliedFov((float)FovMath.Smooth(
                _currentFov, _targetFov, smoothingTime, unscaledDeltaTime));
        }

        private void SetAppliedFov(float requestedFov)
        {
            float maximumFov = NumericUtility.ClampFinite(
                _runtime.Settings.MaximumFov.Value,
                120f,
                MinimumFov,
                179f);
            _currentFov = NumericUtility.ClampFinite(requestedFov, 60f, MinimumFov, maximumFov);

            Camera camera = CameraManager.MainCamera;
            if (camera == null)
            {
                return;
            }

            if (_currentFov < 1f)
            {
                CameraManager.SetCameraFov(1f);
                _customProjectionActive = true;
                MaintainCustomProjection();
                return;
            }

            ResetCustomProjection();
            CameraManager.SetCameraFov(_currentFov);
            _currentFov = camera.fieldOfView;
        }

        private void MaintainCustomProjection()
        {
            if (!_customProjectionActive || !IsSelected || CameraManager == null)
            {
                return;
            }

            Camera camera = CameraManager.MainCamera;
            if (camera == null)
            {
                return;
            }

            float aspect = camera.aspect;
            if (!float.IsNaN(aspect) && !float.IsInfinity(aspect) && aspect > 0f)
            {
                camera.projectionMatrix = Matrix4x4.Perspective(
                    _currentFov,
                    aspect,
                    camera.nearClipPlane,
                    camera.farClipPlane);
            }
        }

        private void ResetCustomProjection()
        {
            if (!_customProjectionActive)
            {
                return;
            }

            if (CameraManager != null && CameraManager.MainCamera != null)
            {
                CameraManager.MainCamera.ResetProjectionMatrix();
            }

            _customProjectionActive = false;
        }

        private void UpdateFloatingOriginFocus()
        {
            if (CameraManager != null && CameraManager.CameraFocalPosition != null &&
                CameraTransform != null)
            {
                CameraManager.CameraFocalPosition.position =
                    CameraTransform.position + CameraTransform.forward * 10f;
            }
        }

        private static Vector3d ToGlobalPosition(Vector3 floatingOriginPosition)
        {
            return GameWorld.Instance.FloatingOriginOffsetD +
                new Vector3d(floatingOriginPosition);
        }

        private void ResetFocusTrackingState()
        {
            _focusSampleGlobalPosition = Vector3d.zero;
            _focusSampleVelocity = Vector3.zero;
            _focusSampleHasDirectVelocity = false;
            _focusSamplePredictsFromFixedTime = false;
            _focusSampleTime = 0.0;
            _focusCorrection = Vector3d.zero;
            _renderedFocusGlobalPosition = Vector3d.zero;
            _hasFocusTrackingState = false;
            _focusMotionSourceId = 0;
            _focusTrackingSuspended = false;
        }

        private void SyncLookAnglesFromCamera(bool resetRoll = false)
        {
            if (CameraTransform == null)
            {
                _lookSmoothingActive = false;
                return;
            }

            Vector3 euler = CameraTransform.rotation.eulerAngles;
            if (resetRoll)
            {
                // Level the horizon immediately on entry, without changing the
                // inherited position, heading, pitch or FOV. Do not smooth roll
                // back from the previous controller on the first mouse drag.
                euler.z = 0f;
                CameraTransform.rotation = Quaternion.Euler(euler);
            }
            _yaw = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            _roll = euler.z > 180f ? euler.z - 360f : euler.z;
            _pitch = Mathf.Clamp(_pitch, -89.9f, 89.9f);
            _targetYaw = _yaw;
            _targetPitch = _pitch;
            _targetRoll = _roll;
            _lookSmoothingActive = false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return NumericUtility.IsFinite(value.x) &&
                NumericUtility.IsFinite(value.y) &&
                NumericUtility.IsFinite(value.z);
        }

        private static bool IsFinite(Vector2 value)
        {
            return NumericUtility.IsFinite(value.x) &&
                NumericUtility.IsFinite(value.y);
        }

        private static bool IsFinite(Vector3d value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
