using LittleDelay.Simulation.Events;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LittleDelay.Simulation.Combat
{
    ///<summary>
    /// Engine-agnostic projectile state. No Rigidbody, no OnTriggerEnter -
    /// SimCombat moves and collision-tests these directly against plain 
    /// distance checks. ProjectileView reads Position/Velocity to
    /// place its pooled visual; it never mutates this.
    ///</summary>
    public sealed class SimProjectile
    {
        public int Id { get; }
        public FighterSlot Owner { get; }

        public Vector3 Position { get; internal set; }
        public Vector3 Velocity { get; internal set; }
        public float ElapsedSeconds { get; internal set; }
        public bool IsExpired { get; internal set; }

        public SimProjectile(int id, FighterSlot owner, Vector3 position, Vector3 velocity)
        {
            Id = id;
            Owner = owner;
            Position = position;
            Velocity = velocity;
        }
    }
}