using UnityEngine;

namespace SP2FreeCamera
{
    internal static class NumericUtility
    {
        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static float ClampFinite(float value, float fallback, float minimum, float maximum)
        {
            return Mathf.Clamp(IsFinite(value) ? value : fallback, minimum, maximum);
        }
    }
}
