// Test doubles for source-extracted production methods. No Unity runtime or game
// assemblies are loaded. Quaternion stores Euler values only: these tests verify
// the entry contract, not Unity's native quaternion math or actual HUD rendering.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.Flight.UI.Targeting;

namespace UnityEngine
{
    public class MonoBehaviour { }
    public sealed class GameObject
    {
        public GameObject Parent;
        private readonly List<MonoBehaviour> _components = new List<MonoBehaviour>();
        public GameObject(MonoBehaviour component = null, GameObject parent = null)
        {
            if (component != null) _components.Add(component);
            Parent = parent;
        }
        public T GetComponent<T>() where T : class
        {
            foreach (MonoBehaviour component in _components)
                if (component is T) return component as T;
            return null;
        }
        public void GetComponentsInParent<T>(bool includeInactive, List<T> results) where T : class
        {
            for (GameObject current = this; current != null; current = current.Parent)
                foreach (MonoBehaviour component in current._components)
                    if (component is T) results.Add(component as T);
        }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static float Distance(Vector2 a, Vector2 b) { return (float)Math.Sqrt((a - b).sqrMagnitude); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z = 0f) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(); } }
    }
    public struct Quaternion
    {
        public Vector3 eulerAngles;
        public static Quaternion Euler(Vector3 euler) { return new Quaternion { eulerAngles = euler }; }
    }
    public sealed class Transform { public Vector3 position; public Quaternion rotation; }
    public static class Mathf
    {
        public static float Abs(float value) { return Math.Abs(value); }
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
    }
    public static class Application { public static bool isFocused = true; }
    public static class Screen { public static int width = 1920, height = 1080; }
    public static class Input
    {
        public static Vector3 mousePosition;
        public static Vector2 mouseScrollDelta;
        public static bool LeftDown, LeftHeld, LeftUp, MiddleDown;
        public static bool GetMouseButtonDown(int button) { return button == 0 ? LeftDown : MiddleDown; }
        public static bool GetMouseButton(int button) { return button == 0 && LeftHeld; }
        public static bool GetMouseButtonUp(int button) { return button == 0 && LeftUp; }
    }
}

namespace UnityEngine.EventSystems
{
    public interface IPointerDownHandler { }
    public class BaseRaycaster { }
    public sealed class PhysicsRaycaster : BaseRaycaster { }
    public struct RaycastResult { public GameObject gameObject; public BaseRaycaster module; }
    public sealed class PointerEventData
    {
        public readonly EventSystem EventSystem;
        public Vector2 position;
        public PointerEventData(EventSystem eventSystem) { EventSystem = eventSystem; }
        public void Reset() { }
    }
    public sealed class EventSystem
    {
        public static EventSystem current;
        public readonly List<RaycastResult> Hits = new List<RaycastResult>();
        public PointerEventData LastEvent;
        public void RaycastAll(PointerEventData eventData, List<RaycastResult> results)
        {
            LastEvent = eventData;
            results.AddRange(Hits);
        }
    }
    public static class ExecuteEvents
    {
        public static GameObject GetEventHandler<T>(GameObject hit) where T : class
        {
            for (GameObject current = hit; current != null; current = current.Parent)
                if (current.GetComponent<T>() != null) return current;
            return null;
        }
    }
}

namespace UnityEngine.UI { public sealed class GraphicRaycaster : BaseRaycaster { } }
namespace Assets.Scripts.Flight.UI
{
    public sealed class FlightScreenInputScript : MonoBehaviour, IPointerDownHandler { }
    public sealed class FlightUIScript { public bool Visible = true, IsPointerInsideGameView = true; }
}
namespace Assets.Scripts.Flight.UI.Targeting { public interface ITargetBox { } }
namespace Assets.Scripts.UI
{
    public sealed class UserInterface
    {
        public bool AnyDialogsOpen = false, IsTextInputFocused = false, ConsoleOpen = false;
        public bool AllowKeyboardInputs { get { return !AnyDialogsOpen && !IsTextInputFocused && !ConsoleOpen; } }
    }
}
namespace Assets.Scripts
{
    public sealed class UIInfo { public bool IsInteracting = false; }
    public sealed class Game
    {
        public static Game Instance;
        public UIInfo UIInfo = new UIInfo();
        public UI.UserInterface UserInterface = new UI.UserInterface();
    }
}

