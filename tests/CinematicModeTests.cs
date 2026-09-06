// Used with CameraInputTests.cs and extracted production methods. Rewired/UI
// doubles deliberately model only the APIs touched by keyboard capture. Native
// input timing and actual rendering must still be checked in the game.
using System;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

namespace BepInEx.Logging
{
    public sealed class ManualLogSource { public void LogWarning(object message) { } }
}

namespace Assets.Scripts.Input
{
    public static class InputWrapper
    {
        public static event EventHandler OnControlsChanged;
        public static int Subscribers { get { return OnControlsChanged == null ? 0 : OnControlsChanged.GetInvocationList().Length; } }
        public static void ChangeControls() { if (OnControlsChanged != null) OnControlsChanged(null, EventArgs.Empty); }
    }
}

namespace Rewired
{
    public enum ControllerType { Keyboard, Joystick }
    public sealed class ActionElementMap
    {
        public KeyCode keyCode;
        public bool hasModifiers, FailWrites;
        private bool _enabled = true;
        public bool enabled
        {
            get { return _enabled; }
            set { if (FailWrites) throw new InvalidOperationException("Stale test map"); _enabled = value; }
        }
    }
    public sealed class ControllerMap
    {
        public int categoryId;
        public ControllerType controllerType;
        public bool enabled = true;
        public readonly List<ActionElementMap> Elements = new List<ActionElementMap>();
        public void GetElementMaps(bool enabledOnly, List<ActionElementMap> results)
        {
            foreach (ActionElementMap item in Elements) if (!enabledOnly || item.enabled) results.Add(item);
        }
    }
    public sealed class InputMapCategory { public string name; }
    public sealed class TestMapping
    {
        public int GetMapCategoryId(string name) { return name == "Craft" ? 1 : name == "Character" ? 2 : 3; }
        public InputMapCategory GetMapCategory(int id) { return new InputMapCategory { name = id == 1 ? "Craft" : id == 2 ? "Character" : "World" }; }
    }
    public sealed class TestMaps
    {
        public readonly List<ControllerMap> Items = new List<ControllerMap>();
        public void GetAllMapsInCategory(int category, ControllerType type, List<ControllerMap> results)
        {
            foreach (ControllerMap map in Items) if (map.categoryId == category && map.controllerType == type) results.Add(map);
        }
    }
    public sealed class TestControllers { public TestMaps maps = new TestMaps(); }
    public sealed class Player { public TestControllers controllers = new TestControllers(); }
    public sealed class TestPlayers { public Player Player = new Player(); public Player GetPlayer(int id) { return Player; } }
    public static class ReInput
    {
        public static bool isReady = true;
        public static TestPlayers players = new TestPlayers();
        public static TestMapping mapping = new TestMapping();
        public static event Action InitializedEvent, PreShutDownEvent;
        public static void Initialize() { isReady = true; if (InitializedEvent != null) InitializedEvent(); }
        public static void ShutDown() { if (PreShutDownEvent != null) PreShutDownEvent(); isReady = false; }
    }
}

