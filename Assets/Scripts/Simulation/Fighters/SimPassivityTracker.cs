using LittleDelay.Simulation.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace LittleDelay.Simulation.Fighters
{
    /// <summary>
    /// Two independent checks, evaluated together every PassivityCheckIntervalSeconds;
    /// total distance traveled since the last check, and whether any action
    /// (firing) was taken since the last check. Failing either marks the fighter
    /// passive for that interval - moving without firing is passive, firing
    /// without moving is passive. 
    /// </summary>
    public sealed class SimPassivityTracker
    {
        private float _distanceSinceCheck;
        private bool _actionTakenSinceCheck;
        private float _timeSinceCheck;

        /// <summary>
        /// Accumulates this tick's movement/action, and evaluates passivity once
        /// the check interval has elapsed. Returns true only on the tick a check
        /// actually completes - callers should only act on <paramref name="isPassive"/>
        /// when this returns true.
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