using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// One tick's worth of gameplay input for a single fighter. This is the
    /// contract at the simulation tick boundary - live player input, AI
    /// decisions, and recorded/played-back input all resolve to a
    /// stream of these before reaching SimWorld, so SimWorld itself never know
    /// or cares which of the tree produced a given frame.
    /// 
    /// MoveDirection and AimDirection are kept separate rather than deriving aim
    /// from movement, matching the existing aim-facing/strafe model (a fighter
    /// can strafe wihle aiming at a fixed direction)
    /// </summary>
    public readonly struct SimInputFrame
    {
        /// <summary>XZ-plane direction, magnitude expected in [0, 1]; SimMovement clamps if it isn't</summary>
        public readonly Vector3 MoveDirection;

        /// <summary>XZ-plane facing/aim direction. Zero means "keep the fighter's current aim" - see SimWorld's per-tick handling</summary>
        public readonly Vector3 AimDirection;

        public readonly bool FirePrimary;

        public SimInputFrame(Vector3 moveDirection, Vector3 aimDirection, bool firePrimary)
        {
            MoveDirection = moveDirection;
            AimDirection = aimDirection;
            FirePrimary = firePrimary;
        }

        ///<summary>No movement, no input, no facing change - a fighter that did nothing this tick</summary>
        public static readonly SimInputFrame None = default;
    }
}