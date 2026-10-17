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

        /// <summary>
        /// One fixed step of vertical motion (semi-implicit Euler: velocity first, then position). A fighter that is
        /// on the ground and not moving up is returned untouched, so jump-free play is bit-for-bit what it was before
        /// jumping existed. Landing snaps exactly to groundHeight and zeroes the velocity. gravity must be positive
        /// (SimWorld never starts a jump otherwise), or a rising fighter would never come down.
        /// </summary>
        public static void IntegrateVertical(
            float height, float verticalVelocity, float groundHeight, float gravity, float deltaTime,
            out float newHeight, out float newVerticalVelocity)
        {
            if (height <= groundHeight && verticalVelocity <= 0f)
            {
                newHeight = height;
                newVerticalVelocity = 0f;
                return;
            }

            newVerticalVelocity = verticalVelocity - gravity * deltaTime;
            newHeight = height + newVerticalVelocity * deltaTime;

            if (newHeight <= groundHeight)
            {
                newHeight = groundHeight;
                newVerticalVelocity = 0f;
            }
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

        /// <summary>Radius during a battle of a round that began at the nominal (first-round formula) radius.</summary>
        public static float ComputeBattleRadius(SimMatchConfig config, int roundNumber, float battleElapsedSeconds)
        {
            return ComputeShrunkRadius(config, ComputeRoundStartRadius(config, roundNumber), battleElapsedSeconds);
        }

        /// <summary>Radius during a battle: the radius the round started at minus continuous shrink over elapsed battle time.</summary>
        public static float ComputeShrunkRadius(SimMatchConfig config, float startRadius, float battleElapsedSeconds)
        {
            float radius = startRadius - config.ArenaShrinkRatePerSecond * battleElapsedSeconds;
            return Mathf.Max(config.ArenaMinRadius, radius);
        }

        /// <summary>
        /// Cumulative shrink: a round starts ArenaShrinkPerRound below where the previous battle ended, so the arena
        /// never regrows between rounds and eventually reaches ArenaMinRadius.
        /// </summary>
        public static float ComputeNextRoundStartRadius(SimMatchConfig config, float previousRoundEndRadius)
        {
            return Mathf.Max(config.ArenaMinRadius, previousRoundEndRadius - config.ArenaShrinkPerRound);
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
