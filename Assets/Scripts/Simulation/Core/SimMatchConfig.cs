using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    ///<summary>
    /// Every tunable constant SimWorld and its collaborators need, in one place,
    /// instead of magic numbers scattered across SimMovement/SimCombat/SimAbility.
    /// Plain [Serializable] C# class (not a ScriptableObject/MonoBheaviour) so it
    /// stays usable from EditMode tests with no Unity scene required; a later
    /// Presentation-layer ScriptableObject can wrap one of these in a [SerializeField]
    /// to expose it in the Inspector without the Simulation
    /// layer itself depending on the asset pipelin.
    ///</summary>
    ///
    [Serializable]
    public class SimMatchConfig
    {
        [Header("Fighter")]
        public float MaxHealth = 100f;
        public float FighterMoveSpeed = 6f;
        public float FighterHurtboxRadius = 0.5f;

        [Header("Blast Ability")]
        public float BlastAbilityCooldownSeconds = 0.5f;
        public float ProjectileSpeed = 20f;
        public float ProjectileRadius = 0.15f;
        public float ProjectileDamage = 8f;
        public float ProjectileLifeTimeSeconds = 3f;
        public float ProjectileSpawnOffset = 0.6f;
        public float ProjectileArenaExitMargin = 2f;

        [Header("Arena Shring")]
        public float ArenaInitialRadius = 15f;
        public float ArenaMinRadius = 4f;
        public float ArenaShringRatePerSecond = 0.15f;

        [Header("Deflection / Knockback")]
        public float DeflectionKnockbackSpeed = 3f;
        public float DeflectionDurationSeconds = 0.35f;

        [Header("Passivity")]
        public float PassivityCheckIntervalSeconds = 4f;
        public float PassivityMinTravelDistance = 1.5f;
        public float PassivityDamage = 5f;
    }
}