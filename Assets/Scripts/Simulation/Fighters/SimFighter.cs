using System.Collections.Generic;
using LittleDelay.Simulation.Abilities;
using LittleDelay.Simulation.Events;
using UnityEngine;

namespace LittleDelay.Simulation.Fighters
{
    /// <summary>
    /// Engine-agnostic fighter state: position, velocity, aim, health, active push effects and
    /// owned abilities. SimWorld is the only thing that mutates this class (via the internal
    /// members), once per tick. Views read it; they never write it.
    /// </summary>
    public sealed class SimFighter
    {
        // List for deterministic iteration order; the dictionary is only for lookup.
        private readonly List<SimAbility> _abilityList = new();
        private readonly Dictionary<string, SimAbility> _abilitiesById = new();

        public FighterSlot Slot { get; }
        public float MaxHealth { get; }

        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 AimDirection { get; private set; }

        /// <summary>State at the start of the current tick - views lerp PreviousPosition -> Position with SimTickClock.Alpha.</summary>
        public Vector3 PreviousPosition { get; private set; }
        public Vector3 PreviousAimDirection { get; private set; }

        /// <summary>This tick's input-driven move direction (clamped to unit length). Dash uses it as its direction.</summary>
        public Vector3 MoveInput { get; private set; }

        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        /// <summary>Non-zero only while an external push (projectile knockback) is active; added on top of input-driven velocity, then ends on its own.</summary>
        public Vector3 DeflectionVelocity { get; private set; }
        public float DeflectionTimeRemaining { get; private set; }

        /// <summary>Non-zero only while a dash is decaying.</summary>
        public Vector3 DashVelocity { get; private set; }

        public IReadOnlyList<SimAbility> Abilities => _abilityList;

        internal SimPassivityTracker PassivityTracker { get; } = new();

        // Edge detection so a held button only reports "ability failed" once, not 50 times a second.
        internal bool FireWasRequested { get; set; }
        internal bool MobilityWasRequested { get; set; }

        public SimFighter(FighterSlot slot, Vector3 spawnPosition, Vector3 spawnAim, float maxHealth, float startHealth = -1f)
        {
            Slot = slot;
            Position = spawnPosition;
            PreviousPosition = spawnPosition;
            MaxHealth = maxHealth;
            CurrentHealth = startHealth > 0f ? Mathf.Min(startHealth, maxHealth) : maxHealth;

            spawnAim.y = 0f;
            AimDirection = spawnAim.sqrMagnitude > 0.0001f ? spawnAim.normalized : Vector3.forward;
            PreviousAimDirection = AimDirection;
        }

        public void AddAbility(SimAbility ability)
        {
            if (_abilitiesById.ContainsKey(ability.AbilityId))
            {
                _abilityList.Remove(_abilitiesById[ability.AbilityId]);
            }

            _abilitiesById[ability.AbilityId] = ability;
            _abilityList.Add(ability);
        }

        public bool TryGetAbility(string abilityId, out SimAbility ability) => _abilitiesById.TryGetValue(abilityId, out ability);

        /// <summary>Called first thing each tick so Previous* always holds the last tick's end state.</summary>
        internal void BeginTick()
        {
            PreviousPosition = Position;
            PreviousAimDirection = AimDirection;
        }

        /// <summary>This tick's clamped move input and aim. AimDirection only changes when the input supplied one (flattened to XZ).</summary>
        internal void SetInput(Vector3 moveInput, Vector3 aimDirection)
        {
            MoveInput = moveInput;

            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude > 0.0001f)
            {
                AimDirection = aimDirection.normalized;
            }
        }

        /// <summary>Position is already arena-clamped.</summary>
        internal void SetMotion(Vector3 position, Vector3 velocity)
        {
            Position = position;
            Velocity = velocity;
        }

        /// <summary>
        /// Starts (or overrides) an external push. Recorded input keeps playing during it: deflection
        /// only adds to the velocity input produces, so once it ends the fighter simply carries on from
        /// wherever the push left it, with no snap-back.
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

        internal void ApplyDash(Vector3 dashVelocity)
        {
            DashVelocity += dashVelocity;
        }

        internal void TickDash(float deltaTime, float frictionPerSecond)
        {
            DashVelocity = SimMovement.DecayToZero(DashVelocity, frictionPerSecond * deltaTime);
        }

        /// <summary>Clamped to zero. Use SimCombat.ApplyDamage rather than calling this directly - it also publishes the events.</summary>
        internal void ReduceHealth(float amount)
        {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        }
    }
}
