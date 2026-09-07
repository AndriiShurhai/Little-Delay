using LittleDelay.Simulation.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine;

namespace LittleDelay.Simulation.Fighters
{
    /// <summary>
    /// Pure, stateless kinmatic movement math. Delibaretely does not touch
    /// Rigidbody/PhysX - per the determinism requirements, neither is guaranteed
    /// bit-for-bit reproducible, so fighter movement is a simple, auditable
    /// integrator isntead: velocity from input, position += velocity * dt,
    /// clamped to the current arena radius. The old inward-moving wall colliders
    /// become a deterministic clamp against a shrinking radius computed straight
    /// from elapsed round time - no physics push involved
    /// </summary>
    /// 
    public static class SimMovement
    {
        public static Vector3 ComputeVelocity(SimInputFrame input, SimMatchConfig config)
        {
            Vector3 moveDirection = input.MoveDirection;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }
            return moveDirection * config.FighterMoveSpeed;
        }

        public static Vector3 Integrate(Vector3 currentPosition, Vector3 velocity, float deltaTime)
        {
            return currentPosition + velocity * deltaTime;
        }

        /// <summary>Deterministic replacement for the old physical arena walls: pushes a position back to the arena boundary along the same direaction it exceeded it from.</summary>
        public static Vector3 ClampToArena(Vector3 position, Vector3 arenaCenter, float arenaRadius)
        {
            Vector3 offset = position - arenaCenter;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance <= arenaRadius || distance <= 0.0001f)
            {
                return position;
            }

            Vector3 clamped = arenaCenter + offset.normalized * arenaRadius;
            clamped.y = position.y;
            return clamped;
        }

        ///<summary>Arena radius as a pure function of elapsed round time - smae input always produces the same radius, unlike a physically-simulated shrinking wall.</summary>
        public static float ComputeArenaRadius(SimMatchConfig config, float roundElapsedSeconds)
        {
            float shrunkRadius = config.ArenaInitialRadius - config.ArenaShringRatePerSecond * roundElapsedSeconds;
            return Mathf.Max(config.ArenaMinRadius, shrunkRadius);
        }
    }
}