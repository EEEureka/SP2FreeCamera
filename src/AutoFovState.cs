using System;

namespace SP2FreeCamera
{
    // A virtual unit sphere, not the target's mesh or bounds. All target kinds
    // supply only their position. Nothing in this class moves the camera.
    internal sealed class AutoFovState
    {
        private double _referenceLogSpan;
        private double _logAreaRatio;

        internal bool Enabled { get; private set; }
        internal bool Active { get; private set; }
        internal bool HasReference { get; private set; }
        internal double AreaRatio
        {
            get { return Math.Exp(FovMath.Clamp(_logAreaRatio, -600, 600)); }
        }

        internal void SetEnabled(bool enabled)
        {
            if (Enabled == enabled)
            {
                return;
            }
            Enabled = enabled;
            ResetTarget();
        }

        internal void ResetTarget()
        {
            Active = false;
            HasReference = false;
            _referenceLogSpan = 0;
            _logAreaRatio = 0;
        }

        internal void Suspend()
        {
            // Preserve the reference during the focus system's brief lost-target
            // grace period. A real target switch/clear calls ResetTarget instead.
            Active = false;
        }

        internal bool TryGetTargetFov(
            double distance,
            double aspect,
            double currentFov,
            double maximumFov,
            double scroll,
            double sensitivity,
            out double targetFov)
        {
            targetFov = currentFov;
            if (!Enabled || !FovMath.IsFinite(distance) || distance < 0 ||
                !FovMath.IsFinite(aspect) || aspect <= 0 || !FovMath.IsFinite(currentFov))
            {
                Suspend();
                return false;
            }

            maximumFov = FovMath.Clamp(
                FovMath.IsFinite(maximumFov) ? maximumFov : 120,
                FovMath.MinimumFov, 179);
            currentFov = FovMath.Clamp(currentFov, FovMath.MinimumFov, maximumFov);
            Active = true;

            if (distance <= 1)
            {
                // The camera is on/inside the reference sphere. There is no
                // finite exterior silhouette to frame: silently saturate. If
                // enabled here, defer reference capture until outside the sphere.
                targetFov = maximumFov;
                return true;
            }

            // Centered sphere area / viewport area is proportional to
            // 1 / (aspect * (distance^2 - 1) * tan(FOV/2)^2).
            // Log arithmetic avoids overflow at extreme distances/magnifications.
            double geometryLog = 0.5 * (Math.Log(distance - 1) +
                Math.Log(distance + 1) + Math.Log(aspect));
            if (!HasReference)
            {
                _referenceLogSpan = geometryLog + FovMath.LogHalfAngleTangent(currentFov);
                _logAreaRatio = 0;
                HasReference = true;
            }

            double minimumLogTangent = FovMath.LogHalfAngleTangent(FovMath.MinimumFov);
            double maximumLogTangent = FovMath.LogHalfAngleTangent(maximumFov);
            double framingLog = _referenceLogSpan - geometryLog;
            double targetLogTangent = FovMath.Clamp(
                framingLog - 0.5 * _logAreaRatio, minimumLogTangent, maximumLogTangent);

            if (FovMath.IsFinite(scroll) && Math.Abs(scroll) > 0.0001)
            {
                sensitivity = FovMath.Clamp(FovMath.IsFinite(sensitivity) ? sensitivity : 1, 0.01, 10);
                double adjustedLogTangent = FovMath.Clamp(
                    targetLogTangent + scroll * sensitivity * Math.Log(0.9),
                    minimumLogTangent, maximumLogTangent);
                if (Math.Abs(adjustedLogTangent - targetLogTangent) > 1e-12)
                {
                    // Start scroll input at the reachable FOV boundary to avoid
                    // hidden wheel debt. Distance-only clamping never rewrites
                    // the user's ratio; it recovers when the target comes back.
                    _logAreaRatio = 2 * (framingLog - adjustedLogTangent);
                    targetLogTangent = adjustedLogTangent;
                }
            }

            targetFov = FovMath.Clamp(
                2 * Math.Atan(Math.Exp(targetLogTangent)) / FovMath.DegreesToRadians,
                FovMath.MinimumFov, maximumFov);
            return true;
        }
    }
}
