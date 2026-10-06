using System;
using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// Fixed-step accumulator. The simulation advances in whole ticks of
    /// FixedDeltaTime regardless of frame rate; Alpha (0..1) is how far the
    /// renderer currently is between the last two ticks, for view interpolation.
    /// Deliberately independent of Unity's Time.fixedDeltaTime so physics settings
    /// can never change simulation cadence.
    /// </summary>
    public sealed class SimTickClock
    {
        private readonly int _maxStepsPerAdvance;
        private float _accumulator;

        public float FixedDeltaTime { get; }

        /// <summary>1 when frozen/idle (views show the latest tick), otherwise the fraction of the next tick already elapsed.</summary>
        public float Alpha { get; private set; } = 1f;

        public SimTickClock(float fixedDeltaTime, int maxStepsPerAdvance = 5)
        {
            if (fixedDeltaTime <= 0f) throw new ArgumentOutOfRangeException(nameof(fixedDeltaTime));
            if (maxStepsPerAdvance < 1) throw new ArgumentOutOfRangeException(nameof(maxStepsPerAdvance));
            FixedDeltaTime = fixedDeltaTime;
            _maxStepsPerAdvance = maxStepsPerAdvance;
        }

        /// <summary>
        /// Accumulates frame time and runs step() once per whole tick owed.
        /// step returns false once the thing being driven is finished, which stops
        /// the loop early. If a frame owes more than maxStepsPerAdvance ticks the
        /// backlog is dropped rather than spiralling. Returns ticks executed.
        /// </summary>
        public int Advance(float frameDeltaTime, Func<bool> step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));

            _accumulator += Mathf.Max(0f, frameDeltaTime);
            int steps = 0;

            while (_accumulator >= FixedDeltaTime && steps < _maxStepsPerAdvance)
            {
                _accumulator -= FixedDeltaTime;
                steps++;

                if (!step())
                {
                    Freeze();
                    return steps;
                }
            }

            if (steps == _maxStepsPerAdvance) _accumulator = 0f;

            Alpha = Mathf.Clamp01(_accumulator / FixedDeltaTime);
            return steps;
        }

        /// <summary>Drops any partial tick and pins Alpha to 1 so views settle on the latest simulated state.</summary>
        public void Freeze()
        {
            _accumulator = 0f;
            Alpha = 1f;
        }
    }
}
