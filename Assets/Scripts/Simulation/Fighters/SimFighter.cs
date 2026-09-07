using System.Collections.Generic;
using UnityEngine;
using LittleDelay.Simulation.Abilities;
using LittleDelay.Simulation.Events;
using UnityEditor.ShaderGraph.Internal;
using Unity.VisualScripting;

namespace LittleDelay.Simulation.Fighters
{
    /// <summary>
    /// Engine-agnostic fighter state: position, velocity, aim, healt, active
    /// deflection, and owned abilities. No MonoBehaviour, no Update() - SimWorld
    /// is the only thing that mutates this class, via the internal members
    /// below, once per Tick. FighterView reads from this or listens to
    /// the events SimWorld/SimCombat publish; it never mutates it.
    /// </summary>
    
    public sealed class SimFighter
    {
        private readonly Dictionary<string, SimAbility> _abilities = new();

        private readonly SimPassivityTracker _passivityTracker = new();

        public FighterSlot Slot { get; }
        public float MaxHealth { get; }

        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        /// <summar> Non-zero only while an external push (e.g. a projectile knock) is active; added on top of input-driven velocity, then decays to zero on its own.</summary>
        public Vector3 DeflectionVelocity { get; private set; }
        public float DeflectionTimeRemaining { get; private set; }
        public IReadOnlyCollection<SimAbility> Abilities => _abilities.Values;

        public SimFighter(FighterSlot slot, Vector3 spawnPositon, float maxHealth)
        {
            Slot = slot;
            Position = spawnPositon;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void AddAbility(SimAbility ability) => _abilities[ability.AbilityId] = ability;

        public bool TryGetAbility(string abilityId, out SimAbility ability) => _abilities.TryGetValue(abilityId, out ability);

        internal SimPassivityTracker PassivityTracker => _passivityTracker;

        /// <summary>Called once per tick by SimWorld after movement/arena clamping. AimDirection only updates when the input actually supplied one - a zero AimDirection means "keep facing where your were"</summary>
        internal void SetKinematics(Vector3 position, Vector3 velocity, Vector3 aimDirection)
        {
            Position = position;
            Velocity = velocity;

            if (aimDirection.sqrMagnitude > 0.0001f)
            {
                AimDirection = aimDirection.normalized;
            }
        }

        /// <summary>
        /// Starts (or overrides) an external push. Recordings replay their
        /// original input exactly regardless of this - deflection only ever
        /// adds to the velocity that input-driven movement produces, so once it
        /// decays to zero the fighter simply continues on from wherever the
        /// push left it with no snap-back to a "should have been here"
        /// position.
        /// </summary>
        internal void ApplyDeflection(Vector3 deflectionVelocity, float durationSeconds)
        {
            DeflectionVelocity = deflectionVelocity;
            DeflectionTimeRemaining = durationSeconds;
        }

        internal void TickDeflection(float deltaTime)
        {
            if (DeflectionTimeRemaining <= 0f)
            {
                DeflectionVelocity = Vector3.zero;
                return;
            }

            DeflectionTimeRemaining = Mathf.Max(0f, DeflectionTimeRemaining - deltaTime);
            if (DeflectionTimeRemaining <= 0f)
            {
                DeflectionVelocity = Vector3.zero;
            }
        }

        /// <summary>Clamped to zero; never goes negative. Use SimCombat.ApplyDamage rather than calling this directly - it also publishes the corresponding events</summary>
        internal void ReduceHealth(float amount)
        {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        }
    }
}