using Drewio.UnityToolkit.Audio;
using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.Scenes;
using Drewio.UnityToolkit.Timers;
using LittleDelay.Presentation;
using LittleDelay.Presentation.LittleDelay.Presentation;
using LittleDelay.Simulation.AI;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using LittleDelay.Simulation.Reveal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleDelay.Composition
{
    /// <summary>
    /// Scene-scoped composition root for the arena scene. A fresh instance exists every time the scene loads, so
    /// nothing match-scoped (the SimMatch, the state machine, the projectile pool) can leak between matches.
    ///
    /// It is the only class that wires simulation to presentation: it builds the pure-C# SimMatch and MatchFlow,
    /// hands the event bus (and, where a view needs one, the tick clock) to every view, and drives the flow from
    /// Update. Views never reference the simulation's owners and the simulation never references a view.
    /// </summary>
    public sealed class MatchCompositionRoot : MonoBehaviour
    {
        [Header("Simulation")]
        [SerializeField] private SimMatchConfig _matchConfig = new();
        [SerializeField] private SimAIConfig _aiConfig = new();

        [Header("Arena Layout (scene markers; their positions and forward vectors become the sim spawns)")]
        [SerializeField] private Transform _arenaCenter;
        [SerializeField] private Transform _playerSpawn;
        [SerializeField] private Transform _opponentSpawn;

        [Header("Input / Camera")]
        [SerializeField] private PlayerSimInputSource _playerInput;
        [SerializeField] private CameraTargetController _cameraTarget;
        [SerializeField] private CameraDirector _cameraDirector;

        [Header("World Views")]
        [SerializeField] private FighterView _playerView;
        [SerializeField] private FighterView _opponentView;
        [SerializeField] private ProjectileViewManager _projectileViews;
        [SerializeField] private ArenaView _arenaView;
        [SerializeField] private RevealVisualizer _revealVisualizer;
        [SerializeField] private SpawnIndicator _spawnIndicator;
        [SerializeField] private ShowdownScreenUI _showdownScreen;

        [Header("UI")]
        [SerializeField] private BattleUIManager _battleHud;
        [SerializeField] private RevealCountdownUI _revealCountdown;
        [SerializeField] private GameOverScreenUI _gameOverScreen;
        [SerializeField] private FighterHealthBarUI[] _healthBars;
        [SerializeField] private AbilitySlotUI[] _abilitySlots;

        [Header("Audio")]
        [SerializeField] private MatchAudioCues _audioCues = new();

        [Header("Scenes")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        public IEventBus EventBus { get; private set; }
        public TimerManager TimerManager { get; private set; }
        public SimMatch Match { get; private set; }
        public MatchFlow Flow { get; private set; }
        public SimTickClock Clock { get; private set; }

        private SceneLoadingService _sceneLoading;
        private AudioPlaybackService _audio;
        private MatchAudioBinder _audioBinder;
        private readonly List<IDisposable> _subs = new();
        private bool _flowStarted;

        private void Awake()
        {
            if (GameCompositionRoot.Instance == null)
            {
                Debug.LogError("[MatchCompositionRoot] No GameCompositionRoot found. Start play mode from the scene that contains it (usually MainMenu), or add one to this scene.");
                enabled = false;
                return;
            }

            var services = GameCompositionRoot.Instance.Services;
            EventBus = services.Resolve<IEventBus>();
            TimerManager = services.Resolve<TimerManager>();
            _sceneLoading = services.Resolve<SceneLoadingService>();
            _audio = services.Resolve<AudioPlaybackService>();
        }

        private void Start()
        {
            Clock = new SimTickClock(_matchConfig.FixedDeltaTime);

            var provider = new RecordingInputProvider(_playerInput, _aiConfig, _matchConfig);
            Match = new SimMatch(
                _matchConfig, EventBus, provider,
                _arenaCenter.position,
                new SimSpawn(_playerSpawn.position, _playerSpawn.forward),
                new SimSpawn(_opponentSpawn.position, _opponentSpawn.forward));

            Flow = new MatchFlow(Match, _matchConfig, EventBus, _cameraDirector, Clock);

            WireViews();

            _audioBinder = new MatchAudioBinder(EventBus, _audio, _audioCues);

            _subs.Add(EventBus.Subscribe<RematchRequestedEvent>(_ => Flow.RequestRematch()));
            _subs.Add(EventBus.Subscribe<QuitToMenuRequestedEvent>(_ => _sceneLoading.LoadScene(_mainMenuSceneName)));
        }

        private void WireViews()
        {
            _playerInput.Initialize(EventBus, GameInput.Instance, _matchConfig);
            _cameraTarget.Initialize(EventBus, GameInput.Instance);

            _playerView.Initialize(EventBus, Clock);
            _opponentView.Initialize(EventBus, Clock);
            _projectileViews.Initialize(EventBus, Clock);
            _arenaView.Initialize(EventBus);
            _revealVisualizer.Initialize(EventBus);
            _spawnIndicator.Initialize(EventBus);
            _showdownScreen.Initialize(EventBus, _matchConfig);

            _battleHud.Initialize(EventBus, Match);
            if (_revealCountdown != null) _revealCountdown.Initialize(EventBus, Match, _matchConfig);
            _gameOverScreen.Initialize(EventBus);
            foreach (FighterHealthBarUI bar in _healthBars) bar.Initialize(EventBus);
            foreach (AbilitySlotUI slot in _abilitySlots) slot.Initialize(EventBus);
        }

        private void Update()
        {
            if (!_flowStarted)
            {
                _flowStarted = true;
                Flow.Start();
            }

            Flow.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
            _audioBinder?.Dispose();
        }

        private sealed class RecordingInputProvider : ISimRecordingInputProvider
        {
            private readonly PlayerSimInputSource _player;
            private readonly SimAIConfig _ai;
            private readonly SimMatchConfig _config;

            public RecordingInputProvider(PlayerSimInputSource player, SimAIConfig ai, SimMatchConfig config)
            {
                _player = player;
                _ai = ai;
                _config = config;
            }

            public ISimInputSource CreateSource(FighterSlot slot, SimWorld world, ISimIntelSource intel)
            {
                return slot == FighterSlot.Player
                    ? _player
                    : new SimAIInputSource(world.Opponent, FighterSlot.Player, intel, _ai, _config);
            }
        }
    }
}
