using System;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.Craft;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Cameras;
using Assets.Scripts.Flight.Combat;
using HarmonyLib;
using UnityEngine;

namespace SP2FreeCamera
{
    // An overlay on native first-person cameras, not another freecam controller.
    // Never writes camera position, focal position, FOV, aircraft or weapon state.
    internal sealed class FirstPersonFocus
    {
        private static readonly FieldInfo LookAnglesField =
            AccessTools.Field(typeof(InteractiveCameraController), "_deltaRotation");
        private static readonly FieldInfo CharacterRotationField =
            AccessTools.Field(typeof(FirstPersonCharacterCameraController), "_currentRotation");
        private static readonly FieldInfo CharacterTransformField =
            AccessTools.Field(typeof(FirstPersonCharacterCameraController), "_targetTransform");
        private static readonly FieldInfo LookAtCockpitField =
            AccessTools.Field(typeof(FirstPersonCameraController), "_lookAtCockpit");
        private static readonly FieldInfo CameraRecenterField =
            AccessTools.Field(typeof(FirstPersonCameraController), "_animatingRecenter");
        private static readonly FieldInfo CharacterRecenterField =
            AccessTools.Field(typeof(FirstPersonCharacterCameraController), "_animatingRecenter");

        private readonly FreeCameraRuntime _runtime;
        private InteractiveCameraController _owner;
        private InteractiveCameraController _nativeLookOwner;
        private Vector2 _nativeLookAngles;
        private FocusTarget _target;
        private Vector2 _lockedAngles;
        private bool _hasLockedAngles;
        private int _lastInputFrame = -1;
        private int _acquiredFrame = -1;
        private bool _middleCaptured;
        private Vector3 _lastTargetAbsolute;
        private bool _hasLastTarget;
        private float _lostElapsed;
        private int _lastLostFrame = -1;
        private bool _faulted;

        internal FirstPersonFocus(FreeCameraRuntime runtime)
        {
            _runtime = runtime;
            if (LookAnglesField == null || CharacterRotationField == null ||
                CharacterTransformField == null || LookAtCockpitField == null ||
                CameraRecenterField == null || CharacterRecenterField == null)
            {
                DisableAfterError();
            }
        }

        internal static bool IsSupportedType(CameraController controller)
        {
            // IsFirstPerson is false for the classic cockpit, and is not a
            // reliable whitelist. Targeting pods and freecam are excluded.
            return controller != null &&
                (controller.GetType() == typeof(CockpitCameraController) ||
                 controller.GetType() == typeof(FirstPersonCameraController) ||
                 controller.GetType() == typeof(FirstPersonCharacterCameraController));
        }

        private InteractiveCameraController GetEligibleController(bool includeLookAtCockpit = false)
        {
            CameraManagerScript manager = CameraManagerScript.Instance;
            if (_faulted || _runtime.Active || !_runtime.Settings.FirstPersonFocusEnabled.Value ||
                FlightSceneScript.Instance == null || manager == null || manager.MainCamera == null ||
                manager.CameraTransform == null ||
                (manager.XRCameraManager != null && manager.XRCameraManager.XrCamerasEnabled))
            {
                return null;
            }
            CameraController controller = manager.Controller;
            if (!IsSupportedType(controller) || !controller.IsSelected || !controller.IsActive)
            {
                return null;
            }
            FirstPersonCameraController camera = controller as FirstPersonCameraController;
            if (!includeLookAtCockpit && camera != null && ((bool)LookAtCockpitField.GetValue(camera) ||
                (camera.CameraVantage != null && camera.CameraVantage.Data.LookAtCockpit)))
            {
                // The game caches this flag at construction. Respect both the
                // effective native state and the current part setting.
                return null;
            }
            return (InteractiveCameraController)controller;
        }

        private void RefreshOwner()
        {
            InteractiveCameraController controller = GetEligibleController();
            if (!ReferenceEquals(controller, _owner))
            {
                Reset(false);
                _owner = controller;
            }
        }

        internal bool HasFocus(CameraController controller)
        {
            return _target != null && ReferenceEquals(controller, _owner) &&
                ReferenceEquals(controller, GetEligibleController());
        }