namespace SP2FreeCamera
{
    internal static class Localization { public static string Text(string key) { return key; } }
    internal static class NumericUtility
    {
        public static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public static float ClampFinite(float value, float fallback, float min, float max)
        {
            return Mathf.Clamp(IsFinite(value) ? value : fallback, min, max);
        }
    }
    internal sealed class TestSetting { public float Value = 4f; }
    internal sealed class TestSettings { public TestSetting DragThresholdPixels = new TestSetting(); }
    internal sealed class FreeCameraRuntime
    {
        internal bool _active = true, _menuVisible = false, PluginUiBlocked = false;
        internal FlightUIScript _flightUi = new FlightUIScript();
        private readonly List<RaycastResult> _pointerRaycastResults = new List<RaycastResult>(8);
        private readonly List<MonoBehaviour> _pointerHitBehaviours = new List<MonoBehaviour>(8);
        private EventSystem _pointerEventSystem;
        private PointerEventData _pointerEventData;
        internal TestSettings Settings = new TestSettings();
        internal bool ShouldBlockScreenInput(Vector2 position) { return PluginUiBlocked; }
        // RUNTIME_METHODS
    }
    internal class CameraController
    {
        public bool IsSelected = true, IsActive = false;
        public object CameraManager = new object();
        public Transform CameraTransform = new Transform();
        public string Name = "FreeCamera";
        public virtual void OnSelected() { }
    }
    internal sealed class FreeCameraController : CameraController
    {
        private readonly FreeCameraRuntime _runtime;
        internal float _yaw, _pitch, _roll, _targetYaw, _targetPitch, _targetRoll;
        internal bool _lookSmoothingActive = false;
        internal int _lastCameraRenderFrame = 0;
        internal Vector3 _smoothedMoveVelocity = Vector3.zero;
        internal float _currentFov = 60f, AppliedFov = 60f, _pendingFovScroll = 0f;
        internal bool _leftPressAccepted = false, _leftDragging = false;
        private Vector2 _leftPressPosition, _lastMousePosition;
        internal int CancelledLooks = 0, PickCount = 0, ClearFocusCount = 0;
        internal Vector2 LookDelta;
        internal FreeCameraController(FreeCameraRuntime runtime) { _runtime = runtime; }
        private void ResetAutoFovReference() { }
        private void ResetFocusTrackingState() { }
        private void SetAppliedFov(float fov) { AppliedFov = fov; }
        private void UpdateCursor() { }
        private void ResetInputSmoothing() { ResetPointerState(); CancelLookSmoothing(); }
        private void ProcessMovement(float dt, bool allowed) { }
        private void UpdateSmoothedLook(float dt) { }
        private void UpdateUnlockedFocalPosition() { }
        private void ClearFocusTarget(bool notify) { ClearFocusCount++; }
        private void ApplyLookDelta(Vector2 delta) { LookDelta = LookDelta + delta; }
        private void CancelLookSmoothing() { CancelledLooks++; SyncLookAnglesFromCamera(); }
        private void TrySelectFocusTarget(Vector2 position) { PickCount++; }
        internal void SyncWithoutEntry() { SyncLookAnglesFromCamera(); }
        // CONTROLLER_METHODS
    }
}

