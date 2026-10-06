using LittleDelay.Simulation.Events;

namespace LittleDelay.Simulation.Flow.States
{
    /// <summary>Both fighters act live in the rehearsal world while their inputs are captured, one fixed tick at a time.</summary>
    public sealed class RecordingState : MatchStateBase
    {
        private SimRoundRunner _runner;

        public RecordingState(MatchFlow flow) : base(flow) { }

        public override void Enter()
        {
            _runner = Flow.Match.CurrentRunner;
            Flow.Clock.Freeze();
            Flow.EventBus.Publish(new RecordingStartedEvent(Flow.Config.RecordingDurationSeconds));
        }

        public override void Tick(float deltaTime)
        {
            Flow.Clock.Advance(deltaTime, _runner.Step);
            if (!_runner.IsFinished) return;

            Flow.Match.CompleteRecording();
            Flow.Machine.ChangeState(Flow.ArenaCameraTransition);
        }

        public override void Exit() => Flow.Clock.Freeze();
    }

    /// <summary>Both recordings replay simultaneously in a fresh battle world until someone dies or they run out.</summary>
    public sealed class BattlePhaseState : MatchStateBase
    {
        private SimRoundRunner _runner;

        public BattlePhaseState(MatchFlow flow) : base(flow) { }

        public override void Enter()
        {
            _runner = Flow.Match.CurrentRunner;
            Flow.Clock.Freeze();
            Flow.EventBus.Publish(new BattleStartedEvent(Flow.Match.RoundNumber));
        }

        public override void Tick(float deltaTime)
        {
            Flow.Clock.Advance(deltaTime, _runner.Step);
            if (_runner.IsFinished)
            {
                Flow.Machine.ChangeState(Flow.RoundResolve);
            }
        }

        public override void Exit() => Flow.Clock.Freeze();
    }
}
