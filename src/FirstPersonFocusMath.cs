using UnityEngine;

namespace SP2FreeCamera
{
    internal static class FirstPersonFocusMath
    {
        internal static bool IsFinite(Vector3 value)
        {
            return NumericUtility.IsFinite(value.x) && NumericUtility.IsFinite(value.y) &&
                NumericUtility.IsFinite(value.z);
        }

        internal static bool IsValidRotation(Quaternion value)
        {
            return NumericUtility.IsFinite(value.x) && NumericUtility.IsFinite(value.y) &&
                NumericUtility.IsFinite(value.z) && NumericUtility.IsFinite(value.w) &&
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0.000001f;
        }

        internal static Quaternion LookOffset(Vector2 angles)
        {
            return Quaternion.Euler(angles.x, angles.y, 0f);
        }

        internal static bool TrySolve(
            Vector3 direction, Quaternion nativeReference, Quaternion nativeRoll,
            float previousYaw, out Vector2 angles, out Quaternion rotation)
        {
            angles = Vector2.zero;
            rotation = Quaternion.identity;
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.00000001f ||
                !IsValidRotation(nativeReference) || !IsValidRotation(nativeRoll) ||
                !NumericUtility.IsFinite(previousYaw))
            {
                return false;
            }

            Vector3 local = Quaternion.Inverse(nativeReference) * direction.normalized;
            float horizontal = Mathf.Sqrt(local.x * local.x + local.z * local.z);
            // At the poles yaw is undefined. Reuse the last local yaw instead of
            // switching to world-up or introducing a one-frame 180-degree roll.
            float yaw = horizontal <= 0.0001f ? previousYaw :
                previousYaw + Mathf.DeltaAngle(previousYaw, Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg);
            angles = new Vector2(-Mathf.Atan2(local.y, horizontal) * Mathf.Rad2Deg, yaw);
            rotation = nativeReference * LookOffset(angles) * nativeRoll;
            return IsValidRotation(rotation);
        }
    }
}
