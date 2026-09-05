using System;
using System.Collections.Generic;

namespace SP2FreeCamera.Tests
{
    public static class AutoFovTests
    {
        private const double Aspect = 16.0 / 9.0;

        public static string[] RunAll()
        {
            var passed = new List<string>();
            CheckActivation();
            passed.Add("Default off, armed without a target, current-FOV baseline without a jump");
            CheckProjection();
            passed.Add("Exact unit-sphere projected area invariant across near/far distances");
            CheckWheel();
            passed.Add("Wheel controls reference area, reversible/fractional input and shared sensitivity");
            CheckTargetTransitions();
            passed.Add("Target clear/switch, disabled/re-enabled state and fresh reference capture");
            CheckLostTarget();
            passed.Add("Invalid/lost target suspends zoom, short-loss recovery preserves reference");
            CheckLimits();
            passed.Add("Silent FOV limits, unchanged desired ratio and automatic range recovery");
            CheckWheelAtLimits();
            passed.Add("No hidden wheel debt at either FOV limit; reverse scroll responds immediately");
            CheckInsideSphere();
            passed.Add("On/inside unit sphere safely saturates; deferred first reference and recovery");
            CheckAspect();
            passed.Add("Viewport-aspect changes preserve reference area as a fraction of the viewport");
            CheckExtremeValues();
            passed.Add("Extreme distances/scroll, sub-degree FOV and non-finite inputs stay safe");
            CheckSmoothing();
            passed.Add("Shared manual/auto log-FOV smoothing: zero/nonzero tau, pause and invalid dt");
            CheckFrameRates();
            passed.Add("30/60/144 FPS and variable frame times agree for a fixed zoom target");
            CheckMovingTargetSmoothing();
            passed.Add("Per-frame moving target: exact ideal area, expected bounded smoothed lag");
            return passed.ToArray();
        }

        private static AutoFovState NewState()
        {
            var state = new AutoFovState();
            state.SetEnabled(true);
            return state;
        }

        private static double Sample(
            AutoFovState state, double distance, double current = 30,
            double maximum = 179, double scroll = 0, double sensitivity = 1,
            double aspect = Aspect)
        {
            double target;
            Assert(state.TryGetTargetFov(distance, aspect, current, maximum,
                scroll, sensitivity, out target), "valid automatic sample");
            Assert(FovMath.IsFinite(target) && target >= FovMath.MinimumFov &&
                target <= maximum, "finite in-range FOV");
            return target;
        }

        private static double ProjectedArea(double distance, double fov, double aspect = Aspect)
        {
            // Independent construction: the camera-to-sphere tangent subtends
            // asin(radius/distance). Project that angular silhouette onto the
            // vertical image plane, then normalize circular area by the viewport.
            double silhouetteAngle = Math.Asin(1 / distance);
            double radius = Math.Tan(silhouetteAngle) /
                Math.Tan(fov * Math.PI / 360);
            return Math.PI * radius * radius / (4 * aspect);
        }

        private static void CheckActivation()
        {
            var state = new AutoFovState();
            double target;
            Assert(!state.Enabled && !state.Active, "default off");
            Assert(!state.TryGetTargetFov(100, Aspect, 30, 120, 0, 1, out target), "off is manual");
            state.SetEnabled(true);
            Assert(!state.TryGetTargetFov(double.NaN, Aspect, 30, 120, 1, 1, out target), "no target");
            Assert(state.Enabled && !state.Active && !state.HasReference, "armed while waiting");
            Near(Sample(state, 100, 47), 47, 1e-10, "capture actual FOV");
            Assert(state.Active && state.HasReference, "tracking after capture");
            Near(state.AreaRatio, 1, 1e-12, "baseline is 100 percent");
            Sample(state, 100, 47, 179, 1);
            double ratio = state.AreaRatio;
            state.SetEnabled(true);
            Near(state.AreaRatio, ratio, 1e-12, "idempotent menu toggle does not reset reference");
        }

        private static void CheckProjection()
        {
            var state = NewState();
            Sample(state, 100);
            double baseline = ProjectedArea(100, 30);
            foreach (double distance in new[] { 2.0, 5, 10, 25, 100, 500, 1000, 10000 })
            {
                double fov = Sample(state, distance);
                Near(ProjectedArea(distance, fov) / baseline, 1, 1e-10, "constant sphere area");
            }
            Near(Sample(state, 100), 30, 1e-10, "no accumulating reference drift");
        }

