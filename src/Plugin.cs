using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SP2FreeCamera
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "local.sp2.freecamera";
        public const string PluginName = "SP2 Free Camera";
        public const string PluginVersion = "0.6.11";

        internal const float DefaultNormalSpeed = 200f;
        internal const float DefaultFastSpeed = 2000f;
        internal const float DefaultNormalAcceleration = 80f;
        internal const float DefaultFastAcceleration = 800f;
        internal const float MaximumMovementAcceleration = 1000000f;
        internal const float DefaultFovSmoothingTime = 0.12f;

        private const int CurrentKeyBindingRevision = 2;
        private const int CurrentMovementSpeedRevision = 1;
        private const float LegacyNormalSpeed = 20f;
        private const float LegacyFastSpeed = 200f;

        private Harmony _harmony;
        private FreeCameraRuntime _runtime;
        private bool _applicationQuitting;

        internal static ManualLogSource Log { get; private set; }

        internal ConfigEntry<string> Language { get; private set; }

        internal ConfigEntry<bool> Enabled { get; private set; }

        internal ConfigEntry<bool> FirstPersonFocusEnabled { get; private set; }

        internal ConfigEntry<bool> CinematicModeEnabled { get; private set; }

        internal ConfigEntry<float> NormalSpeed { get; private set; }

        internal ConfigEntry<float> FastSpeed { get; private set; }

        internal ConfigEntry<float> NormalAcceleration { get; private set; }

        internal ConfigEntry<float> FastAcceleration { get; private set; }

        internal ConfigEntry<float> MovementSmoothingTime { get; private set; }

        internal ConfigEntry<float> LookSensitivity { get; private set; }

        internal ConfigEntry<float> LookSmoothingTime { get; private set; }

        internal ConfigEntry<bool> InvertLookY { get; private set; }

        internal ConfigEntry<bool> ScaleLookSensitivityWithFov { get; private set; }

        internal ConfigEntry<float> DragThresholdPixels { get; private set; }

        internal ConfigEntry<float> FovScrollSensitivity { get; private set; }

        internal ConfigEntry<float> FovSmoothingTime { get; private set; }

        internal ConfigEntry<float> MaximumFov { get; private set; }

        internal ConfigEntry<float> FocusMaximumDistance { get; private set; }

        internal ConfigEntry<float> FocusSmoothingTime { get; private set; }

        internal ConfigEntry<bool> AutoHideUi { get; private set; }

        internal ConfigEntry<bool> ShowStatusMessages { get; private set; }

        internal ConfigEntry<bool> ShowQuickMenu { get; private set; }

        internal ConfigEntry<float> QuickLauncherNormalizedX { get; private set; }

        internal ConfigEntry<float> QuickLauncherNormalizedY { get; private set; }

        internal ConfigEntry<KeyCode> ToggleCameraKey { get; private set; }

        internal ConfigEntry<KeyCode> ToggleMenuKey { get; private set; }

        internal ConfigEntry<KeyCode> ToggleCinematicModeKey { get; private set; }

        internal ConfigEntry<KeyCode> MoveForwardKey { get; private set; }

        internal ConfigEntry<KeyCode> MoveBackwardKey { get; private set; }

        internal ConfigEntry<KeyCode> MoveLeftKey { get; private set; }

        internal ConfigEntry<KeyCode> MoveRightKey { get; private set; }

        internal ConfigEntry<KeyCode> MoveUpKey { get; private set; }

        internal ConfigEntry<KeyCode> MoveDownKey { get; private set; }

        internal ConfigEntry<KeyCode> ToggleSpeedKey { get; private set; }

        internal ConfigEntry<KeyCode> LockSelfKey { get; private set; }

        internal ConfigEntry<KeyCode> FocusSelectedTargetKey { get; private set; }

        internal ConfigEntry<KeyCode> ToggleAutoFovKey { get; private set; }

        private ConfigEntry<int> KeyBindingRevision { get; set; }

        private ConfigEntry<int> MovementSpeedRevision { get; set; }

        private void Awake()
        {
            Log = Logger;
            BindConfiguration();

            _runtime = gameObject.AddComponent<FreeCameraRuntime>();
            _runtime.Initialize(this);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
            if (_runtime != null)
            {
                _runtime.PrepareForApplicationQuit();
            }
        }

        private void OnDestroy()
        {
            if (_runtime != null)
            {
                _runtime.Shutdown(!_applicationQuitting);
                _runtime = null;
            }

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }

            if (!_applicationQuitting)
            {
                FreeCameraGuiTheme.Release();
            }

            if (ReferenceEquals(Log, Logger))
            {
                Log = null;
            }
        }

        private void BindConfiguration()
        {
            Language = Config.Bind(
                "General",
                "Language",
                Localization.EnglishCode,
                "Interface language / 界面语言. Supported values / 支持值: English, SimplifiedChinese.");
            Localization.Initialize(Language);

            Enabled = Config.Bind(
                "General",
                "Enabled",
                true,
                Localization.Text("ConfigEnabled"));

            FirstPersonFocusEnabled = Config.Bind(
                "FirstPerson",
                "FocusEnabled",
                true,
                Localization.Text("ConfigFirstPersonFocus"));

            CinematicModeEnabled = Config.Bind(
                "Movement",
                "CinematicModeEnabled",
                true,
                Localization.Text("ConfigCinematicMode"));

            NormalSpeed = Config.Bind(
                "Movement",
                "NormalSpeed",
                DefaultNormalSpeed,
                Localization.Text("ConfigNormalSpeed"));
            FastSpeed = Config.Bind(
                "Movement",
                "FastSpeed",
                DefaultFastSpeed,
                Localization.Text("ConfigFastSpeed"));
            NormalAcceleration = Config.Bind(
                "Movement",
                "NormalAcceleration",
                DefaultNormalAcceleration,
                Localization.Text("ConfigNormalAcceleration"));
            FastAcceleration = Config.Bind(
                "Movement",
                "FastAcceleration",
                DefaultFastAcceleration,
                Localization.Text("ConfigFastAcceleration"));
            MovementSmoothingTime = Config.Bind(
                "Movement",
                "SmoothingTime",
                0.08f,
                Localization.Text("ConfigMovementSmoothing"));

            LookSensitivity = Config.Bind(
                "Camera",
                "LookSensitivity",
                0.15f,
                Localization.Text("ConfigLookSensitivity"));
            LookSmoothingTime = Config.Bind(
                "Camera",
                "LookSmoothingTime",
                0.06f,
                Localization.Text("ConfigLookSmoothing"));
            InvertLookY = Config.Bind(
                "Camera",
                "InvertLookY",
                false,
                Localization.Text("ConfigInvertLookY"));
            ScaleLookSensitivityWithFov = Config.Bind(
                "Camera",
                "ScaleLookSensitivityWithFov",
                true,
                Localization.Text("ConfigScaleLookWithFov"));
            DragThresholdPixels = Config.Bind(
                "Camera",
                "DragThresholdPixels",
                4f,
                Localization.Text("ConfigDragThreshold"));
            FovScrollSensitivity = Config.Bind(
                "Camera",
                "FovScrollSensitivity",
                1f,
                Localization.Text("ConfigFovSensitivity"));
            FovSmoothingTime = Config.Bind(
                "Camera",
                "FovSmoothingTime",
                DefaultFovSmoothingTime,
                Localization.Text("ConfigFovSmoothing"));
            MaximumFov = Config.Bind(
                "Camera",
                "MaximumFov",
                120f,
                Localization.Text("ConfigMaximumFov"));

            FocusMaximumDistance = Config.Bind(
                "Focus",
                "MaximumDistance",
                100000f,
                Localization.Text("ConfigFocusDistance"));
            FocusSmoothingTime = Config.Bind(
                "Focus",
                "SmoothingTime",
                0.08f,
                Localization.Text("ConfigFocusSmoothing"));

            AutoHideUi = Config.Bind(
                "UI",
                "AutoHideOnEnter",
                false,
                Localization.Text("ConfigAutoHideUi"));
            ShowStatusMessages = Config.Bind(
                "UI",
                "ShowStatusMessages",
                true,
                Localization.Text("ConfigShowStatus"));
            ShowQuickMenu = Config.Bind(
                "UI",
                "ShowQuickMenu",
                true,
                Localization.Text("ConfigShowQuickMenu"));

            QuickLauncherNormalizedX = Config.Bind(
                "UI",
                "QuickLauncherNormalizedX",
                0.0087f,
                Localization.Text("ConfigQuickX"));
            QuickLauncherNormalizedY = Config.Bind(
                "UI",
                "QuickLauncherNormalizedY",
                0.0904f,
                Localization.Text("ConfigQuickY"));

            ToggleCameraKey = BindKey("ToggleCamera", KeyCode.Insert, Localization.Text("ConfigToggleCamera"));
            ToggleMenuKey = BindKey("ToggleMenu", KeyCode.ScrollLock, Localization.Text("ConfigToggleMenu"));
            MoveForwardKey = BindKey("MoveForward", KeyCode.W, Localization.Text("ConfigMoveForward"));
            MoveBackwardKey = BindKey("MoveBackward", KeyCode.S, Localization.Text("ConfigMoveBackward"));
            MoveLeftKey = BindKey("MoveLeft", KeyCode.A, Localization.Text("ConfigMoveLeft"));
            MoveRightKey = BindKey("MoveRight", KeyCode.D, Localization.Text("ConfigMoveRight"));
            MoveUpKey = BindKey("MoveUp", KeyCode.E, Localization.Text("ConfigMoveUp"));
            MoveDownKey = BindKey("MoveDown", KeyCode.Q, Localization.Text("ConfigMoveDown"));
            ToggleSpeedKey = BindKey("ToggleSpeed", KeyCode.Keypad5, Localization.Text("ConfigToggleSpeed"));
            LockSelfKey = BindKey("LockSelf", KeyCode.Backspace, Localization.Text("ConfigLockSelf"));
            FocusSelectedTargetKey = BindKey(
                "FocusSelectedTarget",
                KeyCode.Minus,
                Localization.Text("ConfigFocusSelectedTarget"));
            ToggleAutoFovKey = BindKey(
                "ToggleAutoFov",
                KeyCode.Equals,
                Localization.Text("ConfigToggleAutoFov"));
            ToggleCinematicModeKey = BindKey(
                "ToggleCinematicMode",
                KeyCode.Alpha0,
                Localization.Text("ConfigToggleCinematicMode"));

            KeyBindingRevision = Config.Bind(
                "Internal",
                "KeyBindingRevision",
                0,
                Localization.Text("ConfigKeyRevision"));
            MovementSpeedRevision = Config.Bind(
                "Internal",
                "MovementSpeedRevision",
                0,
                Localization.Text("ConfigMovementSpeedRevision"));
            MigrateLegacyKeyBindings();
            MigrateLegacyMovementSpeeds();
        }

        private void MigrateLegacyMovementSpeeds()
        {
            if (MovementSpeedRevision.Value >= CurrentMovementSpeedRevision)
            {
                return;
            }

            bool changed = false;
            bool saveOnConfigSet = Config.SaveOnConfigSet;
            try
            {
                Config.SaveOnConfigSet = false;

                if (NormalSpeed.Value == LegacyNormalSpeed)
                {
                    NormalSpeed.Value = DefaultNormalSpeed;
                    changed = true;
                }

                if (FastSpeed.Value == LegacyFastSpeed)
                {
                    FastSpeed.Value = DefaultFastSpeed;
                    changed = true;
                }

                MovementSpeedRevision.Value = CurrentMovementSpeedRevision;
                Config.Save();
            }
            finally
            {
                Config.SaveOnConfigSet = saveOnConfigSet;
            }

            if (changed)
            {
                Logger.LogInfo("Migrated legacy free-camera movement speed defaults.");
            }
        }

        private void MigrateLegacyKeyBindings()
        {
            if (KeyBindingRevision.Value >= CurrentKeyBindingRevision)
            {
                return;
            }

            bool changed = false;
            bool saveOnConfigSet = Config.SaveOnConfigSet;
            try
            {
                Config.SaveOnConfigSet = false;
                ConfigEntry<KeyCode>[] entries = GetConfiguredKeyEntries();

                if (KeyBindingRevision.Value < 1)
                {
                    // Move the legacy speed binding away from Home before assigning Home to forward.
                    changed |= MigrateLegacyKey(
                        ToggleSpeedKey,
                        KeyCode.Home,
                        KeyCode.Keypad5,
                        entries);
                    changed |= MigrateLegacyKey(
                        ToggleMenuKey,
                        KeyCode.F10,
                        KeyCode.ScrollLock,
                        entries);
                    changed |= MigrateLegacyKey(
                        MoveForwardKey,
                        KeyCode.UpArrow,
                        KeyCode.Home,
                        entries);
                    changed |= MigrateLegacyKey(
                        MoveBackwardKey,
                        KeyCode.DownArrow,
                        KeyCode.End,
                        entries);
                }

                if (KeyBindingRevision.Value < 2)
                {
                    changed |= MigrateLegacyKey(MoveForwardKey, KeyCode.Home, KeyCode.W, entries);
                    changed |= MigrateLegacyKey(MoveBackwardKey, KeyCode.End, KeyCode.S, entries);
                    changed |= MigrateLegacyKey(MoveLeftKey, KeyCode.LeftArrow, KeyCode.A, entries);
                    changed |= MigrateLegacyKey(MoveRightKey, KeyCode.RightArrow, KeyCode.D, entries);
                    changed |= MigrateLegacyKey(MoveUpKey, KeyCode.PageUp, KeyCode.E, entries);
                    changed |= MigrateLegacyKey(MoveDownKey, KeyCode.PageDown, KeyCode.Q, entries);
                    changed |= MigrateLegacyKey(LockSelfKey, KeyCode.Pause, KeyCode.Backspace, entries);
                }
                KeyBindingRevision.Value = CurrentKeyBindingRevision;
                Config.Save();
            }
            finally
            {
                Config.SaveOnConfigSet = saveOnConfigSet;
            }

            if (changed)
            {
                Logger.LogInfo(
                    "Migrated one or more legacy free-camera keys to the current binding layout.");
            }
        }

        private static bool MigrateLegacyKey(
            ConfigEntry<KeyCode> entry,
            KeyCode legacyDefault,
            KeyCode newDefault,
            ConfigEntry<KeyCode>[] entries)
        {
            if (entry.Value != legacyDefault)
            {
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                ConfigEntry<KeyCode> other = entries[i];
                if (!ReferenceEquals(other, entry) && other.Value == newDefault)
                {
                    Log.LogWarning(
                        "Skipped legacy key migration for " + entry.Definition.Key +
                        " because " + newDefault + " is already used by " +
                        other.Definition.Key + ".");
                    return false;
                }
            }

            entry.Value = newDefault;
            return true;
        }

        private ConfigEntry<KeyCode>[] GetConfiguredKeyEntries()
        {
            return new[]
            {
                ToggleCameraKey,
                ToggleMenuKey,
                ToggleCinematicModeKey,
                MoveForwardKey,
                MoveBackwardKey,
                MoveLeftKey,
                MoveRightKey,
                MoveUpKey,
                MoveDownKey,
                ToggleSpeedKey,
                LockSelfKey,
                FocusSelectedTargetKey,
                ToggleAutoFovKey
            };
        }

        private ConfigEntry<KeyCode> BindKey(string name, KeyCode defaultValue, string description)
        {
            return Config.Bind("Keys", name, defaultValue, description);
        }
    }
}
