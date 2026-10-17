using LittleDelay.Simulation.Core;
using UnityEngine;

namespace LittleDelay.Simulation.Fighters
{
    /// <summary>
    /// Two independent checks, evaluated together every PassivityCheckIntervalSeconds: how far the fighter's
    /// REAL position spread over the interval (the diagonal of the XZ box its positions span), and whether any
    /// ability was successfully used. Failing either marks the fighter passive for that interval - moving
    /// without acting is passive, acting without moving is passive.
    /// Using real positions (not the commanded path length) means pushing into the arena wall, or pacing back
    /// and forth in a small area, does not count as travel. Jumping is ignored (y is not tracked).
    /// </summary>
    public sealed class SimPassivityTracker
    {
        private float _minX, _maxX, _minZ, _maxZ;
        private bool _hasBounds;
        private bool _actionTakenSinceCheck;
        private float _timeSinceCheck;

        public SimPassivityTracker() { }

        /// <summary>Seeds the first interval with the spawn position so the very first tick of movement is counted.</summary>
        public SimPassivityTracker(Vector3 startPosition) => Seed(startPosition);

        /// <summary>
        /// Accumulates this tick and evaluates once the interval has elapsed. Returns true only on the
        /// tick a check completes; callers act on isPassive only when this returns true.
        /// </summary>
        public bool Update(Vector3 position, bool actionTakenThisTick, float deltaTime, SimMatchConfig config, out bool isPassive)
        {
            Include(position);
            _actionTakenSinceCheck |= actionTakenThisTick;
            _timeSinceCheck += deltaTime;

            isPassive = false;
            if (_timeSinceCheck + 0.0001f < config.PassivityCheckIntervalSeconds)
            {
                return false;
            }

            float width = _maxX - _minX;
            float depth = _maxZ - _minZ;
            float extent = Mathf.Sqrt(width * width + depth * depth);

            isPassive = extent < config.PassivityMinTravelDistance || !_actionTakenSinceCheck;

            // The next interval starts where this one ended.
            Seed(position);
            _actionTakenSinceCheck = false;
            _timeSinceCheck = 0f;
            return true;
        }

        private void Seed(Vector3 position)
        {
            _minX = _maxX = position.x;
            _minZ = _maxZ = position.z;
            _hasBounds = true;
        }

        private void Include(Vector3 position)
        {
            if (!_hasBounds)
            {
                Seed(position);
                return;
            }

            if (position.x < _minX) _minX = position.x;
            if (position.x > _maxX) _maxX = position.x;
            if (position.z < _minZ) _minZ = position.z;
            if (position.z > _maxZ) _maxZ = position.z;
        }
    }
}
