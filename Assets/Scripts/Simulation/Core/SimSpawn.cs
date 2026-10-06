using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>Where a fighter starts a round and which way it faces.</summary>
    public readonly struct SimSpawn
    {
        public readonly Vector3 Position;
        public readonly Vector3 AimDirection;

        public SimSpawn(Vector3 position, Vector3 aimDirection)
        {
            Position = position;
            AimDirection = aimDirection;
        }
    }
}