        private static void CheckWheel()
        {
            var state = NewState();
            Sample(state, 100);
            double fov = Sample(state, 100, 30, 179, 1);
            Near(state.AreaRatio, 1 / (0.9 * 0.9), 1e-10, "forward wheel enlarges area");
            Near(ProjectedArea(100, fov) / ProjectedArea(100, 30),
                state.AreaRatio, 1e-10, "ratio is area, not radius");
            double wantedArea = ProjectedArea(100, fov);
            Near(ProjectedArea(250, Sample(state, 250)) / wantedArea, 1, 1e-10, "new ratio tracks distance");
            Near(Sample(state, 100, fov, 179, -1), 30, 1e-10, "reverse wheel restores framing");
            Near(state.AreaRatio, 1, 1e-10, "reverse wheel restores area ratio");

            var a = NewState();
            var b = NewState();
            Sample(a, 100);
            Sample(b, 100);
            double whole = Sample(a, 100, 30, 179, 1, 2);
            Sample(b, 100, 30, 179, 0.25, 2);
            double fractional = Sample(b, 100, 30, 179, 0.75, 2);
            Near(whole, fractional, 1e-10, "fractional wheel accumulation");
        }

        private static void CheckTargetTransitions()
        {
            var state = NewState();
            Sample(state, 100);
            Sample(state, 100, 30, 179, 3);
            state.ResetTarget();
            Assert(state.Enabled && !state.Active && !state.HasReference, "clear leaves switch armed");
            Near(Sample(state, 800, 42), 42, 1e-10, "new target keeps rendered FOV");
            Near(state.AreaRatio, 1, 1e-12, "new target gets its own baseline");
            state.SetEnabled(false);
            Assert(!state.Enabled && !state.Active && !state.HasReference, "off drops automatic state");
            state.SetEnabled(true);
            Near(Sample(state, 700, 19), 19, 1e-10, "enable uses current manual framing");
        }

        private static void CheckLostTarget()
        {
            var state = NewState();
            Sample(state, 100);
            Sample(state, 100, 30, 179, 2);
            double ratio = state.AreaRatio;
            double expected = Sample(state, 200);
            foreach (double distance in new[] { double.NaN, double.PositiveInfinity, -1.0 })
            {
                double target;
                Assert(!state.TryGetTargetFov(distance, Aspect, 22, 120, 1, 1, out target), "reject invalid distance");
                Assert(!state.Active && state.HasReference && state.Enabled, "suspend retains reference");
                Near(target, 22, 0, "invalid sample does not request a different FOV");
                Near(state.AreaRatio, ratio, 1e-12, "lost-frame scroll cannot alter stale reference");
                Near(Sample(state, 200), expected, 1e-10, "same-target recovery");
            }
        }

        private static void CheckLimits()
        {
            var state = NewState();
            Sample(state, 100, 30, 120);
            Near(Sample(state, 2, 30, 120), 120, 1e-10, "upper limit");
            Near(Sample(state, 1e20, 30, 120), FovMath.MinimumFov, 1e-10, "lower limit");
            Near(state.AreaRatio, 1, 0, "distance clamping does not rewrite desired area");
            Near(Sample(state, 100, 30, 120), 30, 1e-10, "original framing returns inside limits");
            Near(Sample(state, 10, 30, 60), 60, 1e-10, "changed menu maximum is respected");
        }

        private static void CheckWheelAtLimits()
        {
            var state = NewState();
            Sample(state, 100, 30, 120);
            Near(Sample(state, 100, 30, 120, 1000000), FovMath.MinimumFov, 1e-10, "wheel zoom-in limit");
            double ratio = state.AreaRatio;
            Sample(state, 100, 30, 120, 1000000);
            Near(state.AreaRatio / ratio, 1, 1e-12, "no additional zoom-in wheel debt");
            Assert(Sample(state, 100, 30, 120, -1) > FovMath.MinimumFov, "zoom out responds immediately");

            Near(Sample(state, 100, 30, 120, -1000000), 120, 1e-10, "wheel zoom-out limit");
            ratio = state.AreaRatio;
            Sample(state, 100, 30, 120, -1000000);
            Near(state.AreaRatio / ratio, 1, 1e-12, "no additional zoom-out wheel debt");
            Assert(Sample(state, 100, 30, 120, 1) < 120, "zoom in responds immediately");

            state = NewState();
            Sample(state, 100, 30, 120);
            Sample(state, 1e10, 30, 120, 1);
            Near(state.AreaRatio, 1, 0, "outward scroll at distance-induced limit preserves reference");
            Assert(Sample(state, 1e10, 30, 120, -1) > FovMath.MinimumFov, "reverse scroll at distance-induced limit");
        }

        private static void CheckInsideSphere()
        {
            var state = NewState();
            foreach (double distance in new[] { 0.0, 0.5, 1 })
            {
                Near(Sample(state, distance, 30, 120, 1), 120, 0, "inside/on sphere clamps high");
                Assert(state.Active && !state.HasReference, "inside capture is deferred");
            }
            Near(Sample(state, 2, 68, 120), 68, 1e-10, "first exterior frame captures actual FOV");
            double expected = Sample(state, 10, 68, 120);
            Sample(state, 0, 68, 120);
            Near(Sample(state, 10, 68, 120), expected, 1e-10, "crossing sphere retains existing reference");
            Near(Sample(state, 1 + 1e-12, 68, 120), 120, 1e-10, "near-surface numerics");
        }

