using System;
using System.Collections.Generic;

namespace SP2FreeCamera.Tests
{
    public static class MovementIntegratorTests
    {
        private struct Vector
        {
            internal double X, Y, Z;
            internal Vector(double x, double y = 0, double z = 0) { X = x; Y = y; Z = z; }
            internal double Length { get { return Math.Sqrt(X * X + Y * Y + Z * Z); } }
            public static Vector operator +(Vector a, Vector b) { return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
            public static Vector operator -(Vector a, Vector b) { return new Vector(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
            public static Vector operator *(Vector a, double scale) { return new Vector(a.X * scale, a.Y * scale, a.Z * scale); }
        }

        private struct State
        {
            internal Vector Position, Velocity;
        }

        public static string[] RunAll()
        {
            var passed = new List<string>();
            CheckStartAndStop();
            passed.Add("Constant acceleration: start, cruise, brake and finite stop distance");
            CheckTurnAndReverse();
            passed.Add("Turns and reversals: continuous world velocity and vector acceleration cap");
            CheckSmoothingModes();
            passed.Add("Smoothing on/off, acceleration disabled and transition inside a frame");
            CheckModeAcceleration();
            passed.Add("Normal 80 / fast 800: independent acceleration, braking and continuous mode switches");
            CheckFrameRates();
            passed.Add("30/60/144 FPS and variable frame times: same velocity and travel");
            CheckLimits();
            passed.Add("Small/large acceleration, low/high speeds, no overshoot or NaN");
            return passed.ToArray();
        }

        private static void CheckStartAndStop()
        {
            var state = new State();
            Simulate(ref state, new Vector(200), 100, 0, 1, new[] { 1.0 / 60 });
            Near(state.Velocity.X, 100, 1e-9, "speed after one second");
            Near(state.Position.X, 50, 1e-9, "distance = a*t*t/2");
            Simulate(ref state, new Vector(200), 100, 0, 2, new[] { 1.0 / 60 });
            Near(state.Velocity.X, 200, 1e-9, "cruise speed");
            Near(state.Position.X, 400, 1e-9, "accelerate then cruise");
            state.Position = new Vector();
            Simulate(ref state, new Vector(), 100, 0, 3, new[] { 1.0 / 60 });
            Near(state.Velocity.Length, 0, 1e-9, "stopped without reversal");
            Near(state.Position.X, 200, 1e-9, "braking distance = v*v/(2*a)");

            state = new State { Velocity = new Vector(10) };
            Advance(ref state, new Vector(), 100, 0, 0.25);
            Near(state.Position.X, 0.5, 1e-12, "stop partway through frame");
            Near(state.Velocity.Length, 0, 1e-12, "no displacement during remaining stopped time");
        }

        private static void CheckTurnAndReverse()
        {
            var state = new State { Velocity = new Vector(200) };
            Simulate(ref state, new Vector(-200), 100, 0, 1, new[] { 1.0 / 60 });
            Near(state.Velocity.X, 100, 1e-9, "reversal brakes before travelling backwards");
            Near(state.Position.X, 150, 1e-9, "reversal distance before zero crossing");
            Simulate(ref state, new Vector(-200), 100, 0, 3, new[] { 1.0 / 60 });
            Near(state.Velocity.X, -200, 1e-9, "reverse cruise");
            Near(state.Position.X, 0, 1e-9, "symmetric reversal travel");

            state = new State { Velocity = new Vector(200) };
            Simulate(ref state, new Vector(0, 200), 100, 0, 1, new[] { 1.0 / 60 });
            Near(state.Velocity.X, 200 - 100 / Math.Sqrt(2), 1e-9, "retain world X momentum during turn");
            Near(state.Velocity.Y, 100 / Math.Sqrt(2), 1e-9, "build lateral velocity during turn");
            Near((state.Velocity - new Vector(200)).Length, 100, 1e-9, "cap applies to full vector");
        }

        private static void CheckSmoothingModes()
        {
            var state = new State();
            Advance(ref state, new Vector(200), 0, 0, 0.1);
            Near(state.Velocity.X, 200, 1e-12, "both controls disabled");
            Near(state.Position.X, 20, 1e-12, "instant movement distance");

            state = new State();
            Advance(ref state, new Vector(200), 0, 0.08, 0.1);
            double decay = Math.Exp(-0.1 / 0.08);
            Near(state.Velocity.X, 200 * (1 - decay), 1e-10, "legacy exponential velocity");
            Near(state.Position.X, 200 * (0.1 - 0.08 * (1 - decay)), 1e-10, "legacy exponential travel");

            state = new State();
            // With a=100, tau=.1 and target=20, ease begins after .1 seconds.
            Advance(ref state, new Vector(20), 100, 0.1, 0.2);
            Near(state.Velocity.X, 20 - 10 / Math.E, 1e-10, "linear-to-eased velocity");
            Near(state.Position.X, 1.5 + 1 / Math.E, 1e-10, "linear-to-eased travel");

            state = new State { Velocity = new Vector(200) };
            Simulate(ref state, new Vector(), 400, 0.08, 4, new[] { 1.0 / 60 });
            Near(state.Velocity.Length, 0, 1e-9, "eased braking settles");
            // Constant braking to a*tau, then exponential tail: v²/(2a) + a*tau²/2.
            Near(state.Position.X, 51.28, 1e-8, "eased stopping distance");
        }

        private static void CheckModeAcceleration()
        {
            var normal = new State();
            var fast = new State();
            Simulate(ref normal, new Vector(200), 80, 0, 1, new[] { 1.0 / 60 });
            Simulate(ref fast, new Vector(2000), 800, 0, 1, new[] { 1.0 / 60 });
            Near(normal.Velocity.X, 80, 1e-9, "normal acceleration");
            Near(fast.Velocity.X, 800, 1e-9, "fast acceleration");
            Near(normal.Position.X, 40, 1e-9, "normal start travel");
            Near(fast.Position.X, 400, 1e-9, "fast start travel");

            normal = new State { Velocity = new Vector(200) };
            fast = new State { Velocity = new Vector(2000) };
            Simulate(ref normal, new Vector(), 80, 0, 1, new[] { 1.0 / 60 });
            Simulate(ref fast, new Vector(), 800, 0, 1, new[] { 1.0 / 60 });
            Near(normal.Velocity.X, 120, 1e-9, "normal braking");
            Near(fast.Velocity.X, 1200, 1e-9, "fast braking");

            var switched = new State { Velocity = new Vector(200) };
            Advance(ref switched, new Vector(2000), 800, 0, 0.1);
            Near(switched.Velocity.X, 280, 1e-9, "switch to fast retains velocity and applies fast rate");
            Near(switched.Position.X, 24, 1e-9, "continuous travel when switching to fast");
            switched = new State { Velocity = new Vector(2000) };
            Advance(ref switched, new Vector(200), 80, 0, 0.1);
            Near(switched.Velocity.X, 1992, 1e-9, "switch to normal brakes at normal rate without clamping speed");
            Near(switched.Position.X, 199.6, 1e-9, "continuous travel when switching to normal");
        }

        private static State RunSequence(double smoothing, double[] frames)
        {
            var state = new State();
            Simulate(ref state, new Vector(200), 80, smoothing, 2.83, frames);
            Simulate(ref state, new Vector(0, 200), 80, smoothing, 0.57, frames);
            Simulate(ref state, new Vector(-200), 80, smoothing, 0.9, frames);
            Simulate(ref state, new Vector(0, 0, 2000), 800, smoothing, 3.23, frames);
            Simulate(ref state, new Vector(0, 0, 200), 80, smoothing, 0.47, frames);
            Simulate(ref state, new Vector(), 80, smoothing, 35, frames);
            return state;
        }

        private static void CheckFrameRates()
        {
            var rates = new[]
            {
                new[] { 1.0 / 30 }, new[] { 1.0 / 60 }, new[] { 1.0 / 144 },
                new[] { 0.007, 0.021, 0.012, 0.048, 0.016 }
            };
            foreach (double smoothing in new[] { 0.0, 0.08, 0.3, 2.0 })
            {
                State reference = RunSequence(smoothing, new[] { 0.001 });
                foreach (double[] frames in rates)
                {
                    State actual = RunSequence(smoothing, frames);
                    Near((actual.Position - reference.Position).Length, 0, 1e-7, "frame-rate-independent position");
                    Near((actual.Velocity - reference.Velocity).Length, 0, 1e-7, "frame-rate-independent velocity");
                }
            }
        }

        private static void CheckLimits()
        {
            foreach (double acceleration in new[] { 0.0, 0.001, 1.0, 80.0, 800.0, 1000000.0 })
            foreach (double smoothing in new[] { 0.0, 0.08, 2.0 })
            foreach (double speed in new[] { 0.1, 200.0, 2000.0, 100000.0 })
            {
                var state = new State { Velocity = new Vector(-speed) };
                Simulate(ref state, new Vector(0, 0, speed), acceleration, smoothing,
                    1.13, new[] { 0.000001, 0.004, 0.25, 1.0 / 144 });
            }
        }

        private static void Simulate(ref State state, Vector target, double acceleration,
            double smoothing, double duration, double[] frames)
        {
            int frame = 0;
            double elapsed = 0;
            while (duration - elapsed > 1e-12)
            {
                double dt = Math.Min(frames[frame++ % frames.Length], duration - elapsed);
                Advance(ref state, target, acceleration, smoothing, dt);
                elapsed += dt;
            }
        }

        private static void Advance(ref State state, Vector target, double acceleration,
            double smoothing, double dt)
        {
            Vector delta = target - state.Velocity;
            double blend, area;
            MovementIntegrator.Calculate(delta.Length, acceleration, smoothing, dt, out blend, out area);
            if (double.IsNaN(blend) || double.IsNaN(area) || blend < 0 || blend > 1 || area < 0 || area > dt)
                throw new Exception("Invalid or overshooting movement step.");
            Vector previous = state.Velocity;
            state.Position += previous * dt + delta * area;
            state.Velocity += delta * blend;
            if (acceleration > 0 && (state.Velocity - previous).Length > acceleration * dt + 1e-8)
                throw new Exception("World velocity changed faster than configured acceleration.");
            if ((target - state.Velocity).Length > delta.Length + 1e-8)
                throw new Exception("Velocity moved away from target.");
        }

        private static void Near(double actual, double expected, double tolerance, string label)
        {
            if (double.IsNaN(actual) || Math.Abs(actual - expected) > tolerance)
                throw new Exception(label + ": expected " + expected + ", actual " + actual);
        }
    }
}
