using LittleDelay.Simulation.Core;
using UnityEngine;

namespace LittleDelay.Simulation.Fighters
{
    /// <summary>
    /// Pure, stateless kinematic movement math. Deliberately avoids Rigidbody/PhysX (not guaranteed
    /// reproducible): velocity from input, position += velocity * dt, clamped to the arena radius.
    /// The old inward-moving wall colliders become a clamp against a radius that is a pure function
    /// of round number and elapsed battle time.
    /// </summary>
    public static class SimMovement
    {
        public static Vector3 ClampMoveInput(Vector3 moveDirection)
        {
            moveDirection.y = 0f;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            return moveDirection;
        }

        public static Vector3 ComputeVelocity(Vector3 clampedMoveDirection, SimMatchConfig config)
        {
            return clampedMoveDirection * config.FighterMoveSpeed;
        }

        public static Vector3 Integrate(Vector3 currentPosition, Vector3 velocity, float deltaTime)
        {
            return currentPosition + velocity * deltaTime;
        }

        /// <summary>Pushes a position back onto the arena boundary along the direction it exceeded it from.</summary>
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

        /// <summary>Radius at the start of a round: shrinks a fixed amount per round, never below the minimum.</summary>
        public static float ComputeRoundStartRadius(SimMatchConfig config, int roundNumber)
        {
            float radius = config.ArenaInitialRadius - config.ArenaShrinkPerRound * Mathf.Max(0, roundNumber - 1);
            return Mathf.Max(config.ArenaMinRadius, radius);
        }

        /// <summary>Radius during a battle: round-start radius minus continuous shrink over elapsed battle time.</summary>
        public static float ComputeBattleRadius(SimMatchConfig config, int roundNumber, float battleElapsedSeconds)
        {
            float radius = ComputeRoundStartRadius(config, roundNumber) - config.ArenaShrinkRatePerSecond * battleElapsedSeconds;
            return Mathf.Max(config.ArenaMinRadius, radius);
        }

        /// <summary>Reduces a vector's length by amount (never past zero) without changing its direction.</summary>
        public static Vector3 DecayToZero(Vector3 velocity, float amount)
        {
            float magnitude = velocity.magnitude;
            if (magnitude <= amount || magnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return velocity * ((magnitude - amount) / magnitude);
        }
    }
}