namespace SP2FreeCamera
{
    internal sealed class ModeTestEntry<T>
    {
        private T _value;
        private readonly Action<T> _save;
        public int Writes;
        public ModeTestEntry(T value, Action<T> save = null) { _value = value; _save = save; }
        public T Value { get { return _value; } set { _value = value; Writes++; if (_save != null) _save(value); } }
    }
    internal sealed class ModeTestConfig
    {
        // In-memory model of Config.Bind preserving an already saved value.
        public readonly Dictionary<string, object> Values = new Dictionary<string, object>();
        public ModeTestEntry<T> Bind<T>(string section, string key, T fallback, string description)
        {
            string path = section + "." + key;
            object saved;
            return new ModeTestEntry<T>(Values.TryGetValue(path, out saved) ? (T)saved : fallback, v => Values[path] = v);
        }
    }
    internal sealed partial class Plugin
    {
        public const float DefaultNormalSpeed = 200f, DefaultFastSpeed = 2000f;
        public const float DefaultNormalAcceleration = 80f, DefaultFastAcceleration = 800f, MaximumMovementAcceleration = 1000000f;
        public ModeTestEntry<float> NormalSpeed = new ModeTestEntry<float>(DefaultNormalSpeed), FastSpeed = new ModeTestEntry<float>(DefaultFastSpeed);
        public ModeTestEntry<float> NormalAcceleration = new ModeTestEntry<float>(DefaultNormalAcceleration), FastAcceleration = new ModeTestEntry<float>(DefaultFastAcceleration);
        public ModeTestEntry<float> MovementSmoothingTime = new ModeTestEntry<float>(0.08f);
        public ModeTestEntry<KeyCode> MoveForwardKey = new ModeTestEntry<KeyCode>(KeyCode.W), MoveBackwardKey = new ModeTestEntry<KeyCode>(KeyCode.S);
        public ModeTestEntry<KeyCode> MoveLeftKey = new ModeTestEntry<KeyCode>(KeyCode.A), MoveRightKey = new ModeTestEntry<KeyCode>(KeyCode.D);
        public ModeTestEntry<KeyCode> MoveUpKey = new ModeTestEntry<KeyCode>(KeyCode.E), MoveDownKey = new ModeTestEntry<KeyCode>(KeyCode.Q);
        public ModeTestEntry<KeyCode> ToggleAutoFovKey = new ModeTestEntry<KeyCode>(KeyCode.Equals), ToggleSpeedKey = new ModeTestEntry<KeyCode>(KeyCode.Keypad5);
        public ModeTestEntry<KeyCode> ToggleCinematicModeKey;
        public ModeTestEntry<bool> CinematicModeEnabled;
        public ModeTestConfig Config;
        public Plugin(ModeTestConfig config = null)
        {
            Config = config ?? new ModeTestConfig();
            // BIND_CinematicModeEnabled
            // BIND_ToggleCinematicModeKey
        }
        private ModeTestEntry<KeyCode> BindKey(string key, KeyCode value, string description) { return Config.Bind("Keys", key, value, description); }
    }
    internal sealed class FreeCameraQuickMenu
    {
        public Vector3 MovementAxes;
        public void ResetMovementInput() { MovementAxes = Vector3.zero; }
    }
    internal sealed partial class FreeCameraRuntime
    {
        internal bool _lastCinematicModeEnabled, _shuttingDown = false, _fastMode = false;
        internal FreeCameraController _freeCameraController;
        internal FreeCameraQuickMenu _quickMenu = new FreeCameraQuickMenu();
        internal FreeCameraKeyboardCapture _keyboardCapture = new FreeCameraKeyboardCapture();
        internal int Notifications;
        internal bool FastMode { get { return _fastMode; } }
        internal FreeCameraRuntime(Plugin settings = null)
        {
            if (settings != null) _settings = settings;
            _lastCinematicModeEnabled = CinematicModeEnabled;
        }
        private void Notify(string message, bool warning) { Notifications++; }
        internal void TestHotkey() { ProcessCinematicModeHotkey(CanProcessKeyboardInput()); }
        internal void TestReconcile() { ApplyCinematicModeIfChanged(); }
        internal void TestCapture() { RefreshKeyboardCapture(); }
    }
}

