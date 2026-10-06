using LittleDelay.Simulation.Events;

namespace LittleDelay.Simulation.Flow.States
{
    /// <summary>Applies the battle result (RoundEndedEvent), holds a beat so the last hit reads, then either rolls into the next round or ends the match.</summary>
    public sealed class RoundResolveState : MatchStateBase
    {
        private float _remaining;

        public RoundResolveState(MatchFlow flow) : base(flow) { }

        public override void Enter()
        {
            _remaining = Flow.Config.RoundResolveSeconds;
            Flow.Match.CompleteBattle();
        }

        public override void Tick(float deltaTime)
        {
            _remaining -= deltaTime;
            if (_remaining > 0f) return;

            if (Flow.Match.IsOver)
            {
                Flow.Machine.ChangeState(Flow.MatchOver);
                return;
            }

            Flow.Match.BeginRound();
            Flow.Machine.ChangeState(Flow.PlayerCameraTransition);
        }
    }

    public sealed class MatchOverState : MatchStateBase
    {
        public MatchOverState(MatchFlow flow) : base(flow) { }

        public override void Enter()
        {
            Flow.EventBus.Publish(new MatchEndedEvent(Flow.Match.MatchOutcome.Value, Flow.Match.RoundNumber));
        }
    }
}
