using System;

namespace SP2FreeCamera
{
    // Integrates a constant target velocity over one rendered frame. Keeping this
    // scalar calculation independent of Unity also permits deterministic tests.
    internal static class MovementIntegrator
    {
        internal static void Calculate(
            double speedDifference,
            double acceleration,
            double smoothingTime,
            double deltaTime,
            out double velocityBlend,
            out double displacementBlendTime)
        {
            velocityBlend = 0.0;
            displacementBlendTime = 0.0;
            if (deltaTime <= 0.0)
            {
                return;
            }

            if (speedDifference <= 0.0)
            {
                velocityBlend = 1.0;
                displacementBlendTime = deltaTime;
                return;
            }

            double responseTime = smoothingTime <= 0.0001 ? 0.0 : smoothingTime;
            double linearTime = 0.0;
            double linearBlend = 0.0;
            if (acceleration > 0.0)
            {
                // Limit the magnitude of the WORLD velocity change, not each
                // axis separately. Turn, brake and speed changes share this cap.
                // dv/dt = normalize(target - v) * min(a, |target - v| / tau).
                double differenceAtEase = acceleration * responseTime;
                linearTime = Math.Min(
                    deltaTime,
                    Math.Max(0.0, (speedDifference - differenceAtEase) / acceleration));
                linearBlend = Math.Min(1.0, acceleration * linearTime / speedDifference);
            }

            velocityBlend = linearBlend;
            displacementBlendTime = 0.5 * linearBlend * linearTime;
            double remainingTime = deltaTime - linearTime;
            if (remainingTime <= 0.0)
            {
                return;
            }

            if (responseTime == 0.0)
            {
                // The target was reached partway through the frame, or both
                // controls are disabled. Integrate the remaining cruise time.
                velocityBlend = 1.0;
                displacementBlendTime += remainingTime;
                return;
            }

            double ratio = remainingTime / responseTime;
            double exponentialBlend;
            double exponentialArea;
            if (ratio < 0.001)
            {
                // Avoid cancellation for very short frames / long smoothing.
                double ratioSquared = ratio * ratio;
                exponentialBlend = ratio * (1.0 - ratio / 2.0 + ratioSquared / 6.0 -
                    ratioSquared * ratio / 24.0 + ratioSquared * ratioSquared / 120.0);
                exponentialArea = ratioSquared * (0.5 - ratio / 6.0 + ratioSquared / 24.0 -
                    ratioSquared * ratio / 120.0 + ratioSquared * ratioSquared / 720.0);
            }
            else
            {
                exponentialBlend = 1.0 - Math.Exp(-ratio);
                exponentialArea = ratio - exponentialBlend;
            }

            // Exact velocity and position integrals, including a possible switch
            // from constant acceleration to easing inside this rendered frame.
            velocityBlend += (1.0 - linearBlend) * exponentialBlend;
            displacementBlendTime += linearBlend * remainingTime +
                (1.0 - linearBlend) * responseTime * exponentialArea;
        }
    }
}
