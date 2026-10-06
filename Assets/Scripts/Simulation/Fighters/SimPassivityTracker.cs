using LittleDelay.Simulation.Core;

namespace LittleDelay.Simulation.Fighters
{
    /// <summary>
    /// Two independent checks, evaluated together every PassivityCheckIntervalSeconds: distance the
    /// fighter chose to travel since the last check, and whether any ability was successfully used
    /// since the last check. Failing either marks the fighter passive for that interval - moving
    /// without acting is passive, acting without moving is passive.
    /// </summary>
    public sealed class SimPassivityTracker
    {
        private float _distanceSinceCheck;
        private bool _actionTakenSinceCheck;
        private float _timeSinceCheck;

        /// <summary>
        /// Accumulates this tick and evaluates once the interval has elapsed. Returns true only on the
        /// tick a check completes; callers act on isPassive only when this returns true.
        /// </summary>
        public bool Update(float distanceThisTick, bool actionTakenThisTick, float deltaTime, SimMatchConfig config, out bool isPassive)
        {
            _distanceSinceCheck += distanceThisTick;
            _actionTakenSinceCheck |= actionTakenThisTick;
            _timeSinceCheck += deltaTime;

            isPassive = false;
            if (_timeSinceCheck < config.PassivityCheckIntervalSeconds)
            {
                return false;
            }

            isPassive = _distanceSinceCheck < config.PassivityMinTravelDistance || !_actionTakenSinceCheck;

            _distanceSinceCheck = 0f;
            _actionTakenSinceCheck = false;
            _timeSinceCheck = 0f;
            return true;
        }
    }
}
