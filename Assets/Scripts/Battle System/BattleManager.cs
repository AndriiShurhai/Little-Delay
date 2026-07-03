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

    [Header("Dependencies")]
    [SerializeField] private PlayerController _liveController;
    [SerializeField] private InputRecorder _inputRecorder;
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

        _liveController.SetInputSource(FindAnyObjectByType<PlayerInputSource>());
        _fighterSpawner.OnGhostCreated += _roundManager.RegisterFighter;
        _roundManager.OnMatchWon += HandleMatchWon;
        _roundManager.OnMatchDraw += HandleMatchDraw;
        _roundManager.OnRoundContinues += HandleRoundContinues;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnRoundStartAction -= InitializeNextTurn;
        }
        _fighterSpawner.OnGhostCreated -= _roundManager.RegisterFighter;
        _roundManager.OnMatchWon -= HandleMatchWon;
        _roundManager.OnMatchDraw -= HandleMatchDraw;
        _roundManager.OnRoundContinues -= HandleRoundContinues;
    }

    private void Update()
    {
        HandleStateTimers();
    }

    private void HandleStateTimers()
    {
        if (CurrentState == BattleState.Countdown)
        {
            Debug.Log($"Countdown Timer: {_stateTimer}");
            _stateTimer -= Time.deltaTime;

            int currentSecond = Mathf.CeilToInt(_stateTimer);

            if (currentSecond != _lastCountdownSecond)
            {
                Debug.Log($"Countdown Tick: {currentSecond}");
                _lastCountdownSecond = currentSecond;
                OnCountdownTick?.Invoke(currentSecond);
            }

            if (_stateTimer <= 0f)
            {
                Debug.Log("Countdown Finished");
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

        if (_recordingTurn == 1)
        {
            _liveController.Teleport(_spawnPoint.position, _spawnPoint.rotation);
        }

        else if (_recordingTurn == 2)
        {
            _liveController.Teleport(_spawnPointSecond.position, _spawnPointSecond.rotation);
        }

        else
        {
            return;
        }

        _liveController.SetInactiveState(true);
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

        _liveController.SetInputSource(FindFirstObjectByType<PlayerInputSource>());
        _inputRecorder.StartRecording();
    }

    private void EndRecordingPhase()
    {
        Recording recording = _inputRecorder.StopRecording();
        _pastRecordings.Add(recording);

        CurrentState = BattleState.Idle;
        OnRecordingFinished?.Invoke();

        _liveController.SetInactiveState(true);

        if (_recordingTurn == 1)
        {
            _recordingTurn++;
            InitializeNextTurn();
        }
        else if (_recordingTurn == 2)
        {
            _recordingTurn++;
            PrepareBattlePhase();
        }
    }

    private void PrepareBattlePhase()
    {
        _liveController.gameObject.SetActive(false);
        _playerCamera.SetActive(false);

        bool isFirstRoundOfMatch = _fighters.Count == 0;

        if (isFirstRoundOfMatch)
        {
            Transform[] spawnPoints = { _spawnPoint, _spawnPointSecond };

            for (int i = 0; i < _pastRecordings.Count; i++)
            {
                var fighter = new Fighter(i, spawnPoints[i]) { Recording = _pastRecordings[i] };
                _fighterSpawner.SpawnGhost(fighter);
                _fighters.Add(fighter);
            }
        }

        else
        {
            for (int i = 0; i < _fighters.Count; i++)
            {
                _fighters[i].Recording = _pastRecordings[i];
                _fighterSpawner.RespawnGhost(_fighters[i]);
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

        _arenaController?.ShrinkForRound(nextRound);

        _pastRecordings.Clear();
        _recordingTurn = 1;
        CurrentState = BattleState.Idle;

        _liveController.gameObject.SetActive(true);

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

        foreach (var fighter in _fighters)
        {
            if (fighter != null) Destroy(fighter.GhostObject);
        }

        _fighters.Clear();
    }
}