using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using System.Collections.Generic;
using UnityEngine;

namespace LittleDelay.Simulation.Combat
{
    ///<summary>
    /// All projectile movement, collsions, and damage resolution. Replaces the
    /// old per-projectile Physics-trigger callbacks (Prjoectile.OnTriggerEnter)
    /// with plain distance checks run centraylly, once per tick, in a fixed
    /// order - which is itself part of what makes this deterministic: the same
    /// projectile set in the same order always resolves the same way, unlike
    /// physics-engine trigger callback ordering.
    ///</summary>
    public sealed class SimCombat
    {
        private readonly SimMatchConfig _config;
        private readonly IEventBus _eventBus;

        public SimCombat(SimMatchConfig config, IEventBus eventBus)
        {
            _config =  config ?? throw new System.ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
        }

        /// <summary>Integrates position and expires anything that has outlived ProjectileLifetimeSeconds</summary>
        public void AdvanceProjectiles(List<SimProjectile> projectiles, float deltaTime)
        {
            foreach (SimProjectile projectile in projectiles)
            {
                if (projectile.IsExpired) continue;

                projectile.Position += projectile.Velocity * deltaTime;
                projectile.ElapsedSeconds += deltaTime;

                if (projectile.ElapsedSeconds >= _config.ProjectileLifeTimeSeconds)
                {
                    projectile.IsExpired = true;
                    _eventBus.Publish(new ProjectileDespawnedEvent(projectile.Id, ProjectileDespawnReason.Expired));
                }
            }
        }

        /// <summary>A projectile only ever damages the fighter it wasn't fired by - friendly fire isn't a concept here since each fighter has exactly one opponent</summary>
        public void ResolveProjectileFighterCollisions(List<SimProjectile> projectiles, SimFighter player, SimFighter opponent)
        {
            float hitDistance = _config.ProjectileRadius + _config.FighterHurtboxRadius;
            float hitDistanceSq = hitDistance * hitDistance;

            foreach (SimProjectile projectile in projectiles)
            {
                if (projectile.IsExpired) continue;

                SimFighter target = projectile.Owner == FighterSlot.Player ? opponent : player;
                if (!target.IsAlive) continue;

                if ((projectile.Position - target.Position).sqrMagnitude > hitDistanceSq) continue;

                ApplyDamage(target, _config.ProjectileDamage, DamageSource.Projectile);

                // Knockback along the projectile's own travel direction. This is 
                // additive on top of whatever input-driven movement the target's 
                // recording plays back for the ticks it's active ( see
                // SimFighter.ApplyDeflection) - once it decays to zero the
                // recording continues from the pushed position rather than 
                // snapping back, matching the confirmed playback behaviour.
                Vector3 knockbackDirection = projectile.Velocity.sqrMagnitude > 0.0001f
                    ? projectile.Velocity.normalized
                    : (target.Position - projectile.Position).normalized;
                target.ApplyDeflection(knockbackDirection * _config.DeflectionKnockbackSpeed, _config.DeflectionDurationSeconds);

                projectile.IsExpired = true;
                _eventBus.Publish(new ProjectileDespawnedEvent(projectile.Id, ProjectileDespawnReason.HitFighter));
            }

        }

        /// <summary>
        /// Opposing projectiles that collide deflect off each other via a
        /// straightforward vector reflection.
        /// </summary>
        public void ResolveProjectileProjectileCollisions(List<SimProjectile> projectiles)
        {
            float combinedRadius = _config.ProjectileRadius * 2f;
            float combinedRadiusSq = combinedRadius * combinedRadius;

            for (int i = 0; i < projectiles.Count; i++)
            {
                SimProjectile a = projectiles[i];
                if (a.IsExpired) continue;

                for (int j = i + 1; j < projectiles.Count; j++)
                {
                    SimProjectile b = projectiles[j];
                    if (b.IsExpired || b.Owner == a.Owner) continue;

                    Vector3 delta = b.Position - a.Position;
                    if (delta.sqrMagnitude > combinedRadiusSq) continue;

                    Vector3 normal = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward;

                    Vector3 newVelocityA = ComputeDeflection(a.Velocity, -normal);
                    Vector3 newVelocityB = ComputeDeflection(b.Velocity, normal);
                    a.Velocity = newVelocityA;
                    b.Velocity = newVelocityB;

                    _eventBus.Publish(new ProjectileDeflectedEvent(a.Id, newVelocityA));
                    _eventBus.Publish(new ProjectileDeflectedEvent(b.Id, newVelocityB));
                }
            }
        }

        public void ResolveArenaBoundsForProjectiles(List<SimProjectile> projectiles, Vector3 arenaCenter, float arenaRadius)
        {
            float boundsRadius = arenaRadius + _config.ProjectileArenaExitMargin;
            foreach(SimProjectile projectile in projectiles)
            {
                if (projectile.IsExpired) continue;

                Vector3 offset = projectile.Position - arenaCenter;
                if (offset.magnitude <= boundsRadius) continue;

                projectile.IsExpired = true;
                _eventBus.Publish(new ProjectileDespawnedEvent(projectile.Id, ProjectileDespawnReason.HitArenaBounds));
            }
        }

        /// <summary>Standard vector reflection - deterministic given the same incoming velocity and collision normal, with no random component</summary>
        public static Vector3 ComputeDeflection(Vector3 incomingVelocity, Vector3 collisionNormal)
        {
            Vector3 normal = collisionNormal.sqrMagnitude > 0.0001f
                ? collisionNormal.normalized
                : -incomingVelocity.normalized;

            return Vector3.Reflect(incomingVelocity, normal);
        }

        /// <summary>
        /// Applies damage and publishes the full event sequence a target expedcts
        /// (FighterDamagedEvent, thenHealthChangedEevnt, then FighterDiedEvent
        /// if this was lethal). Used for both projectile hits and passivity
        /// damage, so every damage path - present and future - stays consistent
        /// through this one method rather than each caller reimplementing it.
        /// </summary>
        public void ApplyDamage(SimFighter target, float amount, DamageSource source)
        {
            if (!target.IsAlive) return;

            target.ReduceHealth(amount);

            _eventBus.Publish(new FighterDamagedEvent(target.Slot, amount, source));
            _eventBus.Publish(new HealthChangedEvent(target.Slot, target.CurrentHealth, target.MaxHealth));

            if (!target.IsAlive)
            {
                _eventBus.Publish(new FighterDiedEvent(target.Slot, source));
            }
        }
    }
}