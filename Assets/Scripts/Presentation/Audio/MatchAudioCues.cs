using System;
using Drewio.UnityToolkit.Audio;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Which registered sound plays for which gameplay moment. Names refer to entries in the SFX/music
    /// banks on GameCompositionRoot; an empty group simply plays nothing, so cues can be filled in over time.
    /// </summary>
    [Serializable]
    public class MatchAudioCues
    {
        [Header("Abilities")]
        public SoundGroup BlastFired;
        public SoundGroup DashUsed;
        public SoundGroup AbilityOnCooldown;

        [Header("Combat")]
        public SoundGroup ProjectileDeflected;
        public SoundGroup FighterHit;
        public SoundGroup PassivityWarning;
        public SoundGroup FighterDied;

        [Header("Match Flow")]
        public SoundGroup CountdownTick;
        public SoundGroup RecordingStarted;
        public SoundGroup RecordingSaved;
        public SoundGroup BattleStarted;
        public SoundGroup MatchWon;
        public SoundGroup MatchDrawn;

        [Header("Music (registered music name, empty = none)")]
        public string MatchMusic;
    }
}