        internal void ProcessFrame()
        {
            RefreshOwner();
            // Native recenter is safe even when Look At Cockpit owns rotation:
            // it resets that camera's local look offset, not its target or bank.
            InteractiveCameraController recenterController = _owner ?? GetEligibleController(true);
            if (recenterController == null || _lastInputFrame == Time.frameCount)
            {
                return;
            }
            // Called by both the plugin Update and the native input prefix.
            // Use the real rendered frame, not CameraController.Update's counter.
            _lastInputFrame = Time.frameCount;
            if (!Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2) && !Input.GetMouseButtonUp(2))
            {
                _middleCaptured = false;
            }
            if (!_runtime.CanProcessFirstPersonKeyboardInput() || IsRecentering(recenterController))
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                // Explicitly release before calling the native method, including
                // cameras whose native UI hides recenter while already centered.
                // This also prevents a pick/target key in this frame from relocking.
                Release(true);
                recenterController.RecenterView();
                return;
            }
            if (_owner == null)
            {
                return;
            }
            Vector2 position = SelectionPosition();
            if (Input.GetMouseButtonDown(2) && _runtime.CanProcessFirstPersonPointerInput(position))
            {
                _middleCaptured = true;
                Pick(position);
            }
            KeyCode key = _runtime.Settings.FocusSelectedTargetKey.Value;
            if (key != KeyCode.None && Input.GetKeyDown(key))
            {
                FocusSelectedGameTarget();
            }
        }

        private bool IsRecentering(InteractiveCameraController controller)
        {
            if (controller is FirstPersonCameraController)
            {
                return (bool)CameraRecenterField.GetValue(controller);
            }
            return controller is FirstPersonCharacterCameraController &&
                (bool)CharacterRecenterField.GetValue(controller);
        }

        private Vector2 SelectionPosition()
        {
            return Cursor.lockState == CursorLockMode.Locked
                ? _owner.CameraManager.MainCamera.pixelRect.center : (Vector2)Input.mousePosition;
        }

        private void Pick(Vector2 position)
        {
            Camera camera = _owner.CameraManager.MainCamera;
            if (!camera.pixelRect.Contains(position))
            {
                return;
            }
            Ray ray = camera.ScreenPointToRay(position);
            if (!FirstPersonFocusMath.IsFinite(ray.origin) || !FirstPersonFocusMath.IsFinite(ray.direction))
            {
                return;
            }
            FlightScenePlayer player = FlightSceneScript.Instance.LocalPlayer;
            AircraftScript aircraft = player != null ? player.Aircraft : null;
            if (_owner.CameraVantage != null)
            {
                aircraft = _owner.CameraVantage.PartScript.Aircraft;
            }
            else if (player != null && player.CurrentIKSeat != null)
            {
                aircraft = player.CurrentIKSeat.PartScript.Aircraft;
            }
            Transform avatar = player != null && player.Avatar != null ? player.Avatar.transform : null;
            float maximumDistance = NumericUtility.ClampFinite(
                _runtime.Settings.FocusMaximumDistance.Value, 100000f, 10f, 1000000f);
            FocusTarget target = FocusSelection.Pick(
                camera, ray, position, maximumDistance, aircraft, avatar, false);
            if (target != null)
            {
                Acquire(target);
            }
            else
            {
                _runtime.Notify(Localization.Text("FirstPersonPickMiss"), false);
            }
        }

        private void FocusSelectedGameTarget()
        {
            FlightSceneScript scene = FlightSceneScript.Instance;
            AircraftScript aircraft = scene.FlightUI != null && scene.FlightUI.TargetingSystem != null
                ? scene.FlightUI.TargetingSystem.Aircraft : null;
            if (aircraft == null && scene.LocalPlayer != null)
            {
                aircraft = scene.LocalPlayer.CurrentOrPreviousAircraft;
            }
            Target target = aircraft != null && aircraft.TargetingSystem != null
                ? aircraft.TargetingSystem.CurrentTarget : null;
            if (target == null)
            {
                _runtime.Notify(Localization.Text("NoSelectedGameTarget"), false);
                return;
            }
            Acquire(FocusTarget.CreateGameTarget(target));
        }

        private void Acquire(FocusTarget target)
        {
            Vector3 position;
            if (target == null || !target.TryGetFloatingOriginPosition(out position) ||
                !FirstPersonFocusMath.IsFinite(position))
            {
                _runtime.Notify(Localization.Text("TargetUnavailable"), false);
                return;
            }
            _target = target;
            _hasLockedAngles = false;
            _acquiredFrame = Time.frameCount;
            _lastTargetAbsolute = Utility.ConvertFloatingOriginToAbsolutePosition(position);
            _hasLastTarget = true;
            _lostElapsed = 0f;
            _lastLostFrame = -1;
            _runtime.Notify(Localization.Text("FirstPersonFocusLocked"), false);
        }

        internal bool ConsumeMiddle(InteractiveCameraController controller)
        {
            ProcessFrame();
            return ReferenceEquals(controller, _owner) && _middleCaptured;
        }

        internal void CaptureNativeLook(InteractiveCameraController controller, Vector2 angles)
        {
            RefreshOwner();
            if (ReferenceEquals(controller, _owner))
            {
                // Capture after base.Update reads native input, BEFORE derived
                // cockpit auto-centering mutates the angles used for this pose.
                _nativeLookOwner = controller;
                _nativeLookAngles = angles;
            }
        }

        internal void ApplyAfterNativeUpdate(InteractiveCameraController controller)
        {
            RefreshOwner();
            if (!HasFocus(controller) || !ReferenceEquals(controller, _nativeLookOwner))
            {
                return;
            }
            Vector3 targetPosition;
            if (!ResolveTarget(out targetPosition))
            {
                return;
            }

            Quaternion nativeRotation = controller.CameraTransform.rotation;
            Quaternion reference;
            Quaternion roll = Quaternion.identity;
            FirstPersonCharacterCameraController character = controller as FirstPersonCharacterCameraController;
            if (character == null)
            {
                // Native part rotation * local look. Removing only the look
                // retains the game's mounting/AutoOrient policy, not world-up.
                reference = nativeRotation * Quaternion.Inverse(FirstPersonFocusMath.LookOffset(_nativeLookAngles));
            }
            else if (character.IsCockpitMode)
            {
                Func<Transform> getTransform = (Func<Transform>)CharacterTransformField.GetValue(character);
                Transform anchor = getTransform() ?? controller.CameraManager.CameraFocalPosition;
                if (anchor == null)
                {
                    return;
                }
                reference = anchor.rotation;
                // The game already computed Chicken Head this update:
                // native = anchor * local look * native roll correction.
                // Reuse that correction; do not duplicate or zero its algorithm.
                Quaternion correction = Quaternion.Inverse(
                    reference * FirstPersonFocusMath.LookOffset(_nativeLookAngles)) * nativeRotation;
                roll = new Quaternion(0f, 0f, correction.z, correction.w).normalized;
            }
            else
            {
                // On-foot native FPV is world-relative, with no mounted roll.
                reference = Quaternion.identity;
            }

            Quaternion rotation;
            Vector2 angles;
            float previousYaw = _hasLockedAngles ? _lockedAngles.y : _nativeLookAngles.y;
            if (FirstPersonFocusMath.TrySolve(targetPosition - controller.CameraTransform.position,
                reference, roll, previousYaw, out angles, out rotation))
            {
                _lockedAngles = angles;
                _hasLockedAngles = true;
            }
            else if (_hasLockedAngles && FirstPersonFocusMath.IsValidRotation(reference) &&
                FirstPersonFocusMath.IsValidRotation(roll))
            {
                // Coincident target: retain local look and this update's bank.
                rotation = reference * FirstPersonFocusMath.LookOffset(_lockedAngles) * roll;
            }
            else
            {
                return;
            }
            controller.CameraTransform.rotation = rotation;
        }

        private bool ResolveTarget(out Vector3 position)
        {
            if (_target.TryGetFloatingOriginPosition(out position) && FirstPersonFocusMath.IsFinite(position))
            {
                _lastTargetAbsolute = Utility.ConvertFloatingOriginToAbsolutePosition(position);
                _hasLastTarget = true;
                _lostElapsed = 0f;
                _lastLostFrame = -1;
                return true;
            }
            if (_lastLostFrame != Time.frameCount)
            {
                _lastLostFrame = Time.frameCount;
                if (!PauseManager.Paused && Application.isFocused)
                {
                    _lostElapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
                }
            }
            if (_hasLastTarget && _lostElapsed < 0.2f)
            {
                position = Utility.ConvertAbsoluteToFloatingOriginPosition(_lastTargetAbsolute);
                return FirstPersonFocusMath.IsFinite(position);
            }
            Release(true);
            _runtime.Notify(Localization.Text("FirstPersonFocusLost"), false);
            return false;
        }

        internal bool BeforeNativeRotate(InteractiveCameraController controller, Vector2 rotation)
        {
            if (!HasFocus(controller))
            {
                return true;
            }
            if (!_runtime.CanProcessFirstPersonKeyboardInput())
            {
                return false;
            }
            // The click's mouse motion must not immediately cancel its own lock.
            if (_acquiredFrame == Time.frameCount)
            {
                return false;
            }
            if (rotation.sqrMagnitude > 0.000001f)
            {
                Release(true);
            }
            return true;
        }

        internal void BeforeRecenter(CameraController controller)
        {
            if (HasFocus(controller))
            {
                Release(true);
            }
        }

        private void Release(bool handoff)
        {
            if (handoff && _hasLockedAngles && _owner != null &&
                ReferenceEquals(_owner, GetEligibleController()))
            {
                // Seed native look state before a user drag/recenter, so input
                // starts at the locked direction instead of the old hidden one.
                LookAnglesField.SetValue(_owner, _lockedAngles);
                FirstPersonCharacterCameraController character = _owner as FirstPersonCharacterCameraController;
                if (character != null && !character.IsCockpitMode)
                {
                    CharacterRotationField.SetValue(character, _lockedAngles);
                }
            }
            _target = null;
            _hasLockedAngles = false;
            _hasLastTarget = false;
        }

        internal void Reset(bool handoff)
        {
            Release(handoff);
            _owner = null;
            _nativeLookOwner = null;
            _middleCaptured = false;
        }

        internal void DisableAfterError()
        {
            if (_faulted)
            {
                return;
            }
            Reset(false);
            _faulted = true;
            // No stack trace, user paths or target/player names in public logs.
            Plugin.Log.LogWarning("First-person focus disabled after an incompatible camera state. Native cameras and freecam remain available.");
        }
    }
}
