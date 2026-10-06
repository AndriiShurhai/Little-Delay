using LittleDelay.Simulation.Events;

namespace LittleDelay.Simulation.Flow.States
{
    /// <summary>Blend to the player's camera at the start of a round, then the pre-recording countdown.</summary>
    public sealed class PlayerCameraTransitionState : CameraTransitionStateBase
    {
        public PlayerCameraTransitionState(MatchFlow flow) : base(flow) { }

        protected override CameraView View => CameraView.Player;

        protected override void OnTransitionFinished() => Flow.Machine.ChangeState(Flow.Countdown);
    }

    /// <summary>
    /// Blend out to the arena camera. The battle world is created on entry so the fighters are already
    /// standing at their spawns when the camera arrives.
    /// </summary>
    public sealed class ArenaCameraTransitionState : CameraTransitionStateBase
    {
        public ArenaCameraTransitionState(MatchFlow flow) : base(flow) { }

        protected override CameraView View => CameraView.Arena;

        public override void Enter()
        {
            Flow.Match.BeginBattle();
            base.Enter();
        }

        protected override void OnTransitionFinished() => Flow.Machine.ChangeState(Flow.PreBattleCountdown);
    }
}
