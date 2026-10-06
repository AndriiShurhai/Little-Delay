using Drewio.UnityToolkit.Core;

namespace LittleDelay.Simulation.Events
{
    /// <summary>Which side of the 1v1 a fighter-scoped event refers to.</summary>
    public enum FighterSlot
    {
        Player = 0,
        Opponent = 1
    }

    public enum RoundOutcome
    {
        PlayerWin,
        OpponentWin,
        Draw,
        /// <summary>Both recordings ran out and nobody died. Not a result by itself - SimMatch decides whether another round follows.</summary>
        NoKill
    }

    /// <summary>Raised once when a new match begins (after the showdown preview).</summary>
    public readonly struct MatchStartedEvent : IGameEvent
    {
        public readonly int MaxRounds;
        public MatchStartedEvent(int maxRounds) => MaxRounds = maxRounds;
    }

    public readonly struct ShowdownPreviewStartedEvent : IGameEvent
    {
        public readonly float Duration;
        public ShowdownPreviewStartedEvent(float duration) => Duration = duration;
    }

    /// <summary>A new round begins (its recording world has just been created).</summary>
    public readonly struct RoundStartedEvent : IGameEvent
    {
        public readonly int RoundNumber;
        public readonly int MaxRounds;

        public RoundStartedEvent(int roundNumber, int maxRounds)
        {
            RoundNumber = roundNumber;
            MaxRounds = maxRounds;
        }
    }

    /// <summary>Pre-recording countdown, during which the enemy's spawn is revealed (and, later, the player's own spawn pulses).</summary>
    public readonly struct PreRecordingCountdownStartedEvent : IGameEvent
    {
        public readonly float Duration;
        public PreRecordingCountdownStartedEvent(float duration) => Duration = duration;
    }

    /// <summary>Whole-second tick shared by both countdowns (3, 2, 1). Discrete, so event-driven; continuous timers are pulled instead.</summary>
    public readonly struct CountdownTickEvent : IGameEvent
    {
        public readonly int SecondsRemaining;
        public CountdownTickEvent(int secondsRemaining) => SecondsRemaining = secondsRemaining;
    }

    public readonly struct RecordingStartedEvent : IGameEvent
    {
        public readonly float Duration;
        public RecordingStartedEvent(float duration) => Duration = duration;
    }

    public readonly struct RecordingCompletedEvent : IGameEvent
    {
    }

    public enum CameraView
    {
        Player,
        Arena
    }

    /// <summary>The flow asks for a camera swap; Cinemachine handles the blend, the flow waits for it to finish.</summary>
    public readonly struct CameraViewRequestedEvent : IGameEvent
    {
        public readonly CameraView View;
        public CameraViewRequestedEvent(CameraView view) => View = view;
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

    /// <summary>Battle playback (both recordings replaying simultaneously) has started for this round.</summary>
    public readonly struct BattleStartedEvent : IGameEvent
    {
        public readonly int RoundNumber;
        public BattleStartedEvent(int roundNumber) => RoundNumber = roundNumber;
    }

    /// <summary>One battle resolved. Simultaneous death on the same tick is a Draw; NoKill means the recordings ran out.</summary>
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

    /// <summary>Whoever kills the opponent in any round wins the match outright; Draw covers simultaneous death and "no kill after the last round".</summary>
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