        private static void CheckAspect()
        {
            var state = NewState();
            Sample(state, 100);
            double baseline = ProjectedArea(100, 30);
            foreach (double aspect in new[] { 1.0, 4.0 / 3, 16.0 / 9, 32.0 / 9 })
            {
                double fov = Sample(state, 200, 30, 179, 0, 1, aspect);
                Near(ProjectedArea(200, fov, aspect) / baseline, 1, 1e-10, "aspect-correct viewport area");
            }
        }

        private static void CheckExtremeValues()
        {
            var state = NewState();
            Near(Sample(state, double.MaxValue, 40), 40, 1e-9, "huge reference distance without overflow");
            Sample(state, 2, 40, 179, double.MaxValue);
            Sample(state, double.MaxValue, 40, 179, -double.MaxValue);
            Assert(FovMath.IsFinite(state.AreaRatio), "finite displayed ratio");

            state = NewState();
            Near(Sample(state, 100, 0.2), 0.2, 1e-12, "sub-degree baseline");
            double belowOne = Sample(state, 200, 0.2);
            Assert(belowOne >= FovMath.MinimumFov && belowOne < 1, "custom-projection range supported");
            double target;
            Assert(!state.TryGetTargetFov(100, 0, 30, 120, 0, 1, out target), "invalid aspect");
            Assert(!state.TryGetTargetFov(100, Aspect, double.NaN, 120, 0, 1, out target), "invalid current FOV");
            Assert(state.TryGetTargetFov(100, Aspect, 30, double.NaN, double.NaN, double.NaN, out target), "invalid settings fallback");
            Assert(FovMath.IsFinite(target), "fallback result finite");
        }

        private static void CheckSmoothing()
        {
            Near(FovMath.Smooth(30, 90, 0, 1.0 / 60), 90, 0, "zero smoothing snaps");
            Near(FovMath.Smooth(30, 90, 0.3, 0), 30, 0, "paused timer holds");
            Near(FovMath.Smooth(30, 90, 0.3, double.NaN), 30, 0, "invalid timer holds");
            Near(FovMath.Smooth(30, 90, 0.3, 0.3),
                Math.Exp(Math.Log(30) + Math.Log(3) * (1 - 1 / Math.E)), 1e-10, "existing log-FOV easing");
            double current = 30;
            for (int frame = 0; frame < 300; frame++)
            {
                double next = FovMath.Smooth(current, 90, 0.3, 1.0 / 60);
                Assert(next >= current - 1e-12 && next <= 90 + 1e-12, "no smoothing overshoot");
                current = next;
            }
            Near(current, 90, 1e-8, "smooth zoom settles");
        }

        private static void CheckFrameRates()
        {
            double expected = FovMath.Smooth(30, 90, 0.3, 1);
            foreach (int fps in new[] { 30, 60, 144 })
            {
                double current = 30;
                for (int frame = 0; frame < fps; frame++)
                {
                    current = FovMath.Smooth(current, 90, 0.3, 1.0 / fps);
                }
                Near(current, expected, 1e-9, "frame-rate independent smoothing");
            }
            double variable = 30;
            foreach (double dt in new[] { 0.01, 0.07, 0.22, 0.3, 0.4 })
            {
                variable = FovMath.Smooth(variable, 90, 0.3, dt);
            }
            Near(variable, expected, 1e-9, "variable frame times");
        }

        private static void CheckMovingTargetSmoothing()
        {
            var state = NewState();
            Sample(state, 1000);
            double reference = ProjectedArea(1000, 30);
            double applied = 30;
            double ideal = 30;
            for (int frame = 1; frame <= 60; frame++)
            {
                double distance = 1000 - 500.0 * frame / 60;
                ideal = Sample(state, distance, applied, 120);
                Near(ProjectedArea(distance, ideal) / reference, 1, 1e-10, "moving target's ideal area");
                applied = FovMath.Smooth(applied, ideal, 0.3, 1.0 / 60);
                Assert(applied <= ideal, "approach allows intended smoothing lag");
            }
            double ratio = ProjectedArea(500, applied) / reference;
            Assert(ratio > 1 && ratio < ProjectedArea(500, 30) / reference,
                "smoothed area lies between ideal compensation and unchanged manual FOV");
            for (int frame = 0; frame < 300; frame++)
            {
                applied = FovMath.Smooth(applied, ideal, 0.3, 1.0 / 60);
            }
            Near(ProjectedArea(500, applied) / reference, 1, 1e-9, "settled area matches reference");
        }

        private static void Near(double actual, double expected, double tolerance, string label)
        {
            if (!FovMath.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
            {
                throw new Exception(label + ": expected " + expected + ", got " + actual);
            }
        }

        private static void Assert(bool condition, string label)
        {
            if (!condition)
            {
                throw new Exception(label);
            }
        }
    }
}
