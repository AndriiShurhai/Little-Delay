using Drewio.UnityToolkit.Core;

namespace LittleDelay.Simulation.Flow.States
{
    public abstract class MatchStateBase : IState
    {
        protected readonly MatchFlow Flow;

        protected MatchStateBase(MatchFlow flow) => Flow = flow;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }

    /// <summary>Counts real (frame) time down and publishes CountdownTickEvent whenever the displayed whole second changes.</summary>
    public abstract class TimedCountdownStateBase : MatchStateBase
    {
        private float _remaining;
        private int _lastShownSecond;

        protected TimedCountdownStateBase(MatchFlow flow) : base(flow) { }

        protected abstract float DurationSeconds { get; }
        protected abstract void OnCountdownFinished();

        public override void Enter()
        {
            _remaining = DurationSeconds;
            _lastShownSecond = int.MaxValue;
            PublishSecondIfChanged();
        }

        public override void Tick(float deltaTime)
        {
            _remaining -= deltaTime;
            if (_remaining <= 0f)
            {
                OnCountdownFinished();
                return;
            }

            PublishSecondIfChanged();
        }

        private void PublishSecondIfChanged()
        {
            int second = (int)System.Math.Ceiling(_remaining);
            if (second == _lastShownSecond) return;

            _lastShownSecond = second;
            Flow.EventBus.Publish(new Events.CountdownTickEvent(second));
        }
    }

    /// <summary>Asks the camera for a view, gives Cinemachine one frame to start the blend, then waits for it to end.</summary>
    public abstract class CameraTransitionStateBase : MatchStateBase
    {
        private bool _blendHadAFrameToStart;

        protected CameraTransitionStateBase(MatchFlow flow) : base(flow) { }

        protected abstract Events.CameraView View { get; }
        protected abstract void OnTransitionFinished();

        public override void Enter()
        {
            _blendHadAFrameToStart = false;
            Flow.Camera.ShowView(View);
            Flow.EventBus.Publish(new Events.CameraViewRequestedEvent(View));
        }

        public override void Tick(float deltaTime)
        {
            if (!_blendHadAFrameToStart)
            {
                _blendHadAFrameToStart = true;
                return;
            }

            if (!Flow.Camera.IsBlending)
            {
                OnTransitionFinished();
            }
        }
    }
}
