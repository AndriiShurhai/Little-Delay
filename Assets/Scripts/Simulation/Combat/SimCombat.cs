using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using UnityEngine;

namespace LittleDelay.Simulation.Combat
{
    /// <summary>
    /// All projectile movement, collision and damage resolution. Replaces per-projectile physics
    /// trigger callbacks with swept distance checks run centrally, once per tick, in a fixed order -
    /// the same projectile list in the same order always resolves the same way, unlike physics-engine
    /// callback ordering. Collision tests are swept (segment vs sphere) so a fast projectile cannot
    /// tunnel through a target between two ticks.
    /// </summary>
    public sealed class SimCombat
    {
        private readonly SimMatchConfig _config;
        private readonly IEventBus _eventBus;

        public SimCombat(SimMatchConfig config, IEventBus eventBus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>Integrates position and expires anything that has outlived ProjectileLifeTimeSeconds.</summary>
        public void AdvanceProjectiles(List<SimProjectile> projectiles, float deltaTime)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                SimProjectile projectile = projectiles[i];
                if (projectile.IsExpired) continue;

                projectile.PreviousPosition = projectile.Position;
                projectile.Position += projectile.Velocity * deltaTime;
                projectile.ElapsedSeconds += deltaTime;

                if (projectile.ElapsedSeconds >= _config.ProjectileLifeTimeSeconds)
                {
                    projectile.IsExpired = true;
                    _eventBus.Publish(new ProjectileDespawnedEvent(projectile.Id, ProjectileDespawnReason.Expired));
                }
            }
        }

        /// <summary>
        /// Opposing projectiles that touch during a tick bounce off each other (vector reflection about the
        /// contact normal). Swept relative-motion test, resolved at the moment of closest approach, and only
        /// while the pair is actually closing - so a pair that has just bounced can never re-trigger.
        /// </summary>
        public void ResolveProjectileProjectileCollisions(List<SimProjectile> projectiles, float deltaTime)
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

                    Vector3 relativeStart = b.PreviousPosition - a.PreviousPosition;
                    Vector3 relativeVelocity = b.Velocity - a.Velocity;

                    // Only a pair that is actually approaching can collide. Judged from the relative position at the
                    // START of the tick, so a pair that just bounced (coincident or separating) can never re-trigger.
                    if (Vector3.Dot(relativeStart, relativeVelocity) >= 0f) continue;

                    Vector3 relativeEnd = b.Position - a.Position;
                    float t = ClosestApproachFraction(relativeStart, relativeEnd, out float closestDistanceSq);
                    if (closestDistanceSq > combinedRadiusSq) continue;

                    Vector3 contactA = Vector3.Lerp(a.PreviousPosition, a.Position, t);
                    Vector3 contactB = Vector3.Lerp(b.PreviousPosition, b.Position, t);

                    // Normal points a -> b. Exactly head-on projectiles are coincident at closest approach,
                    // so fall back to the approach direction (non-zero, because they are closing).
                    Vector3 delta = contactB - contactA;
                    Vector3 normal = delta.sqrMagnitude > 0.000001f ? delta.normalized : relativeStart.normalized;

                    Vector3 newVelocityA = ComputeDeflection(a.Velocity, normal);
                    Vector3 newVelocityB = ComputeDeflection(b.Velocity, normal);
                    float remaining = (1f - t) * deltaTime;

                    a.PreviousPosition = contactA;
                    b.PreviousPosition = contactB;
                    a.Velocity = newVelocityA;
                    b.Velocity = newVelocityB;
                    a.Position = contactA + newVelocityA * remaining;
                    b.Position = contactB + newVelocityB * remaining;

                    _eventBus.Publish(new ProjectileDeflectedEvent(a.Id, newVelocityA));
                    _eventBus.Publish(new ProjectileDeflectedEvent(b.Id, newVelocityB));
                }
            }
        }

        /// <summary>A projectile only damages the fighter it wasn't fired by - each fighter has exactly one opponent.</summary>
        public void ResolveProjectileFighterCollisions(List<SimProjectile> projectiles, SimFighter player, SimFighter opponent)
        {
            float hitDistance = _config.ProjectileRadius + _config.FighterHurtboxRadius;
            float hitDistanceSq = hitDistance * hitDistance;

            for (int i = 0; i < projectiles.Count; i++)
            {
                SimProjectile projectile = projectiles[i];
                if (projectile.IsExpired) continue;

                SimFighter target = projectile.Owner == FighterSlot.Player ? opponent : player;
                if (!target.IsAlive) continue;

                Vector3 relativeStart = projectile.PreviousPosition - target.Position;
                Vector3 relativeEnd = projectile.Position - target.Position;
                ClosestApproachFraction(relativeStart, relativeEnd, out float closestDistanceSq);
                if (closestDistanceSq > hitDistanceSq) continue;

                ApplyDamage(target, _config.ProjectileDamage, DamageSource.Projectile);

                // Knockback along the projectile's travel direction, additive on top of whatever input the
                // target's recording is playing - once it ends the recording simply continues from the pushed position.
                Vector3 travel = projectile.Velocity;
                travel.y = 0f;
                Vector3 knockbackDirection = travel.sqrMagnitude > 0.0001f
                    ? travel.normalized
                    : (target.Position - projectile.Position).normalized;
                target.ApplyDeflection(knockbackDirection * _config.DeflectionKnockbackSpeed, _config.DeflectionDurationSeconds);

                projectile.IsExpired = true;
                _eventBus.Publish(new ProjectileDespawnedEvent(projectile.Id, ProjectileDespawnReason.HitFighter));
            }
        }

        public void ResolveArenaBoundsForProjectiles(List<SimProjectile> projectiles, Vector3 arenaCenter, float arenaRadius)
        {
            float boundsRadius = arenaRadius + _config.ProjectileArenaExitMargin;
            float boundsRadiusSq = boundsRadius * boundsRadius;

            for (int i = 0; i < projectiles.Count; i++)
            {
                SimProjectile projectile = projectiles[i];
                if (projectile.IsExpired) continue;

                Vector3 offset = projectile.Position - arenaCenter;
                offset.y = 0f;
                if (offset.sqrMagnitude <= boundsRadiusSq) continue;

                projectile.IsExpired = true;
                _eventBus.Publish(new ProjectileDespawnedEvent(projectile.Id, ProjectileDespawnReason.HitArenaBounds));
            }
        }

        /// <summary>Standard vector reflection - deterministic given the same inputs, with no random component.</summary>
        public static Vector3 ComputeDeflection(Vector3 incomingVelocity, Vector3 collisionNormal)
        {
            Vector3 normal = collisionNormal.sqrMagnitude > 0.0001f
                ? collisionNormal.normalized
                : -incomingVelocity.normalized;

            return Vector3.Reflect(incomingVelocity, normal);
        }

        /// <summary>
        /// Applies damage and publishes the full event sequence (FighterDamagedEvent, HealthChangedEvent,
        /// then FighterDiedEvent if lethal). Used for projectile hits and passivity damage alike, so every
        /// damage path stays consistent through this one method.
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

        /// <summary>Fraction t in [0,1] along the segment start->end at which the point is closest to the origin.</summary>
        private static float ClosestApproachFraction(Vector3 start, Vector3 end, out float closestDistanceSq)
        {
            Vector3 direction = end - start;
            float lengthSq = direction.sqrMagnitude;
            float t = lengthSq > 0.00000001f ? Mathf.Clamp01(-Vector3.Dot(start, direction) / lengthSq) : 0f;
            closestDistanceSq = (start + direction * t).sqrMagnitude;
            return t;
        }
    }
}
