using System;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using UnityEngine;

namespace LittleDelay.Simulation.Abilities
{
    /// <summary>
    /// The prototype's single weapon: a small, high-fire-rate projectile along the fighter's aim
    /// direction. No Instantiate anywhere - spawning goes through SimWorld.SpawnProjectile, which
    /// publishes ProjectileSpawnedEvent for the view layer's pool.
    /// </summary>
    public sealed class SimBlastAbility : SimAbility
    {
        public const string Id = "Blast";
        public override string AbilityId => Id;

        private readonly SimMatchConfig _config;

        public SimBlastAbility(FighterSlot ownerSlot, IEventBus eventBus, SimMatchConfig config)
            : base(ownerSlot, eventBus, (config ?? throw new ArgumentNullException(nameof(config))).BlastAbilityCooldownSeconds)
        {
            _config = config;
        }

        protected override void OnUse(SimFighter owner, SimWorld world)
        {
            Vector3 spawnPosition = owner.Position + owner.AimDirection * _config.ProjectileSpawnOffset;
            Vector3 velocity = owner.AimDirection * _config.ProjectileSpeed;
            world.SpawnProjectile(owner.Slot, spawnPosition, velocity);
        }
    }
}
