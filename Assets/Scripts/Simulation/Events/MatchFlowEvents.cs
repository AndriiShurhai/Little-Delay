using Drewio.UnityToolkit.Core;

namespace LittleDelay.Simulation.Events
{
    /// <summary>
    /// Identifies which side of a 1v1 match a fighter-scoped event refers to.
    /// The prototype is strictly two-fighter (player + AI, or player + player
    /// once online multiplayer lands), so a named enum reads better at every call
    /// site than a raw index and avoids off-by-one mixups between the two slots.
    /// If/when the roster grows beyond two, this becomes a FighterID value type
    /// instead - not a concern for Phase 1.
    /// </summary>
    
    public enum FighterSlot
    {
        Player = 0,
        Opponent = 1
    }

    public enum RoundOutcome
    {
        PlayerWin,
        OpponentWin,
        Draw
    }

    /// <summary> Raised once when a new match begins, before ShowdownPreview. </summary>
    public readonly struct MatchStartedEvent : IGameEvent
    {
        public readonly int RoundsToWin;
        public MatchStartedEvent(int roundsToWin) => RoundsToWin = roundsToWin;
    }

    /// <summary>
    /// Pre-recording countdown, during which the player's own spawn point
    /// pulsates (see SpawnPointPulseStartedEvent in RevealEvents.cs) - future
    /// feature, once spawn-point choice on a minimap exists.
    /// </summary>
    public readonly struct PreRecordingCountdownStartedEvent : IGameEvent
    {
        public readonly float Duration;
        public PreRecordingCountdownStartedEvent(float duration) => Duration = duration;
    }

    ///<summary>Recording phase started for both fighters simultaneously. Duration is the full 20s window</summary>
    public readonly struct RecordingStartedEvent : IGameEvent
    {
        public readonly float Duration;
        public RecordingStartedEvent(float duration) => Duration = duration;
    }

    public readonly struct RecordingCompletedEvent : IGameEvent
    {
    }

    /// <summary>Camera swaps form the third-person view to the arena view. Cinemachine handles the blend.</summary>
    public readonly struct ArenaCameraTrnasitionStartedEvent : IGameEvent
    {
    }

    public readonly struct RoundCountdownStartedEvent : IGameEvent
    {
        public readonly int RoundNumber;
        public readonly float Duration;

        public RoundCountdownStartedEvent(int roundNumber, float duration)
        {
            RoundNumber = roundNumber;
            Duration = duration;
        }
    }

    ///<summary>Battle playback(both recordings replaying simultaneously) has started for this round</summary>
    public readonly struct BattleStartedEvent : IGameEvent
    {
        public readonly int RoundNumber;
        public BattleStartedEvent(int roundNumber) => RoundNumber = roundNumber;
    }

    ///<summary>One round resolved. Simultaneous death (both fighters die on the same tick) resolves as Draw.</summary>
    public readonly struct RoundEndedEvent : IGameEvent
    {
        public readonly int RoundNumber;
        public readonly RoundOutcome Outcome;
        public RoundEndedEvent(int roundNumber, RoundOutcome outcome)
        {
            RoundNumber = roundNumber;
            Outcome = outcome;
        }
    }

    ///<summary>Whoever wins a round wins the match outright</summary>
    public readonly struct MatchEndedEvent : IGameEvent
    {
        public readonly RoundOutcome Outcome;
        public readonly int RoundNumber;
        public MatchEndedEvent(RoundOutcome outcome, int roundNumber)
        {
            Outcome = outcome;
            RoundNumber = roundNumber;
        }
    }
}
