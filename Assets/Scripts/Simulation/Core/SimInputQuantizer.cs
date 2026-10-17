using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// Quantizes input at capture time so recordings and network streams store compact integers
    /// (9 bytes per tick) and every simulation run consumes identical bit-level values.
    /// Aim is 3D (pitch + yaw) and normalized with a square root (no trig functions).
    /// </summary>
    public static class SimInputQuantizer
    {
        public static sbyte EncodeAxis8(float v) =>
            (sbyte)Mathf.Clamp(Mathf.RoundToInt(v * 127f), -127, 127);

        public static short EncodeAxis16(float v) =>
            (short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32767, 32767);

        public static Vector3 DecodeMove(sbyte x, sbyte z) =>
            new Vector3(x / 127f, 0f, z / 127f);

        public static Vector3 DecodeAim(short x, short y, short z)
        {
            var v = new Vector3(x, y, z);
            return v.sqrMagnitude > 0f ? v.normalized : Vector3.zero; // zero = keep current aim
        }
    }
}