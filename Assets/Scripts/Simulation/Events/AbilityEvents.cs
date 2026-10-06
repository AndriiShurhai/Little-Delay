using Drewio.UnityToolkit.Core;
using UnityEngine;

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

    /// <summary>Raised the tick an ability successfully fires (cooldown consumed). Ability identity is a string so new abilities never need a new event type.</summary>
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

    /// <summary>
    /// Raised when an ability use is rejected (e.g. still on cooldown) so UI/audio can give feedback.
    /// Only published on the rising edge of the input (the tick a held button starts asking), not on every held tick.
    /// </summary>
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
    /// Raised the moment a cooldown becomes ready again. UI does not need this to draw
    /// the fill (it samples NormalizedRemaining every frame) - it is for one-shot cues.
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

    /// <summary>Raised by simulation spawning when a projectile enters the world; ProjectileViewManager pulls a pooled view for it.</summary>
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

    /// <summary>Raised on projectile-vs-projectile deflection so presentation can play impact VFX without owning any deflection math.</summary>
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

    /// <summary>The simulation decides a projectile's life is over; the view releases its pooled instance in response.</summary>
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
