using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation;
using LittleDelay.Simulation.Abilities;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using System;
using UnityEngine;

namespace LittleDelay.Simulation.Abilities
{
    ///<summary>
    /// The prototype's single weapon: a small, high-fire-rate projectile fired
    /// along the fighter's current aim direaction. Replaces the old
    /// MonoBehaviour BlastAbility - no direct Instantiate here at all; spawning
    /// goes through SimWorld.SpawnProjectile, which publishes
    /// ProjectileSpawnedEvent for ProjectileView to pull an instance
    /// from it GameObject pool.
    ///</summary>
    
    public sealed class SimBlastAbility : SimAbility
    {
        public const string Id = "Blast";
        public override string AbilityId => Id;

        private readonly SimMatchConfig _config;

        public SimBlastAbility(FighterSlot ownerSlot, IEventBus eventBus, SimMatchConfig config)
            : base(ownerSlot, eventBus, config.BlastAbilityCooldownSeconds)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override void OnUse(SimFighter owner, SimWorld world)
        {
            Vector3 spawnPosition = owner.Position + owner.AimDirection * _config.ProjectileSpawnOffset;
            Vector3 velocity = owner.AimDirection * _config.ProjectileSpeed;
            world.SpawnProjectile(owner.Slot, spawnPosition, velocity);
        }
    }
}