using UnityEngine;
using Drewio.UnityToolkit.Audio;
using System.Collections.Generic;
using UnityEngine.Audio;
using Drewio.UnityToolkit.Services;
using Drewio.UnityToolkit.Scenes;
using Drewio.UnityToolkit.Timers;
using Drewio.UnityToolkit.EventBus;
using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.Persistence;

namespace LittleDelay.Composition
{
    ///<summary>
    ///Game-lifetime composition root. It lives on a GameObject placed in the very
    ///first scene loaded, survives every subsequent scene reload vis DontDestroyOnLoad, and 
    ///constructs each root-level toolkit service exactly once per process.
    ///
    /// This is the only place allowed to `new()` up EventBus, TimerManager,
    /// SceneLoadingService, AudioPlaybackService, and SaveSystem. Everything
    /// downstream - starting with MatchCompositionRoot, and in later phases the 
    /// Simulation/Presentation classes it constructs - resolves what it needs
    /// from <see cref="Services"/> once during its own Awake and threads it
    /// through constructors from there. Resolve&lt; T&gt;() should not appear
    /// scattered through gameplay code; if a class needs IEventBus, that 
    /// dependency belongs in its constructor signature, not a locaor lookup
    /// buried inside a method body.
    ///</summary>
    [DefaultExecutionOrder(-1000)] // ensure this is the very first MonoBehaviour to Awake in the process
    public class GameCompositionRoot : MonoBehaviour
    {
        [Header("Audio Banks")]
        [Tooltip("Every music track referenced by name from anywhere in the project.")]
        [SerializeField] private List<SoundDefinition> _musicDefinitions = new();

        [Tooltip("Every SFX referenced by name from anywhere in the project.")]
        [SerializeField] private List<SoundDefinition> _sfxDefinisitons = new();

        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;

        public static GameCompositionRoot Instance { get; private set; }
        
        /// <summary>Root-level service registry. Scene-scoped composition roots resolve from this; nothing else should construct a second ServiceLocator.</summary>
        public ServiceLocator Services { get; private set; }

        private SceneLoadingService _sceneLoadingService;
        private TimerManager _timerManager;


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

            var musicBank = new SoundBank(_musicDefinitions);
            var sfxBank = new SoundBank(_sfxDefinisitons);
            var audioService = new AudioPlaybackService(
                eventBus,
                musicBank,
                sfxBank,
                sourceContainer: transform,
                musicMixerGroup: _musicMixerGroup,
                sfxMixerGroup: _sfxMixerGroup);
            Services.Register(audioService);


            var saveSystem = new SaveSystem();
            Services.Register(saveSystem);
        }

        private void Update()
        {
            // SceneLoadingService's completion callback fires regardless of
            // whether Tick() is ever called - this only drives progress events
            // for whatever loading-screen linstener is write up
            _sceneLoadingService.Tick();
            _timerManager.Tick(Time.deltaTime);
        }
    }
}