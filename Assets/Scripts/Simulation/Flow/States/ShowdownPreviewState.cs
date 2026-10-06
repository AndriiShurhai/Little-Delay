using LittleDelay.Simulation.Events;

namespace LittleDelay.Simulation.Flow.States
{
    public sealed class ShowdownPreviewState : MatchStateBase
    {
        private float _remaining;

        public ShowdownPreviewState(MatchFlow flow) : base(flow) { }

        public override void Enter()
        {
            _remaining = Flow.Config.ShowdownPreviewSeconds;
            Flow.EventBus.Publish(new ShowdownPreviewStartedEvent(_remaining));
        }

        public override void Tick(float deltaTime)
        {
            _remaining -= deltaTime;
            if (_remaining > 0f) return;

            Flow.Match.StartMatch();
            Flow.Match.BeginRound();
            Flow.Machine.ChangeState(Flow.PlayerCameraTransition);
        }
    }
}
