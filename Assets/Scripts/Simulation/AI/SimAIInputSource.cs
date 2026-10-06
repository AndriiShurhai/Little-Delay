using System;
using LittleDelay.Simulation.Abilities;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using LittleDelay.Simulation.Reveal;
using UnityEngine;

namespace LittleDelay.Simulation.AI
{
    /// <summary>
    /// Recording-phase bot. A pure function of (tick index, its own state, its intel): no Random, no clocks,
    /// no engine types beyond Vector3, so the same inputs always produce the same SimInputFrame.
    /// It does NOT read the opponent's live state - only what a reveal window showed it, extrapolated by the
    /// last observed velocity. That is the same information a human gets, which is the whole premise of
    /// recording "blind".
    /// </summary>
    public sealed class SimAIInputSource : ISimInputSource
    {
        private readonly SimFighter _self;
        private readonly FighterSlot _targetSlot;
        private readonly ISimIntelSource _intel;
        private readonly SimAIConfig _ai;
        private readonly float _dt;
        private readonly int _strafeSwitchTicks;
        private readonly int _dashIntervalTicks;

        public SimAIInputSource(SimFighter self, FighterSlot targetSlot, ISimIntelSource intel, SimAIConfig ai, SimMatchConfig matchConfig)
        {
            _self = self ?? throw new ArgumentNullException(nameof(self));
            _intel = intel ?? throw new ArgumentNullException(nameof(intel));
            _ai = ai ?? throw new ArgumentNullException(nameof(ai));
            if (matchConfig == null) throw new ArgumentNullException(nameof(matchConfig));

            _targetSlot = targetSlot;
            _dt = matchConfig.FixedDeltaTime;
            _strafeSwitchTicks = Mathf.Max(1, Mathf.RoundToInt(ai.StrafeSwitchSeconds / _dt));
            _dashIntervalTicks = Mathf.Max(1, Mathf.RoundToInt(ai.DashIntervalSeconds / _dt));
        }

        public SimInputFrame GetInput(int tickIndex)
        {
            SimIntel intel = _intel.GetIntelAbout(_targetSlot);
            if (!intel.IsKnown) return SimInputFrame.None;

            float ageSeconds = Mathf.Min(Mathf.Max(0, tickIndex - intel.ObservedTick) * _dt, _ai.PredictionHorizonSeconds);
            Vector3 predicted = intel.Position + intel.Velocity * ageSeconds;

            Vector3 toTarget = predicted - _self.Position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance < 0.0001f) return SimInputFrame.None;

            Vector3 aim = toTarget / distance;

            float radial = distance > _ai.EngageDistance ? 1f : (distance < _ai.RetreatDistance ? -1f : 0f);
            float strafeSign = (tickIndex / _strafeSwitchTicks) % 2 == 0 ? 1f : -1f;
            Vector3 strafe = Vector3.Cross(Vector3.up, aim) * (strafeSign * _ai.StrafeStrength);
            Vector3 move = SimMovement.ClampMoveInput(aim * radial + strafe);

            bool fire = distance <= _ai.FireRange
                        && Vector3.Angle(_self.AimDirection, aim) <= _ai.FireConeDegrees
                        && IsReady(SimBlastAbility.Id);

            bool dash = tickIndex > 0
                        && tickIndex % _dashIntervalTicks == 0
                        && distance > _ai.EngageDistance * 1.5f
                        && IsReady(SimDashAbility.Id);

            return new SimInputFrame(move, aim, fire, dash);
        }

        private bool IsReady(string abilityId) => _self.TryGetAbility(abilityId, out SimAbility ability) && ability.IsReady;
    }
}
