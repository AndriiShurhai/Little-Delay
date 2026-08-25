using UnityEngine;
using Drewio.UnityToolkit.Core;

namespace LittleDelay.Simulation.Events
{
    public enum AbilityFailureReason
    {
        OnCooldown,
        Invalid
    }

    public enum ProjectileDespawnReason
    {
        HitFighter,
        HitArenaBounds,
        Expired
    }

    /// <summary>Raised the tick an abilit successfully fires (cooldown consumed). Ability identity is a string is so new abilities never require a new event type</summary>
    public readonly struct AbilityUsedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly string AbilityId;

        public AbilityUsedEvent(FighterSlot fighter, string abilityId)
        {
            Fighter = fighter;
            AbilityId = abilityId;
        }
    }

    ///<summary>Raised when an ability use is rejected (e.g. still on cooldown) so UI/audio can give feedback withough simulation silently swallowing the input.</summary>
    public readonly struct AbilityFailedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly string AbilityId;
        public readonly AbilityFailureReason Reason;

        public AbilityFailedEvent(FighterSlot fighter, string abilityId, AbilityFailureReason reason)
        {
            Fighter = fighter;
            AbilityId = abilityId;
            Reason = reason;
        }
    }

    public readonly struct AbilityCooldownStartedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly string AbilityId;
        public readonly float Duration;

        public AbilityCooldownStartedEvent(FighterSlot fighter, string abilityId, float duration)
        {
            Fighter = fighter;
            AbilityId = abilityId;
            Duration = duration;
        }
    }

    /// <summary>
    /// Raised the moment a cooldown becomes ready again. UI does NOT need this to
    /// draw the fill - AbilitySlotUI samples CooldownNomralizedRemaining every
    /// frame (pull model) - but it's useful for a one-shot "ready" flash/SFX cue.
    /// </summary>
    public readonly struct AbilityReadyEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly string AbilityId;

        public AbilityReadyEvent(FighterSlot fighter, string abilityId)
        {
            Fighter = fighter;
            AbilityId = abilityId;
        }
    }

    /// <summary>
    /// Raised by simulation spawning (never by a View calling Instantiate
    /// directly) when a projectile enters the sim. ProectileView listens for
    /// this to pull an instance from its GameObjectPool and snap it to Position.
    /// </summary>
    public readonly struct ProjectileSpawnedEvent : IGameEvent
    {
        public readonly int ProjectileId;
        public readonly FighterSlot Owner;
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;

        public ProjectileSpawnedEvent(int projectileId, FighterSlot owner, Vector3 position, Vector3 velocity)
        {
            ProjectileId = projectileId;
            Owner = owner;
            Position = position;
            Velocity = velocity;
        }
    }
    /// <summary>Raised on deflection so presentation can play an impact/redirect VFX without owning any deflection math itself (that math is a simulation determinism concern - Phase 2/3).</summary>
    public readonly struct ProjectileDeflectedEvent : IGameEvent
    {
        public readonly int ProjectileId;
        public readonly Vector3 NewVelocity;

        public ProjectileDeflectedEvent(int projectileId, Vector3 newVelocity)
        {
            ProjectileId = projectileId;
            NewVelocity = newVelocity;
        }
    }

    /// <summary>ProjectileView listens for this to Release() its instance back to the GameObjectPool - simulation decides the projectile's lifetime is over, view never decides that on its own.</summary>
    public readonly struct ProjectileDespawnedEvent : IGameEvent
    {
        public readonly int ProjectileId;
        public readonly ProjectileDespawnReason Reason;

        public ProjectileDespawnedEvent(int projectileId, ProjectileDespawnReason reason)
        {
            ProjectileId = projectileId;
            Reason = reason;
        }
    }

}