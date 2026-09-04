using Assets.Scripts.Flight.Cameras;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace SP2FreeCamera
{
    [HarmonyPatch]
    internal static class InputGuardPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScreenInputScript), "OnPointerDown")]
        private static bool ScreenInputPointerDownPrefix(PointerEventData eventData)
        {
            return !ShouldBlockScreenInput(eventData, false);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScreenInputScript), "OnDrag")]
        private static bool ScreenInputDragPrefix(PointerEventData eventData)
        {
            return !ShouldBlockScreenInput(eventData, false);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScreenInputScript), "OnScroll")]
        private static bool ScreenInputScrollPrefix(PointerEventData eventData)
        {
            return !ShouldBlockScreenInput(eventData, true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CameraManagerScript), "EnterKillCam")]
        private static bool EnterKillCamPrefix()
        {
            return !FreeCameraRuntime.IsFreeCameraActive;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UserInterface), "get_AllowKeyboardInputs")]
        private static void AllowKeyboardInputsPostfix(ref bool __result)
        {
            FreeCameraRuntime runtime = FreeCameraRuntime.Instance;
            if (runtime != null && runtime.MenuVisible)
            {
                __result = false;
            }
        }

        private static bool ShouldBlockScreenInput(PointerEventData eventData, bool isScroll)
        {
            FreeCameraRuntime runtime = FreeCameraRuntime.Instance;
            if (runtime == null || eventData == null)
            {
                return false;
            }

            if (runtime.ShouldBlockScreenInput(eventData.position))
            {
                return true;
            }

            if (!runtime.Active)
            {
                return false;
            }

            return isScroll || eventData.button == PointerEventData.InputButton.Left ||
                eventData.button == PointerEventData.InputButton.Middle;
        }
    }
}
