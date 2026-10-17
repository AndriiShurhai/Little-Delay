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

            Vector3 toTarget3D = predicted - _self.Position;
            float distance3D = toTarget3D.magnitude;
            if (distance3D < 0.0001f) return SimInputFrame.None;

            // Full 3D aim so the bot can shoot upward at a jumping opponent or downward if elevated.
            Vector3 aim = toTarget3D / distance3D;

            // Horizontal plane vector for ground movement and strafing.
            Vector3 toTargetFlat = new Vector3(toTarget3D.x, 0f, toTarget3D.z);
            float flatDistance = toTargetFlat.magnitude;
            Vector3 flatDir = flatDistance > 0.0001f ? toTargetFlat / flatDistance : Vector3.forward;

            float radial = flatDistance > _ai.EngageDistance ? 1f : (flatDistance < _ai.RetreatDistance ? -1f : 0f);
            float strafeSign = (tickIndex / _strafeSwitchTicks) % 2 == 0 ? 1f : -1f;
            Vector3 strafe = Vector3.Cross(Vector3.up, flatDir) * (strafeSign * _ai.StrafeStrength);
            Vector3 move = SimMovement.ClampMoveInput(flatDir * radial + strafe);

            bool fire = distance3D <= _ai.FireRange
                        && Vector3.Angle(_self.AimDirection, aim) <= _ai.FireConeDegrees
                        && IsReady(_self.Loadout.WeaponId);

            bool useAbility1 = tickIndex > 0
                               && tickIndex % _dashIntervalTicks == 0
                               && flatDistance > _ai.EngageDistance * 1.5f
                               && IsReady(_self.Loadout.Ability1Id);

            return new SimInputFrame(move, aim, fire, useAbility1);
        }

        private bool IsReady(string abilityId) =>
            !string.IsNullOrEmpty(abilityId) && _self.TryGetAbility(abilityId, out SimAbility ability) && ability.IsReady;
    } 
}
