using Drewio.UnityToolkit.Core;

namespace LittleDelay.Simulation.Events
{
    public enum DamageSource
    {
        Projectile,
        Passivity
    }

    /// <summary>Snapshot published on every health change, so a health bar wires to one event type.</summary>
    public readonly struct HealthChangedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly float CurrentHealth;
        public readonly float MaxHealth;

        public HealthChangedEvent(FighterSlot fighter, float currentHealth, float maxHealth)
        {
            Fighter = fighter;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }

    /// <summary>Raised alongside HealthChangedEvent when the change was damage, carrying the source.</summary>
    public readonly struct FighterDamagedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly float Amount;
        public readonly DamageSource Source;

        public FighterDamagedEvent(FighterSlot fighter, float amount, DamageSource source)
        {
            Fighter = fighter;
            Amount = amount;
            Source = source;
        }
    }

    /// <summary>Raised when a passivity check fails, distinct from FighterDamagedEvent so UI can show a specific "passive" indicator.</summary>
    public readonly struct PassivityDetectedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;

        public PassivityDetectedEvent(FighterSlot fighter)
        {
            Fighter = fighter;
        }
    }

    public readonly struct FighterDiedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly DamageSource Cause;

        public FighterDiedEvent(FighterSlot fighter, DamageSource cause)
        {
            Fighter = fighter;
            Cause = cause;
        }
    }
}
