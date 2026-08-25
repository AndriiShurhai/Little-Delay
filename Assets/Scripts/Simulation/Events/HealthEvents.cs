using Drewio.UnityToolkit.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace LittleDelay.Simulation.Events
{
    public enum DamageSource
    {
        Projectile,
        Passivity
    }

    ///<summary>
    /// Pull-friendly snapshot published on every health change, so
    /// BattleUIManager's health bar wires to one event type instead of
    /// separately handling damage/heal/passivity-tick cases itself.
    ///</summary>
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

    ///<summary>Raised alongside HealthChangedEvent specifically when the change was damage, carrying the source - lets a hit-VFX listener distinguish "hot shot" from "passivity tick" withough inferring it from the delta</summary>
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

    ///<summary>
    ///Passivity punishment is interval-based damage during the battle phase
    ///applied when either the travel-distance check or the 
    ///time-since-last-action check fails. Raised distinctly from
    ///FighterDamagedEvent so UI can show a specific "passive" indicator rather
    ///than just a generic damage number.
    ///</summary>
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