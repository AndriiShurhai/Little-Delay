using System.Collections.Generic;
using Drewio.UnityToolkit.Audio;
using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.EventBus;
using Drewio.UnityToolkit.Persistence;
using Drewio.UnityToolkit.Scenes;
using Drewio.UnityToolkit.Services;
using Drewio.UnityToolkit.Timers;
using LittleDelay.Presentation;
using UnityEngine;
using UnityEngine.Audio;

namespace LittleDelay.Composition
{
    /// <summary>
    /// Game-lifetime composition root. Lives on a GameObject in the very first scene loaded, survives scene loads
    /// via DontDestroyOnLoad, and constructs each root-level toolkit service exactly once per process.
    /// This is the only place allowed to create EventBus, TimerManager, SceneLoadingService, AudioPlaybackService
    /// and SaveSystem. Scene-scoped roots resolve from <see cref="Services"/> once in their own Awake/Start and
    /// pass dependencies down through constructors / Initialize calls.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // the very first MonoBehaviour to Awake in the process
    public class GameCompositionRoot : MonoBehaviour
    {
        [Header("Audio Banks")]
        [Tooltip("Every music track referenced by name from anywhere in the project.")]
        [SerializeField] private List<SoundDefinition> _musicDefinitions = new();

        // Field name kept as-is (typo included) so existing serialized data in your scene is not lost.
        [Tooltip("Every SFX referenced by name from anywhere in the project.")]
        [SerializeField] private List<SoundDefinition> _sfxDefinitions = new();

        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;

        [Header("Scene Transitions")]
        [Tooltip("Optional. Fades in/out on SceneLoadStartedEvent / SceneLoadCompletedEvent.")]
        [SerializeField] private ScreenFadeController _screenFade;

        public static GameCompositionRoot Instance { get; private set; }

        /// <summary>Root-level service registry. Nothing else should construct a second ServiceLocator.</summary>
        public ServiceLocator Services { get; private set; }

        private SceneLoadingService _sceneLoadingService;
        private TimerManager _timerManager;
        private AudioPlaybackService _audioService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildServices();
        }

        private void BuildServices()
        {
            Services = new ServiceLocator();

            var eventBus = new EventBus();
            Services.Register<IEventBus>(eventBus);

            _timerManager = new TimerManager();
            Services.Register(_timerManager);

            _sceneLoadingService = new SceneLoadingService(eventBus);
            Services.Register(_sceneLoadingService);

            var musicBank = new SoundBank(_musicDefinitions);
            var sfxBank = new SoundBank(_sfxDefinitions);
            _audioService = new AudioPlaybackService(
                eventBus,
                musicBank,
                sfxBank,
                sourceContainer: transform,
                musicMixerGroup: _musicMixerGroup,
                sfxMixerGroup: _sfxMixerGroup);
            Services.Register(_audioService);

            Services.Register(new SaveSystem());

            if (_screenFade != null) _screenFade.Initialize(eventBus);
        }

        private void Update()
        {
            // Drives load-progress events; the completion callback fires on its own.
            _sceneLoadingService.Tick();
            _timerManager.Tick(Time.deltaTime);
            _audioService.Tick(Time.unscaledDeltaTime); // music fades must not freeze under the debug slow-motion toggle
        }
    }
}