namespace SP2FreeCamera.Tests
{
    public static class CinematicModeTests
    {
        private static FreeCameraRuntime _previous;
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static FreeCameraRuntime Reset(Plugin settings = null)
        {
            if (_previous != null) _previous._keyboardCapture.Restore();
            ReInput.players = new TestPlayers(); ReInput.isReady = true; Time.unscaledTime = 0f;
            Assets.Scripts.Game.Instance = new Assets.Scripts.Game();
            UnityEngine.EventSystems.EventSystem.current = null;
            Application.isFocused = true;
            Input.HeldKeys.Clear(); Input.DownKeys.Clear();
            Input.LeftDown = Input.LeftHeld = Input.LeftUp = Input.MiddleDown = false;
            Input.mousePosition = new Vector3(100f, 100f); Input.mouseScrollDelta = new Vector2();
            _previous = new FreeCameraRuntime(settings);
            return _previous;
        }
        private static ActionElementMap AddBinding(KeyCode key, int category = 1, bool enabled = true,
            bool modifiers = false, ControllerType type = ControllerType.Keyboard)
        {
            var binding = new ActionElementMap { keyCode = key, enabled = enabled, hasModifiers = modifiers };
            var map = new ControllerMap { categoryId = category, controllerType = type };
            map.Elements.Add(binding); ReInput.players.Player.controllers.maps.Items.Add(map);
            return binding;
        }
        public static string[] RunAll()
        {
            var passed = new List<string>();
            FreeCameraRuntime runtime = Reset();
            Require(runtime.CinematicModeEnabled && runtime.Settings.ToggleCinematicModeKey.Value == KeyCode.Alpha0, "Production defaults must be enabled and main keyboard 0");
            Input.DownKeys.Add(KeyCode.Keypad0); runtime.TestHotkey();
            Require(runtime.CinematicModeEnabled, "Keypad0 must not toggle");
            Input.DownKeys.Clear(); Input.DownKeys.Add(KeyCode.Alpha0); runtime.TestHotkey();
            Require(!runtime.CinematicModeEnabled, "Main 0 must disable cinematic mode");
            Input.DownKeys.Clear(); runtime.TestHotkey();
            Require(!runtime.CinematicModeEnabled, "A held key without a new down event must not repeat");
            Input.DownKeys.Add(KeyCode.Alpha0); runtime.TestHotkey();
            Require(runtime.CinematicModeEnabled, "Next main 0 press must enable cinematic mode");
            runtime.Settings.ToggleCinematicModeKey.Value = KeyCode.None; runtime.TestHotkey();
            Require(runtime.CinematicModeEnabled, "None disables the hotkey");
            passed.Add("Production bindings default ON and Alpha0; Keypad0, held keys and None do not toggle");

            var guards = new Action<FreeCameraRuntime>[] {
                r => r._active = false, r => r._menuVisible = true, r => Application.isFocused = false,
                r => Assets.Scripts.Game.Instance = null, r => Assets.Scripts.Game.Instance.UserInterface = null,
                r => Assets.Scripts.Game.Instance.UserInterface.AnyDialogsOpen = true,
                r => Assets.Scripts.Game.Instance.UserInterface.IsTextInputFocused = true,
                r => Assets.Scripts.Game.Instance.UserInterface.ConsoleOpen = true
            };
            foreach (Action<FreeCameraRuntime> guard in guards)
            {
                runtime = Reset(); guard(runtime); Input.DownKeys.Add(KeyCode.Alpha0); runtime.TestHotkey();
                Require(runtime.CinematicModeEnabled, "Inactive/menu/dialog/text/console/focus guards must prevent toggles");
            }
            passed.Add("Mode hotkey respects free-camera, full-menu, dialog, text/chat, console and application-focus gates");

            runtime = Reset(); var camera = new FreeCameraController(runtime);
            camera._smoothedMoveVelocity = new Vector3(30f, 40f, 50f); camera._leftPressAccepted = camera._leftDragging = true;
            camera._lookSmoothingActive = true; camera._pendingFovScroll = 0.75f;
            camera.CameraTransform.position = new Vector3(15f, 230f, -80f);
            camera.CameraTransform.rotation = Quaternion.Euler(new Vector3(20f, 30f, 0f));
            runtime._quickMenu.MovementAxes = new Vector3(1f, 1f, 1f);
            runtime.SetCinematicModeEnabled(false);
            Require(camera._smoothedMoveVelocity.sqrMagnitude == 0f && runtime._quickMenu.MovementAxes.sqrMagnitude == 0f, "Turning off must clear inertia and queued buttons synchronously");
            Require(camera._leftDragging && camera._leftPressAccepted && camera._lookSmoothingActive && camera._pendingFovScroll == 0.75f, "Toggle must not reset drag, look smoothing or pending zoom");
            Require(camera.ClearFocusCount == 0 && camera.CancelledLooks == 0 && camera.CameraTransform.rotation.eulerAngles.y == 30f, "Toggle must retain target and orientation");
            Require(camera.CameraTransform.position.y == 230f && camera.AppliedFov == 60f, "Toggle must not move or zoom the camera");
            int writes = runtime.Settings.CinematicModeEnabled.Writes;
            runtime.SetCinematicModeEnabled(false);
            Require(runtime.Settings.CinematicModeEnabled.Writes == writes && runtime.Notifications == 1, "Repeated UI draws must not save or notify unchanged settings");
            passed.Add("Mode change immediately clears only translation; drag, orientation, focus and FOV remain intact");

            runtime = Reset(); camera = new FreeCameraController(runtime); runtime.SetCinematicModeEnabled(false);
            Input.HeldKeys.Add(KeyCode.W); Input.HeldKeys.Add(KeyCode.E); Input.HeldKeys.Add(KeyCode.A);
            Input.DownKeys.Add(KeyCode.Keypad5); runtime._quickMenu.MovementAxes = new Vector3(1f, 1f, 1f);
            camera._smoothedMoveVelocity = new Vector3(100f, 200f, 300f);
            for (int frame = 0; frame < 120; frame++) camera.ProcessFrame(1f / 60f);
            Require(camera.CameraTransform.position.sqrMagnitude == 0f && camera._smoothedMoveVelocity.sqrMagnitude == 0f, "General mode must block keys, queued quick buttons and stale inertia across frames");
            Require(runtime.QuickMovementAxes.sqrMagnitude == 0f, "Runtime must reject quick axes even if the UI has stale input");
            runtime.ToggleFastMode(); Require(!runtime.FastMode, "Speed toggle is inactive in general mode");
            Input.LeftDown = Input.LeftHeld = true; camera.ProcessFrame(1f / 60f); Input.LeftDown = false;
            Input.mousePosition = new Vector3(110f, 100f); Input.mouseScrollDelta = new Vector2(0f, 1f); Input.MiddleDown = true;
            camera.ProcessFrame(1f / 60f);
            Require(camera._leftDragging && camera.LookDelta.x == 10f && camera.PickCount == 1 && camera._pendingFovScroll == 1f, "General mode still processes look, focus selection and shared manual/auto FOV input");
            passed.Add("General mode remains stationary for 120 frames while look, focus and wheel paths continue");

            runtime = Reset(); camera = new FreeCameraController(runtime); runtime.SetCinematicModeEnabled(false);
            runtime._quickMenu.MovementAxes = new Vector3(1f, 0f, 0f); runtime.SetCinematicModeEnabled(true);
            Input.HeldKeys.Add(KeyCode.W); camera.ProcessFrame(1f / 60f);
            Require(camera.CameraTransform.position.z > 0f && camera.CameraTransform.position.x == 0f, "Re-enable must resume keyboard movement without stale quick input");
            Require(camera._smoothedMoveVelocity.magnitude <= Plugin.DefaultNormalAcceleration / 60f + 0.001f, "Movement resumes from rest under the configured acceleration limit");
            runtime.ToggleFastMode(); Require(runtime.FastMode, "Speed toggle resumes in cinematic mode");
            passed.Add("Re-enabling resumes acceleration from rest and restores normal/fast speed control");

            runtime = Reset();
            var w = AddBinding(KeyCode.W); var q = AddBinding(KeyCode.Q, enabled: false);
            var a = AddBinding(KeyCode.A, category: 2); var shifted = AddBinding(KeyCode.E, modifiers: true);
            var world = AddBinding(KeyCode.W, category: 3); var joystick = AddBinding(KeyCode.W, type: ControllerType.Joystick);
            var fov = AddBinding(KeyCode.Equals); var mode = AddBinding(KeyCode.Alpha0);
            runtime.TestCapture();
            Require(!w.enabled && !q.enabled && !a.enabled && !fov.enabled && !mode.enabled, "Cinematic capture owns unmodified movement and toggle bindings");
            Require(shifted.enabled && world.enabled && joystick.enabled, "Modified keys, other categories and joystick maps remain untouched");
            for (int cycle = 0; cycle < 10; cycle++)
            {
                runtime.SetCinematicModeEnabled(false);
                Require(w.enabled && !q.enabled && a.enabled && !fov.enabled && !mode.enabled, "General mode must restore exact original movement states without releasing the mode keys");
                Require(Assets.Scripts.Input.InputWrapper.Subscribers == 1, "Recapture must not leak event subscriptions");
                runtime.SetCinematicModeEnabled(true); Require(!w.enabled && !a.enabled, "Cinematic mode must recapture both Craft and Character");
            }
            foreach (ControllerMap map in ReInput.players.Player.controllers.maps.Items) Require(map.enabled, "Capture must never disable a whole map");
            runtime._keyboardCapture.Restore();
            Require(w.enabled && !q.enabled && a.enabled && fov.enabled && mode.enabled && Assets.Scripts.Input.InputWrapper.Subscribers == 0, "Exit must restore exact states and remove subscriptions");
            passed.Add("Real capture restores exact Craft/Character states over repeated switches, retains toggles and leaves other maps alone");

            runtime = Reset(); runtime.SetCinematicModeEnabled(false);
            w = AddBinding(KeyCode.W); fov = AddBinding(KeyCode.Equals);
            Assets.Scripts.Input.InputWrapper.ChangeControls();
            Require(w.enabled && !fov.enabled, "Controls reload in general mode must not steal movement keys");
            runtime.SetCinematicModeEnabled(true); Require(!w.enabled, "Cinematic mode captures recreated maps immediately");
            w.enabled = true; Time.unscaledTime = 2f; runtime._keyboardCapture.Refresh();
            Require(!w.enabled, "Periodic reconciliation retains cinematic ownership");
            runtime.SetCinematicModeEnabled(false); fov.enabled = true; Time.unscaledTime = 4f; runtime._keyboardCapture.Refresh();
            Require(w.enabled && !fov.enabled, "Periodic reconciliation in general mode retains only toggle ownership");
            passed.Add("Controls-change events and periodic reconciliation honor the current mode after map replacement");

            runtime = Reset(); camera = new FreeCameraController(runtime); w = AddBinding(KeyCode.W); runtime.TestCapture();
            camera._smoothedMoveVelocity = new Vector3(100f, 0f, 0f);
            runtime.Settings.CinematicModeEnabled.Value = false; runtime.TestReconcile();
            Require(w.enabled && camera._smoothedMoveVelocity.sqrMagnitude == 0f, "An externally reloaded configuration must reconcile movement and bindings on Update");
            passed.Add("Configuration reload is reconciled on the main update path without relying on UI events");

            runtime = Reset(); runtime.SetCinematicModeEnabled(false);
            ModeTestConfig saved = runtime.Settings.Config;
            runtime = Reset(new Plugin(saved)); runtime.TestCapture();
            Require(!runtime.CinematicModeEnabled, "Rebinding configuration for a new session must preserve saved false instead of reapplying the true default");
            runtime._active = false; runtime.TestCapture(); runtime.SetCinematicModeEnabled(true);
            Require(!runtime._keyboardCapture.IsCaptured, "Preselecting mode outside freecam must not capture game keys");
            runtime = Reset(new Plugin(saved)); Require(runtime.CinematicModeEnabled, "Saved true must also survive a new session");
            passed.Add("Saved mode values survive re-binding/session entry in the config model; inactive menu changes never capture keys");

            runtime = Reset(); ReInput.isReady = false; w = AddBinding(KeyCode.W); runtime.TestCapture();
            Require(w.enabled, "Capture waits for Rewired readiness");
            ReInput.Initialize(); Require(!w.enabled, "Initialization event captures the active cinematic key set");
            ReInput.ShutDown(); Require(w.enabled && !runtime._keyboardCapture.IsCaptured, "Rewired shutdown restores before maps disappear");
            ReInput.isReady = true; runtime.TestCapture();
            var stale = AddBinding(KeyCode.E); Assets.Scripts.Input.InputWrapper.ChangeControls(); stale.FailWrites = true;
            runtime.SetCinematicModeEnabled(false); Require(w.enabled, "A stale map must not prevent the remaining movement bindings being restored");
            runtime._keyboardCapture.Restore();
            passed.Add("Delayed Rewired initialization, shutdown and stale bindings preserve best-effort restoration");
            return passed.ToArray();
        }
    }
}
