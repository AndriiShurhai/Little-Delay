using LittleDelay.Simulation.Events;

namespace LittleDelay.Simulation.Flow.States
{
    /// <summary>Pre-recording countdown. The opponent's spawn point is revealed for its duration.</summary>
    public sealed class CountdownState : TimedCountdownStateBase
    {
        public CountdownState(MatchFlow flow) : base(flow) { }

        protected override float DurationSeconds => Flow.Config.PreRecordingCountdownSeconds;

        public override void Enter()
        {
            float duration = DurationSeconds;
            Flow.EventBus.Publish(new PreRecordingCountdownStartedEvent(duration));
            Flow.EventBus.Publish(new SpawnRevealedEvent(FighterSlot.Opponent, Flow.Match.CurrentWorld.Opponent.Position, duration));
            base.Enter();
        }

        protected override void OnCountdownFinished() => Flow.Machine.ChangeState(Flow.Recording);
    }

    public sealed class PreBattleCountdownState : TimedCountdownStateBase
    {
        public PreBattleCountdownState(MatchFlow flow) : base(flow) { }

        protected override float DurationSeconds => Flow.Config.PreBattleCountdownSeconds;

        public override void Enter()
        {
            Flow.EventBus.Publish(new RoundCountdownStartedEvent(Flow.Match.RoundNumber, DurationSeconds));
            base.Enter();
        }

        protected override void OnCountdownFinished() => Flow.Machine.ChangeState(Flow.BattlePhase);
    }
}
