using Drewio.UnityToolkit.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace LittleDelay.Simulation.Events
{
    /// <summary>
    /// A reveal window opened for one fighter's recording: 3 per 20s recording,
    /// starting at recording start and repeating every 7s, each lasting 2s
    /// (GDD v0.1). RevealIndex is 0-based (0, 1, 2).
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
    /// Published once per tick while a reveal window is active, carrying the
    /// revealed fighter's real-time recorded position. The reveal format is a
    /// thick highlighted trail of actual movement (not a zone circle), so the
    /// view accumulates these points into a trail renderer rather than sampling
    /// simulation state directly - keeps the reveal visual entirely
    /// event-driven, consistent with the rest of the presentation layer.
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

    /// <summary>
    /// Future feature (GDD v0.1: not yet implemented) - the player's own spawn
    /// point pulsates during the pre-recording countdown once spawn-point choice
    /// on a minimap exists. Only ever raised for FighterSlot.Player, never the
    /// opponent's spawn.
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