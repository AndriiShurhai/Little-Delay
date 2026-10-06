using Drewio.UnityToolkit.Core;
using UnityEngine;

namespace LittleDelay.Simulation.Events
{
    /// <summary>
    /// A reveal window opened: 3 per recording, starting at recording start and repeating
    /// every 7s, each lasting 2s (GDD v0.1). RevealIndex is 0-based. One event is published
    /// per fighter being revealed - the HUD shows the opponent's, the AI is told the player's.
    /// </summary>
    public readonly struct RevealWindowStartedEvent : IGameEvent
    {
        public readonly FighterSlot RevealedFighter;
        public readonly int RevealIndex;
        public readonly float Duration;

        public RevealWindowStartedEvent(FighterSlot revealedFighter, int revealIndex, float duration)
        {
            RevealedFighter = revealedFighter;
            RevealIndex = revealIndex;
            Duration = duration;
        }
    }

    public readonly struct RevealWindowEndedEvent : IGameEvent
    {
        public readonly FighterSlot RevealedFighter;
        public readonly int RevealIndex;

        public RevealWindowEndedEvent(FighterSlot revealedFighter, int revealIndex)
        {
            RevealedFighter = revealedFighter;
            RevealIndex = revealIndex;
        }
    }

    /// <summary>
    /// Published once per tick while a reveal window is active, carrying the revealed
    /// fighter's real-time position. The view accumulates these into a thick trail.
    /// </summary>
    public readonly struct RevealTrailPointEvent : IGameEvent
    {
        public readonly FighterSlot RevealedFighter;
        public readonly int RevealIndex;
        public readonly Vector3 Position;

        public RevealTrailPointEvent(FighterSlot revealedFighter, int revealIndex, Vector3 position)
        {
            RevealedFighter = revealedFighter;
            RevealIndex = revealIndex;
            Position = position;
        }
    }

    /// <summary>The enemy's spawn point is shown for the duration of the pre-recording countdown.</summary>
    public readonly struct SpawnRevealedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;
        public readonly Vector3 Position;
        public readonly float Duration;

        public SpawnRevealedEvent(FighterSlot fighter, Vector3 position, float duration)
        {
            Fighter = fighter;
            Position = position;
            Duration = duration;
        }
    }

    /// <summary>
    /// Future feature (GDD v0.1: not implemented) - the player's own spawn point pulsates during
    /// the pre-recording countdown once spawn-point choice on a minimap exists. Only ever for FighterSlot.Player.
    /// </summary>
    public readonly struct SpawnPointPulseStartedEvent : IGameEvent
    {
        public readonly FighterSlot Fighter;

        public SpawnPointPulseStartedEvent(FighterSlot fighter)
        {
            Fighter = fighter;
        }
    }
}
