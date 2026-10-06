using System;
using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.StateMachines;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Flow.States;

namespace LittleDelay.Simulation.Flow
{
    /// <summary>
    /// Owns the toolkit StateMachine and the eight match states. The transition table is the single place
    /// that says which phase may follow which - an illegal jump throws instead of silently corrupting the match.
    /// The states are plain C# (no MonoBehaviour); a MonoBehaviour only calls Tick(Time.deltaTime).
    /// </summary>
    public sealed class MatchFlow
    {
        public SimMatch Match { get; }
        public SimMatchConfig Config { get; }
        public IEventBus EventBus { get; }
        public ICameraDirector Camera { get; }
        public SimTickClock Clock { get; }
        public StateMachine Machine { get; }

        public ShowdownPreviewState ShowdownPreview { get; }
        public PlayerCameraTransitionState PlayerCameraTransition { get; }
        public CountdownState Countdown { get; }
        public RecordingState Recording { get; }
        public ArenaCameraTransitionState ArenaCameraTransition { get; }
        public PreBattleCountdownState PreBattleCountdown { get; }
        public BattlePhaseState BattlePhase { get; }
        public RoundResolveState RoundResolve { get; }
        public MatchOverState MatchOver { get; }

        public MatchFlow(SimMatch match, SimMatchConfig config, IEventBus eventBus, ICameraDirector camera, SimTickClock clock)
        {
            Match = match ?? throw new ArgumentNullException(nameof(match));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Camera = camera ?? new NullCameraDirector();
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            ShowdownPreview = new ShowdownPreviewState(this);
            PlayerCameraTransition = new PlayerCameraTransitionState(this);
            Countdown = new CountdownState(this);
            Recording = new RecordingState(this);
            ArenaCameraTransition = new ArenaCameraTransitionState(this);
            PreBattleCountdown = new PreBattleCountdownState(this);
            BattlePhase = new BattlePhaseState(this);
            RoundResolve = new RoundResolveState(this);
            MatchOver = new MatchOverState(this);

            Machine = new StateMachine(new TransitionTableBuilder()
                .Allow<ShowdownPreviewState, PlayerCameraTransitionState>()
                .Allow<PlayerCameraTransitionState, CountdownState>()
                .Allow<CountdownState, RecordingState>()
                .Allow<RecordingState, ArenaCameraTransitionState>()
                .Allow<ArenaCameraTransitionState, PreBattleCountdownState>()
                .Allow<PreBattleCountdownState, BattlePhaseState>()
                .Allow<BattlePhaseState, RoundResolveState>()
                .Allow<RoundResolveState, PlayerCameraTransitionState>() // no kill: next round
                .Allow<RoundResolveState, MatchOverState>()
                .Allow<MatchOverState, ShowdownPreviewState>()           // rematch
                .Build());
        }

        public void Start() => Machine.ChangeState(ShowdownPreview);

        public void Tick(float deltaTime) => Machine.Tick(deltaTime);

        /// <summary>Only meaningful on the result screen; ignored in any other state.</summary>
        public void RequestRematch()
        {
            if (Machine.CurrentState == MatchOver)
            {
                Machine.ChangeState(ShowdownPreview);
            }
        }
    }
}
