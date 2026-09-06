using System;
using Assets.Scripts.Flight.Cameras;
using Assets.Scripts.Input.Events;
using HarmonyLib;
using UnityEngine;

namespace SP2FreeCamera
{
    [HarmonyPatch]
    internal static class FirstPersonFocusPatches
    {
        private static FirstPersonFocus Focus
        {
            get { return FreeCameraRuntime.Instance != null ? FreeCameraRuntime.Instance.FirstPersonFocus : null; }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractiveCameraController), "Update")]
        private static void BeforeNativeInput()
        {
            FirstPersonFocus focus = Focus;
            if (focus == null) return;
            try { focus.ProcessFrame(); }
            catch (Exception) { focus.DisableAfterError(); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InteractiveCameraController), "Update")]
        private static void CaptureNativeInput(InteractiveCameraController __instance, Vector2 ____deltaRotation)
        {
            FirstPersonFocus focus = Focus;
            if (focus == null) return;
            try { focus.CaptureNativeLook(__instance, ____deltaRotation); }
            catch (Exception) { focus.DisableAfterError(); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CockpitCameraController), "Update")]
        private static void AfterCockpitPose(CockpitCameraController __instance) { AfterNativePose(__instance); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FirstPersonCameraController), "Update")]
        private static void AfterCameraPose(FirstPersonCameraController __instance) { AfterNativePose(__instance); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FirstPersonCharacterCameraController), "Update")]
        private static void AfterCharacterPose(FirstPersonCharacterCameraController __instance) { AfterNativePose(__instance); }

        private static void AfterNativePose(InteractiveCameraController __instance)
        {
            FirstPersonFocus focus = Focus;
            if (focus == null) return;
            try { focus.ApplyAfterNativeUpdate(__instance); }
            catch (Exception) { focus.DisableAfterError(); }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractiveCameraController), "HandleInput")]
        private static bool BeforeNativePointer(InteractiveCameraController __instance, InputEvent e)
        {
            FirstPersonFocus focus = Focus;
            if (focus == null || e.InputButton != InputButton.Middle) return true;
            try { return !focus.ConsumeMiddle(__instance); }
            catch (Exception) { focus.DisableAfterError(); return true; }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractiveCameraController), "Rotate")]
        private static bool BeforeNativeRotate(InteractiveCameraController __instance, Vector2 rotation)
        {
            FirstPersonFocus focus = Focus;
            if (focus == null) return true;
            try { return focus.BeforeNativeRotate(__instance, rotation); }
            catch (Exception) { focus.DisableAfterError(); return true; }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CockpitCameraController), "RecenterView")]
        private static void RecenterCockpit(CockpitCameraController __instance) { BeforeRecenter(__instance); }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FirstPersonCameraController), "RecenterView")]
        private static void RecenterCamera(FirstPersonCameraController __instance) { BeforeRecenter(__instance); }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FirstPersonCharacterCameraController), "RecenterView")]
        private static void RecenterCharacter(FirstPersonCharacterCameraController __instance) { BeforeRecenter(__instance); }

        private static void BeforeRecenter(CameraController __instance)
        {
            FirstPersonFocus focus = Focus;
            if (focus == null) return;
            try { focus.BeforeRecenter(__instance); }
            catch (Exception) { focus.DisableAfterError(); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CockpitCameraController), "get_IsRecenterAvailable")]
        private static void CockpitRecenterAvailable(CockpitCameraController __instance, ref bool __result) { RecenterAvailable(__instance, ref __result); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FirstPersonCameraController), "get_IsRecenterAvailable")]
        private static void CameraRecenterAvailable(FirstPersonCameraController __instance, ref bool __result) { RecenterAvailable(__instance, ref __result); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FirstPersonCharacterCameraController), "get_IsRecenterAvailable")]
        private static void CharacterRecenterAvailable(FirstPersonCharacterCameraController __instance, ref bool __result) { RecenterAvailable(__instance, ref __result); }

        private static void RecenterAvailable(CameraController __instance, ref bool __result)
        {
            FirstPersonFocus focus = Focus;
            if (focus == null) return;
            try { if (focus.HasFocus(__instance)) __result = true; }
            catch (Exception) { focus.DisableAfterError(); }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CameraManagerScript), "SelectCamera")]
        private static void BeforeCameraSwitch(CameraManagerScript __instance, out CameraController __state)
        {
            __state = __instance.Controller;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CameraManagerScript), "SelectCamera")]
        private static void AfterCameraSwitch(CameraManagerScript __instance, CameraController __state)
        {
            FirstPersonFocus focus = Focus;
            if (focus == null || ReferenceEquals(__instance.Controller, __state)) return;
            try { focus.Reset(false); }
            catch (Exception) { focus.DisableAfterError(); }
        }
    }
}
