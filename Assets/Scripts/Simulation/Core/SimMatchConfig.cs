using System;
using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// Every tunable constant the simulation needs, in one place. Plain [Serializable]
    /// class (not a ScriptableObject) so it stays usable from EditMode tests; a
    /// Presentation-side ScriptableObject can wrap one in a [SerializeField].
    /// </summary>
    [Serializable]
    public class SimMatchConfig
    {
        [Header("Simulation")]
        [Tooltip("Fixed simulation rate. Changing this changes every per-tick quantity, including recordings made at the old rate.")]
        public int TicksPerSecond = 50;

        [Header("Match")]
        public int MaxRounds = 3;
        [Tooltip("GDD v0.1 decided 20s.")]
        public float RecordingDurationSeconds = 20f;
        [Tooltip("Health left at the end of a round that ends with no kill carries into the next round.")]
        public bool CarryHealthBetweenRounds = true;

        [Header("Phase Timing (seconds)")]
        public float ShowdownPreviewSeconds = 3f;
        public float PreRecordingCountdownSeconds = 3f;
        public float PreBattleCountdownSeconds = 3f;
        [Tooltip("Pause on the result of a round before the next round / game-over screen.")]
        public float RoundResolveSeconds = 1.5f;

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

        [Header("Dash Ability")]
        public float DashCooldownSeconds = 2f;
        [Tooltip("Initial dash speed; decays linearly. Travel distance is roughly speed^2 / (2 * friction).")]
        public float DashSpeed = 18f;
        public float DashFrictionPerSecond = 60f;

        [Header("Arena")]
        public float ArenaInitialRadius = 15f;
        public float ArenaMinRadius = 4f;
        [Tooltip("Radius lost at the start of each round after the first (replaces the old between-rounds wall move).")]
        public float ArenaShrinkPerRound = 2f;
        [Tooltip("Extra shrink while a battle is running. Set both shrink values to taste.")]
        public float ArenaShrinkRatePerSecond = 0.15f;

        [Header("Deflection / Knockback")]
        public float DeflectionKnockbackSpeed = 3f;
        public float DeflectionDurationSeconds = 0.35f;

        [Header("Passivity")]
        public float PassivityCheckIntervalSeconds = 4f;
        public float PassivityMinTravelDistance = 1.5f;
        public float PassivityDamage = 5f;

        [Header("Reveals")]
        public int RevealsPerRecording = 3;
        public float RevealIntervalSeconds = 7f;
        public float RevealDurationSeconds = 2f;

        public float FixedDeltaTime => 1f / TicksPerSecond;
        public int RecordingTicks => Mathf.RoundToInt(RecordingDurationSeconds * TicksPerSecond);
    }
}
