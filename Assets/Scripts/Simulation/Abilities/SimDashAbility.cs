using System;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using UnityEngine;

namespace LittleDelay.Simulation.Abilities
{
    /// <summary>
    /// The mobility slot (port of the old DashAbility): an impulse along the current move input - or the
    /// aim direction when standing still - that decays linearly under SimFighter.TickDash.
    /// </summary>
    public sealed class SimDashAbility : SimAbility
    {
        public const string Id = "Dash";
        public override string AbilityId => Id;

        private readonly SimMatchConfig _config;

        public SimDashAbility(FighterSlot ownerSlot, IEventBus eventBus, SimMatchConfig config)
            : base(ownerSlot, eventBus, (config ?? throw new ArgumentNullException(nameof(config))).DashCooldownSeconds)
        {
            _config = config;
        }

        protected override void OnUse(SimFighter owner, SimWorld world)
        {
            Vector3 direction;

            if (owner.MoveInput.sqrMagnitude > 0.0001f)
            {
                direction = owner.MoveInput.normalized;
            }
            else
            {
                Vector3 flatAim = new Vector3(owner.AimDirection.x, 0f, owner.AimDirection.z);
                direction = flatAim.sqrMagnitude > 0.0001f ? flatAim.normalized : Vector3.forward;
            }
            owner.ApplyDash(direction * _config.DashSpeed);
        }
    }
}
