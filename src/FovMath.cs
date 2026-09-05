using System;

namespace SP2FreeCamera
{
    // Shared by manual and automatic zoom; no Unity dependencies so the exact
    // production smoothing path can also run in the headless tests.
    internal static class FovMath
    {
        internal const float MinimumFov = 0.1f;
        internal const double DegreesToRadians = Math.PI / 180.0;

        internal static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        internal static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        internal static double LogHalfAngleTangent(double fov)
        {
            return Math.Log(Math.Tan(fov * DegreesToRadians * 0.5));
        }

        internal static double Smooth(double current, double target, double seconds, double deltaTime)
        {
            if (!IsFinite(current) || !IsFinite(target) || current <= 0 || target <= 0)
            {
                return IsFinite(current) && current > 0 ? current : 60.0;
            }

            seconds = Clamp(IsFinite(seconds) ? seconds : 0.12, 0, 2);
            if (seconds <= 0.0001)
            {
                return target;
            }
            if (!IsFinite(deltaTime) || deltaTime <= 0)
            {
                return current;
            }

            double currentLog = Math.Log(current);
            double targetLog = Math.Log(target);
            if (Math.Abs(targetLog - currentLog) <= 0.00005)
            {
                return target;
            }

            double blend = 1 - Math.Exp(-deltaTime / seconds);
            return Math.Exp(currentLog + (targetLog - currentLog) * blend);
        }
    }
}
