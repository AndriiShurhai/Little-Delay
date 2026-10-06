using LittleDelay.Simulation.Events;
using UnityEngine;

namespace LittleDelay.Simulation.Combat
{
    /// <summary>
    /// Engine-agnostic projectile state. No Rigidbody, no OnTriggerEnter - SimCombat moves and
    /// collision-tests these directly. Views read Position/PreviousPosition to place a pooled
    /// visual; they never mutate this.
    /// </summary>
    public sealed class SimProjectile
    {
        public int Id { get; }
        public FighterSlot Owner { get; }

        public Vector3 Position { get; internal set; }
        public Vector3 PreviousPosition { get; internal set; }
        public Vector3 Velocity { get; internal set; }
        public float ElapsedSeconds { get; internal set; }
        public bool IsExpired { get; internal set; }

        public SimProjectile(int id, FighterSlot owner, Vector3 position, Vector3 velocity)
        {
            Id = id;
            Owner = owner;
            Position = position;
            PreviousPosition = position;
            Velocity = velocity;
        }
    }
}
