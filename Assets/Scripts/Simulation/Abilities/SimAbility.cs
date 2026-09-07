using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.Timers;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using System;
using Unity.Multiplayer.Center.Common;
using UnityEngine;

namespace LittleDelay.Simulation.Abilities
{
    /// <summary>
    /// Pure C# ability, replacing the old BaseAbility MonoBehaviour: no 
    /// inheritance from MonoBehaviour, no Update() ticking - cooldown tracking
    /// is the toolkit's own Cooldown (pull-model NormalizedRemaining for UI,
    /// push-model Ready event for one-shot cues), ticked explicitly by SimWorld
    /// alongside everything else in the fixed-tick loop.
    /// 
    /// One instance per owning fighter (constructed with that fighter's 
    /// FighterSlot at construction time), so its Cooldown state is never shared
    /// between the two fighters the way a MonoBehaviour-per-prefab-instance
    /// pattern might accidentally allow.
    /// </summary>
    public abstract class SimAbility
    {
        private readonly Cooldown _cooldown;
        private readonly IEventBus _eventBus;

        public FighterSlot OwnerSlot { get; }
        public abstract string AbilityId { get; }

        public bool IsReady => _cooldown.IsReady;

        ///<summary>1 right after use, 0 once ready - smaple this every frame for a cooldown fill, no evnt wiring needed.</summary>
        public float NormalizedRemaining => _cooldown.NormalizedRemaining;
        protected SimAbility(FighterSlot ownerSlot, IEventBus eventBus, float cooldownSeconds)
        {
            OwnerSlot = ownerSlot;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _cooldown = new Cooldown(cooldownSeconds);
            _cooldown.Ready += () => _eventBus.Publish(new AbilityReadyEvent(OwnerSlot, AbilityId));
        }

        public void Tick(float deltaTime) => _cooldown.Tick(deltaTime);

        /// <summary>
        /// Attempts to use the ability. On success, consumes the cooldown,
        /// invokes the subclass's gameplay effect, and published
        /// AbilityUsedEvent and AbilityCooldownStartedEvent. On failure, publishes
        /// AbilityFailedEvent instead - the input is never
        /// silently swallowed, so UI/Audio can always give feedback either way.
        /// </summary>
        public bool TryUse(SimFighter owner, SimWorld world)
        {
            if (!_cooldown.TryConsume())
            {
                _eventBus.Publish(new AbilityFailedEvent(OwnerSlot, AbilityId, AbilityFailureReason.OnCooldown));
                return false;
            }

            OnUse(owner, world);

            _eventBus.Publish(new AbilityUsedEvent(OwnerSlot, AbilityId));
            _eventBus.Publish(new AbilityCooldownStartedEvent(OwnerSlot, AbilityId, _cooldown.Duration));
            return true;
        }

        ///<summary>The actual gameplay effect</summary>
        protected abstract void OnUse(SimFighter owner, SimWorld world);
    }
}