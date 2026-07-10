using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity;
using UnityEngine.Events;
using System.Runtime.CompilerServices;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public enum BattleState { Idle, Countdown, Recording, PreBattleCountdown, BattlePhase}

    public BattleState CurrentState { get; private set; } = BattleState.Idle;

    [Header("Player")]
    [SerializeField] private PlayerController _liveController;
    [SerializeField] private InputRecorder _playerRecorder;


    [Header("AI Opponent")]
    [SerializeField] private PlayerController _botController;
    [SerializeField] private AIInputSource _botInputSource;
    [SerializeField] private InputRecorder _botRecorder;

    [SerializeField] private RevealSystem _revealSystem;
    
    [Header("Dependencies")]
    [SerializeField] private FighterSpawner _fighterSpawner;
    [SerializeField] private RoundManager _roundManager;
    [SerializeField] private ArenaController _arenaController;

    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _spawnPointSecond;

    [Header("Phase Timing")]
    [SerializeField] private readonly float _countdownDuration = 3f;
    [SerializeField] private readonly float _recordingDuration = 10f;
    [SerializeField] private readonly float _battleCountdownDuration = 3f;

    [Header("Cameras")]
    [Tooltip("The static overview camera for the battle phase")]
    [SerializeField] private GameObject _arenaCamera;
    [SerializeField] private GameObject _playerCamera;

    [Header("UI Feedback Events")]
    public UnityEvent<int> OnCountdownTick;
    public UnityEvent<float> OnRecordingTick;
    public UnityEvent OnRecordingFinished;
    public UnityEvent OnBattlePhaseStarted;
    public UnityEvent<int> OnRoundContinues;
    public UnityEvent<int> OnFighterWon;
    public UnityEvent OnMatchDraw;

    public event Action<Fighter> OnFighterSpawned;
    public event Action OnMatchStarted;

    public PlayerController LivePlayer => _liveController;

    private readonly List<Recording> _pastRecordings = new();
    private readonly List<Fighter> _fighters = new();

    private int _recordingTurn = 1;
    private float _stateTimer;
    private int _lastCountdownSecond;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GameInput.Instance.OnRoundStartAction += InitializeNextTurn;
        GameInput.Instance.OnRoundStartAction += () => OnMatchStarted?.Invoke();

        _liveController.SetInputSource(FindAnyObjectByType<PlayerInputSource>());
        _botController.SetInputSource(_botInputSource);
        _fighterSpawner.OnGhostCreated += HandleFighterSpawned;
        _roundManager.OnMatchWon += HandleMatchWon;
        _roundManager.OnMatchDraw += HandleMatchDraw;
        _roundManager.OnRoundContinues += HandleRoundContinues;

        if (_revealSystem != null)
        {
            _revealSystem.OnEnemyPositionRevealed += HandleEnemyPositionRevealed;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnRoundStartAction -= InitializeNextTurn;
            GameInput.Instance.OnRoundStartAction -= () => OnMatchStarted?.Invoke();
        }
        _fighterSpawner.OnGhostCreated -= HandleFighterSpawned;
        _roundManager.OnMatchWon -= HandleMatchWon;
        _roundManager.OnMatchDraw -= HandleMatchDraw;
        _roundManager.OnRoundContinues -= HandleRoundContinues;

        if (_revealSystem != null)
        {
            _revealSystem.OnEnemyPositionRevealed -= HandleEnemyPositionRevealed;
        }

    }

    private void Update()
    {
        HandleStateTimers();
    }

    private void HandleStateTimers()
    {
        if (CurrentState == BattleState.Countdown)
        {
            _stateTimer -= Time.deltaTime;

            int currentSecond = Mathf.CeilToInt(_stateTimer);

            if (currentSecond != _lastCountdownSecond)
            {
                _lastCountdownSecond = currentSecond;
                OnCountdownTick?.Invoke(currentSecond);
            }

            if (_stateTimer <= 0f)
            {
                StartRecordingPhase();
            }
        }
        else if (CurrentState == BattleState.Recording)
        {
            _stateTimer -= Time.deltaTime;
            OnRecordingTick?.Invoke(_stateTimer);

            if (_stateTimer <= 0f)
            {
                EndRecordingPhase();
            }
        }

        else if (CurrentState == BattleState.PreBattleCountdown)
        {
            _stateTimer -= Time.deltaTime;

            int currentSecond = Mathf.CeilToInt(_stateTimer);
            if (currentSecond != _lastCountdownSecond)
            {
                _lastCountdownSecond = currentSecond;
                OnCountdownTick?.Invoke(currentSecond);
            }

            if (_stateTimer <= 0f)
            {
                StartBattlePhase();
            }
        }
    }

    private void InitializeNextTurn()
    {
        if (CurrentState == BattleState.Countdown || CurrentState == BattleState.Recording)
            return;
        
        Debug.Log("Initializing next turn: " + _recordingTurn);

        _liveController.Teleport(_spawnPoint.position, _spawnPoint.rotation);
        _botController.Teleport(_spawnPointSecond.position, _spawnPointSecond.rotation);

        _liveController.SetInactiveState(true);
        _botController.SetInactiveState(true);

        _botInputSource.ResetKnowledge();
        _playerCamera.SetActive(true);

        CurrentState = BattleState.Countdown;
        _stateTimer = _countdownDuration;
        _lastCountdownSecond = -1;
    }

    private void StartRecordingPhase()
    {
        CurrentState = BattleState.Recording;
        _stateTimer = _recordingDuration;

        _liveController.SetInactiveState(false);
        _botController.SetInactiveState(false);

        _liveController.SetInputSource(FindFirstObjectByType<PlayerInputSource>());
        _playerRecorder.BeginRecording(_liveController, _liveController.InputSource);

        _botController.SetInputSource(_botInputSource);
        _botRecorder.BeginRecording(_botController, _botInputSource);

        if (_revealSystem != null)
        {
            _revealSystem.BeginReveals(_liveController.transform, _botInputSource, _recordingDuration);
        }
    }

    private void EndRecordingPhase()
    {
        if (_revealSystem != null) _revealSystem.StopReveal();

        Recording playerRecording = _playerRecorder.StopRecording();
        Recording botRecording = _botRecorder.StopRecording();

        _pastRecordings.Clear();
        _pastRecordings.Add(playerRecording);
        _pastRecordings.Add(botRecording);

        CurrentState = BattleState.Idle;
        OnRecordingFinished?.Invoke();

        _liveController.SetInactiveState(true);
        _botController.SetInactiveState(true);

        PrepareBattlePhase();
    }

    private void HandleFighterSpawned(Fighter fighter)
    {
        _roundManager.RegisterFighter(fighter);
        OnFighterSpawned?.Invoke(fighter);
    }

    private void HandleEnemyPositionRevealed(Transform aiTransform)
    {
        Debug.Log($"Enemy position revealed at: {aiTransform.position}");
    }

    private void PrepareBattlePhase()
    {
        _liveController.gameObject.SetActive(false);
        _botController.gameObject.SetActive(false);

        _playerCamera.SetActive(false);

        bool isFirstRoundOfMatch = _fighters.Count == 0;

        if (isFirstRoundOfMatch)
        {
            Transform[] spawnPoints = { _spawnPoint, _spawnPointSecond };

            for (int i = 0; i < _pastRecordings.Count; i++)
            {
                var fighter = new Fighter(i, spawnPoints[i]) { Recording = _pastRecordings[i] };
                _fighterSpawner.SpawnGhost(fighter);
                fighter.GhostObject.GetComponent<PlayerController>().SetInvincible(true);
                _fighters.Add(fighter);
            }
        }

        else
        {
            for (int i = 0; i < _fighters.Count; i++)
            {
                _fighters[i].Recording = _pastRecordings[i];
                _fighterSpawner.RespawnGhost(_fighters[i]);
                _fighters[i].GhostObject.GetComponent<PlayerController>().SetInvincible(true);
            }
        }

        _roundManager.BeginRound();

        CurrentState = BattleState.PreBattleCountdown;
        _stateTimer = _battleCountdownDuration;
        _lastCountdownSecond = -1;
    }

    private void StartBattlePhase()
    {
        CurrentState = BattleState.BattlePhase;
        OnBattlePhaseStarted?.Invoke();

        foreach (var fighter in _fighters)
        {
            fighter.GhostObject.GetComponent<PlayerController>().SetInvincible(false);
        }
        _fighterSpawner.ResumeALL(_fighters);
    }

    private void HandleRoundContinues(int nextRound) 
    {
        foreach (Fighter fighter in _fighters)
        {
            fighter.SpawnPoint.position = fighter.GhostObject.transform.position;
            fighter.SpawnPoint.rotation = fighter.GhostObject.transform.rotation;
            fighter.GhostObject.SetActive(false);
        }

        _arenaController?.InwardMove(nextRound);

        _pastRecordings.Clear();
        _recordingTurn = 1;
        CurrentState = BattleState.Idle;

        _liveController.gameObject.SetActive(true);
        _botController.gameObject.SetActive(true);

        OnRoundContinues?.Invoke(nextRound);

        InitializeNextTurn();
    }

    private void HandleMatchWon(Fighter winner)
    {
        OnFighterWon?.Invoke(winner.ID);
        ResetForNextMatch();
    }

    private void HandleMatchDraw()
    {
        OnMatchDraw?.Invoke();
        ResetForNextMatch();
    }

    private void ResetForNextMatch()
    {
        _roundManager.ClearForNewMatch();
        _pastRecordings.Clear();
        _recordingTurn = 1;
        CurrentState = BattleState.Idle;

        _arenaController?.ResetArena();

        _liveController.gameObject.SetActive(true);
        _botController.gameObject.SetActive(true);

        foreach (var fighter in _fighters)
        {
            if (fighter != null) Destroy(fighter.GhostObject);
        }

        _fighters.Clear();
    }
}