namespace SP2FreeCamera.Tests
{
    public static class CameraInputTests
    {
        private sealed class TargetBox : MonoBehaviour, ITargetBox { }
        private sealed class Button : MonoBehaviour, IPointerDownHandler { }
        private static readonly GraphicRaycaster Graphics = new GraphicRaycaster();
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        private static void Near(float actual, float expected, string message)
        {
            Require(Math.Abs(actual - expected) < 0.0001f, message);
        }
        private static FreeCameraRuntime Reset()
        {
            Assets.Scripts.Game.Instance = new Assets.Scripts.Game();
            EventSystem.current = new EventSystem();
            Application.isFocused = true;
            Frame(100f);
            return new FreeCameraRuntime();
        }
        private static void Frame(float x, bool down = false, bool held = false, bool up = false, float scroll = 0f)
        {
            Input.mousePosition = new Vector3(x, 100f);
            Input.LeftDown = down; Input.LeftHeld = held; Input.LeftUp = up; Input.MiddleDown = false;
            Input.mouseScrollDelta = new Vector2(0f, scroll);
        }
        private static void Hits(params GameObject[] hits)
        {
            EventSystem.current.Hits.Clear();
            foreach (GameObject hit in hits)
                EventSystem.current.Hits.Add(new RaycastResult { gameObject = hit, module = Graphics });
        }
        private static void CheckBoth(FreeCameraRuntime runtime, bool expected, string message)
        {
            Require(runtime.CanProcessPointerInput() == expected, message + " (drag/wheel)");
            Require(runtime.CanProcessFocusSelectionInput() == expected, message + " (middle pick)");
        }
        public static string[] RunAll()
        {
            var passed = new List<string>();
            FreeCameraRuntime runtime = Reset();
            foreach (float roll in new[] { 0f, 40f, 180f, 270f, 359f })
            foreach (float pitch in new[] { 0f, 25f, 335f, 89.95f, 90f, 270f })
            {
                var controller = new FreeCameraController(runtime);
                controller.CameraTransform.position = new Vector3(15f, 230f, -80f);
                controller.CameraTransform.rotation = Quaternion.Euler(new Vector3(pitch, 123f, roll));
                controller._currentFov = 42f;
                controller.OnSelected();
                Vector3 euler = controller.CameraTransform.rotation.eulerAngles;
                Near(euler.z, 0f, "Entry must immediately remove camera roll before any frame/input");
                Near(euler.x, pitch, "Entry must retain exact pitch, including vertical views");
                Near(euler.y, 123f, "Entry must retain heading");
                Near(controller._roll, 0f, "Current roll state");
                Near(controller._targetRoll, 0f, "Smoothed roll target");
                Near(controller.CameraTransform.position.x, 15f, "Position x unchanged");
                Near(controller.CameraTransform.position.y, 230f, "Position y unchanged");
                Near(controller.CameraTransform.position.z, -80f, "Position z unchanged");
                Near(controller.AppliedFov, 42f, "FOV unchanged");
                Require(!controller._lookSmoothingActive, "No inherited roll smoothing on entry");
            }
            passed.Add("Entry resets applied/current/target roll immediately while retaining position, pitch, heading and FOV");

            var sync = new FreeCameraController(runtime);
            sync.CameraTransform.rotation = Quaternion.Euler(new Vector3(25f, 50f, 315f));
            sync.SyncWithoutEntry();
            Near(sync._roll, -45f, "Ordinary angle sync must not alter the camera");
            Near(sync.CameraTransform.rotation.eulerAngles.z, 315f, "Roll reset is entry-only");
            passed.Add("Ordinary focus/look synchronization is unchanged outside camera entry");

            GameObject scene = new GameObject(new FlightScreenInputScript());
            GameObject sceneChild = new GameObject(parent: scene);
            GameObject target = new GameObject(parent: new GameObject(new TargetBox()));
            GameObject button = new GameObject(new Button());
            foreach (bool interacting in new[] { false, true })
            foreach (bool gameHover in new[] { false, true })
            {
                Assets.Scripts.Game.Instance.UIInfo.IsInteracting = interacting;
                runtime._flightUi.IsPointerInsideGameView = gameHover;
                Hits(target, scene); CheckBoth(runtime, true, "Target box child must pass regardless of stale UI flags");
                Hits(sceneChild); CheckBoth(runtime, true, "Fresh scene hit must pass immediately after leaving target box");
            }
            passed.Add("Current target-box and scene raycasts override delayed native hover/interaction flags");

            runtime = Reset();
            var drag = new FreeCameraController(runtime);
            Hits(scene); Frame(100f, down: true, held: true); drag.ProcessFrame(1f / 60f);
            Frame(102f, held: true); drag.ProcessFrame(1f / 60f);
            Require(!drag._leftDragging, "Original drag threshold is preserved");
            Hits(target, scene); runtime._flightUi.IsPointerInsideGameView = false;
            Assets.Scripts.Game.Instance.UIInfo.IsInteracting = true;
            Frame(110f, held: true); drag.ProcessFrame(1f / 60f);
            Hits(sceneChild); Frame(120f, held: true); drag.ProcessFrame(1f / 60f);
            Hits(target); Frame(130f, held: true); drag.ProcessFrame(1f / 60f);
            Require(drag._leftPressAccepted && drag._leftDragging, "Crossing target boundaries must keep the original press");
            Near(drag.LookDelta.x, 28f, "Drag deltas must remain continuous through each boundary");
            Require(drag.CancelledLooks == 0 && drag.ClearFocusCount == 1, "No hover-triggered cancel or repeated focus clear");
            Frame(130f, up: true); drag.ProcessFrame(1f / 60f);
            Frame(140f); drag.ProcessFrame(1f / 60f);
            Require(!drag._leftPressAccepted && !drag._leftDragging, "Release ends capture normally");
            Near(drag.LookDelta.x, 28f, "No movement after release");
            passed.Add("Accepted drag remains continuous across repeated scene/target-box transitions and ends on release");

            var targetStart = new FreeCameraController(runtime);
            Frame(100f, down: true, held: true); targetStart.ProcessFrame(1f / 60f);
            Frame(110f, held: true, scroll: 1f); targetStart.ProcessFrame(1f / 60f);
            Require(targetStart._leftDragging, "A drag may start over a target box");
            Near(targetStart._pendingFovScroll, 1f, "Wheel over target must reach the shared manual/auto FOV path");
            Hits(sceneChild); Frame(120f, held: true, scroll: -0.5f); targetStart.ProcessFrame(1f / 60f);
            Near(targetStart._pendingFovScroll, 0.5f, "Wheel immediately after target hover must also pass");
            passed.Add("Dragging can start over a target box; wheel input reaches the shared zoom path during and after hover");

            runtime = Reset();
            Hits(button, target, scene); CheckBoth(runtime, false, "Foreground unrelated UI must not leak to a target behind it");
            Hits(new GameObject(new Button(), scene), scene); CheckBoth(runtime, false, "Nearest child button wins over game-view ancestor");
            Hits(target, button, scene); CheckBoth(runtime, true, "Only the frontmost graphic hit determines ownership");
            passed.Add("Foreground buttons/panels retain input, including controls nested under the game-view surface");

            var guards = new Action<FreeCameraRuntime>[] {
                r => r._active = false, r => r._menuVisible = true, r => r.PluginUiBlocked = true,
                r => Application.isFocused = false, r => Assets.Scripts.Game.Instance = null,
                r => Assets.Scripts.Game.Instance.UserInterface = null,
                r => Assets.Scripts.Game.Instance.UserInterface.AnyDialogsOpen = true,
                r => Assets.Scripts.Game.Instance.UserInterface.IsTextInputFocused = true,
                r => Assets.Scripts.Game.Instance.UserInterface.ConsoleOpen = true,
                r => r._flightUi = null, r => Input.mousePosition = new Vector3(-1f, 100f),
                r => Input.mousePosition = new Vector3(1921f, 100f),
                r => Input.mousePosition = new Vector3(100f, -1f),
                r => Input.mousePosition = new Vector3(100f, 1081f)
            };
            foreach (Action<FreeCameraRuntime> guard in guards)
            {
                runtime = Reset(); Hits(target, scene); guard(runtime);
                CheckBoth(runtime, false, "Modal/plugin/text/console/focus/viewport safety gates must override target-box hit");
            }
            passed.Add("Menus, plugin captures, dialogs, text input, console, focus loss and viewport boundaries remain protected");

            runtime = Reset(); drag = new FreeCameraController(runtime);
            Hits(scene); Frame(100f, down: true, held: true); drag.ProcessFrame(1f / 60f);
            Frame(110f, held: true); drag.ProcessFrame(1f / 60f);
            Hits(button, scene); Frame(120f, held: true, scroll: 1f); drag.ProcessFrame(1f / 60f);
            Require(!drag._leftPressAccepted && !drag._leftDragging, "A real UI boundary still cancels the drag");
            Near(drag._pendingFovScroll, 0f, "UI wheel input must not zoom the camera");
            Hits(scene); Frame(130f, held: true); drag.ProcessFrame(1f / 60f);
            Near(drag.LookDelta.x, 10f, "No resumed or jumping drag after a real UI cancellation");
            passed.Add("Crossing real UI still cancels capture and does not resume until a new accepted press");

            runtime = Reset(); Hits(target); CheckBoth(runtime, true, "Initial event system");
            PointerEventData previous = EventSystem.current.LastEvent;
            CheckBoth(runtime, true, "Repeated event system");
            Require(ReferenceEquals(previous, EventSystem.current.LastEvent), "Pointer data is reused across frames");
            EventSystem.current = new EventSystem(); Hits(sceneChild); CheckBoth(runtime, true, "Recreated event system");
            Require(!ReferenceEquals(previous, EventSystem.current.LastEvent), "Scene switch must replace stale pointer data");
            EventSystem.current.Hits.Clear();
            EventSystem.current.Hits.Add(new RaycastResult { gameObject = button, module = new PhysicsRaycaster() });
            CheckBoth(runtime, true, "Physics results are not UI blockers");
            Assets.Scripts.Game.Instance.UIInfo.IsInteracting = true;
            CheckBoth(runtime, false, "Unknown hit retains the conservative UI interaction fallback");
            EventSystem.current = null; CheckBoth(runtime, false, "Missing event system cannot bypass interaction protection");
            Assets.Scripts.Game.Instance.UIInfo.IsInteracting = false;
            runtime._flightUi.Visible = false;
            runtime._flightUi.IsPointerInsideGameView = false;
            CheckBoth(runtime, true, "Hidden HUD still permits camera input");
            passed.Add("Pointer data reuse, event-system replacement, physics-hit filtering and conservative hidden/unknown UI fallback");

            runtime = Reset(); Hits(target); var pick = new FreeCameraController(runtime);
            Input.MiddleDown = true; pick.ProcessFrame(1f / 60f);
            Require(pick.PickCount == 1, "Existing middle-click target picking remains available");
            runtime._menuVisible = true; pick.ProcessFrame(1f / 60f);
            Require(pick.PickCount == 1, "Middle-click still respects menu protection");
            passed.Add("Middle-click picking uses the same target-transparent boundary without bypassing menus");
            return passed.ToArray();
        }
    }
}
