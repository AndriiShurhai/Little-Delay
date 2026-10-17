using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// One tick's worth of gameplay input for a single fighter. Live player input,
    /// AI decisions and recorded playback all resolve to a stream of these before
    /// reaching SimWorld, so SimWorld never knows (or cares) which one produced a frame.
    /// MoveDirection and AimDirection are separate (aim-facing/strafe model).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct SimInputFrame : IEquatable<SimInputFrame>
    {
        public const byte ButtonFirePrimary = 1 << 0;
        public const byte ButtonAbility1 = 1 << 1;
        public const byte ButtonAbility2 = 1 << 2;
        public const byte ButtonJump = 1 << 3;

        public readonly sbyte MoveX;
        public readonly sbyte MoveZ;
        public readonly short AimX;
        public readonly short AimY;
        public readonly short AimZ;
        public readonly byte Buttons;

        /// <summary>XZ-plane direction decoded from quantized sbytes.</summary>
        public Vector3 MoveDirection => SimInputQuantizer.DecodeMove(MoveX, MoveZ);

        /// <summary>3D aim direction decoded from quantized shorts. Zero means "keep current aim".</summary>
        public Vector3 AimDirection => SimInputQuantizer.DecodeAim(AimX, AimY, AimZ);

        public bool FirePrimary => (Buttons & ButtonFirePrimary) != 0;
        public bool UseAbility1 => (Buttons & ButtonAbility1) != 0;
        public bool UseAbility2 => (Buttons & ButtonAbility2) != 0;

        /// <summary>Alias for UseAbility1 so existing single-mobility code continues to read cleanly.</summary>
        public bool UseMobility => UseAbility1;

        public bool Jump => (Buttons & ButtonJump) != 0;

        public SimInputFrame(
            Vector3 moveDirection,
            Vector3 aimDirection,
            bool firePrimary,
            bool useAbility1 = false,
            bool jump = false,
            bool useAbility2 = false)
        {
            if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();
            MoveX = SimInputQuantizer.EncodeAxis8(moveDirection.x);
            MoveZ = SimInputQuantizer.EncodeAxis8(moveDirection.z);

            if (aimDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 normAim = aimDirection.normalized;
                AimX = SimInputQuantizer.EncodeAxis16(normAim.x);
                AimY = SimInputQuantizer.EncodeAxis16(normAim.y);
                AimZ = SimInputQuantizer.EncodeAxis16(normAim.z);
            }
            else
            {
                AimX = 0;
                AimY = 0;
                AimZ = 0;
            }

            byte buttons = 0;
            if (firePrimary) buttons |= ButtonFirePrimary;
            if (useAbility1) buttons |= ButtonAbility1;
            if (useAbility2) buttons |= ButtonAbility2;
            if (jump) buttons |= ButtonJump;
            Buttons = buttons;
        }

        public SimInputFrame(sbyte moveX, sbyte moveZ, short aimX, short aimY, short aimZ, byte buttons)
        {
            MoveX = moveX;
            MoveZ = moveZ;
            AimX = aimX;
            AimY = aimY;
            AimZ = aimZ;
            Buttons = buttons;
        }

        public static readonly SimInputFrame None = default;

        public bool Equals(SimInputFrame other) =>
            MoveX == other.MoveX &&
            MoveZ == other.MoveZ &&
            AimX == other.AimX &&
            AimY == other.AimY &&
            AimZ == other.AimZ &&
            Buttons == other.Buttons;

        public override bool Equals(object obj) => obj is SimInputFrame other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(MoveX, MoveZ, AimX, AimY, AimZ, Buttons);
    }
}