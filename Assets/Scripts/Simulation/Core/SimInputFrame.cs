using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// One tick's worth of gameplay input for a single fighter. Live player input,
    /// AI decisions and recorded playback all resolve to a stream of these before
    /// reaching SimWorld, so SimWorld never knows (or cares) which one produced a frame.
    /// MoveDirection and AimDirection are separate (aim-facing/strafe model).
    /// </summary>
    public readonly struct SimInputFrame
    {
        /// <summary>XZ-plane direction, magnitude expected in [0, 1]; SimMovement clamps if it isn't.</summary>
        public readonly Vector3 MoveDirection;

        /// <summary>XZ-plane facing/aim direction. Zero means "keep the fighter's current aim".</summary>
        public readonly Vector3 AimDirection;

        /// <summary>Level-triggered ("fire is wanted this tick"), not edge-triggered - a held button is true every tick.</summary>
        public readonly bool FirePrimary;

        /// <summary>Level-triggered request for the mobility ability (dash).</summary>
        public readonly bool UseMobility;

        /// <summary>
        /// Level-triggered request to jump. Only takes effect while the fighter is on the ground, so a jump pressed
        /// mid-air is ignored (no buffering, no double jump), and a stream that holds it hops again on every landing.
        /// </summary>
        public readonly bool Jump;

        public SimInputFrame(Vector3 moveDirection, Vector3 aimDirection, bool firePrimary, bool useMobility = false, bool jump = false)
        {
            MoveDirection = moveDirection;
            AimDirection = aimDirection;
            FirePrimary = firePrimary;
            UseMobility = useMobility;
            Jump = jump;
        }

        /// <summary>No movement, no input, no facing change - a fighter that did nothing this tick.</summary>
        public static readonly SimInputFrame None = default;
    }
}