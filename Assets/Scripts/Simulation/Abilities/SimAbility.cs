using System;
using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.Timers;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;

namespace LittleDelay.Simulation.Abilities
{
    /// <summary>
    /// Pure C# ability (replaces the old BaseAbility MonoBehaviour): no Update(), no MonoBehaviour.
    /// Cooldown tracking is the toolkit's Cooldown - pull-model NormalizedRemaining for UI, push-model
    /// Ready event for one-shot cues - ticked explicitly by SimWorld inside the fixed-tick loop.
    /// One instance per owning fighter, so cooldown state is never shared between the two fighters.
    /// </summary>
    public abstract class SimAbility
    {
        private readonly Cooldown _cooldown;
        private readonly IEventBus _eventBus;

        public FighterSlot OwnerSlot { get; }
        public abstract string AbilityId { get; }

        public bool IsReady => _cooldown.IsReady;

        /// <summary>1 right after use, 0 once ready - sample every frame for a cooldown fill, no event wiring needed.</summary>
        public float NormalizedRemaining => _cooldown.NormalizedRemaining;

        public float CooldownDuration => _cooldown.Duration;

        protected SimAbility(FighterSlot ownerSlot, IEventBus eventBus, float cooldownSeconds)
        {
            OwnerSlot = ownerSlot;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _cooldown = new Cooldown(cooldownSeconds);
            _cooldown.Ready += () => _eventBus.Publish(new AbilityReadyEvent(OwnerSlot, AbilityId));
        }

        public void Tick(float deltaTime) => _cooldown.Tick(deltaTime);

        /// <summary>
        /// Attempts to use the ability. On success: consumes the cooldown, runs the gameplay effect, and
        /// publishes AbilityUsedEvent + AbilityCooldownStartedEvent. On failure: publishes AbilityFailedEvent
        /// when reportFailure is set (SimWorld passes true only on the input's rising edge so a held button
        /// does not spam the bus).
        /// </summary>
        public bool TryUse(SimFighter owner, SimWorld world, bool reportFailure = true)
        {
            if (!_cooldown.TryConsume())
            {
                if (reportFailure)
                {
                    _eventBus.Publish(new AbilityFailedEvent(OwnerSlot, AbilityId, AbilityFailureReason.OnCooldown));
                }

                return false;
            }

            OnUse(owner, world);

            _eventBus.Publish(new AbilityUsedEvent(OwnerSlot, AbilityId));
            _eventBus.Publish(new AbilityCooldownStartedEvent(OwnerSlot, AbilityId, _cooldown.Duration));
            return true;
        }

        /// <summary>The actual gameplay effect.</summary>
        protected abstract void OnUse(SimFighter owner, SimWorld world);
    }
}
