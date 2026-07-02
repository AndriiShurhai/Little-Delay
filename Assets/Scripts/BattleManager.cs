using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BattleManager : MonoBehaviour 
{
    public static BattleManager Instance { get; private set; }

    public enum BattleState { Idle, Countdown, Recording, PreBattleCountdown, BattlePhase }
    public BattleState CurrentState { get; private set; } = BattleState.Idle;

    [Header("Dependencies")]
    [SerializeField] private PlayerController _liveController;
    [SerializeField] private InputRecorder _inputRecorder;
    [SerializeField] private GameObject _ghostPrefab;
    
    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _spawnPointSecond;

    [Header("Phase Timings")]
    [SerializeField] private float _countdownDuration = 3f;
    [SerializeField] private float _recordingDuration = 10f;
    [SerializeField] private float _battleCountdownDuration = 3f;

    [Header("Cameras")]
    [Tooltip("The static overview camera for the battle phase")]
    [SerializeField] private GameObject _arenaCamera;

    [Header("UI Feedback Events")]
    public UnityEvent<int> OnCountdownTick;       // Fires every second during countdown (3, 2, 1)
    public UnityEvent<float> OnRecordingTick;     // Fires every frame during recording with remaining time
    public UnityEvent OnRecordingFinished;        // Fires when the time limit hits 0
    public UnityEvent OnBattlePhaseStarted;       // Fires when ghosts spawn and fight

    public PlayerController LivePlayer => _liveController;

    private List<Recording> _pastRecordings = new();
    private List<GameObject> _activeGhosts = new();

    private int _recordingTurn = 1;
    private float _stateTimer;
    private int _lastCountdownSecond;
    private int _currentRound = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GameInput.Instance.OnRoundStartAction += InitializeNextTurn;
    }
    
    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnRoundStartAction -= InitializeNextTurn;
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

        if (_arenaCamera != null) _arenaCamera.SetActive(false);

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

        if (_currentRound == 1)
        {
            foreach (var pastRecording in _pastRecordings)
            {
                GameObject ghost = Instantiate(_ghostPrefab, pastRecording.StartPosition, pastRecording.StartBodyRotation);

                InputPlayback ghostPlayback = ghost.GetComponent<InputPlayback>();
                PlayerController ghostController = ghost.GetComponent<PlayerController>();

                ghostController.SetInputSource(ghostPlayback);
                ghostPlayback.StartPlayback(pastRecording);

                ghostPlayback.OnPlaybackCompleted += OnGhostPlaybackComplete;
                ghost.GetComponent<GhostLifecycle>().OnGhostDied += OnGhostDied;

                ghostPlayback.PausePlayback();

                _activeGhosts.Add(ghost);
            }
        }

        else
        {
            for (int i = 0; i < _activeGhosts.Count; i++)
            {
                GameObject ghost = _activeGhosts[i];
                ghost.SetActive(true);

                InputPlayback ghostPlayback = ghost.GetComponent<InputPlayback>();
                PlayerController ghostController = ghost.GetComponent<PlayerController>();

                ghostController.Teleport(_pastRecordings[i].StartPosition, _pastRecordings[i].StartBodyRotation);

                ghostController.SetInputSource(ghostPlayback);
                ghostPlayback.StartPlayback(_pastRecordings[i]);

                ghostPlayback.PausePlayback();

            }
        }


        CurrentState = BattleState.PreBattleCountdown;
        _stateTimer = _battleCountdownDuration;
        _lastCountdownSecond = -1;
    }

    private void StartBattlePhase()
    {
        CurrentState = BattleState.BattlePhase;
        OnBattlePhaseStarted?.Invoke();

        _liveController.gameObject.SetActive(false);

        if(_arenaCamera != null) _arenaCamera.SetActive(true);

        foreach (var ghost in _activeGhosts)
        {
            if (ghost != null)
            {
                ghost.GetComponent<InputPlayback>().ResumePlayback();
            }
        }
    }

    private void OnGhostDied(GameObject ghost)
    {
        _activeGhosts.Remove(ghost);
        _pastRecordings.Clear();

        foreach (var remainingGhost in _activeGhosts)
        {
            if (remainingGhost != null)
            {
                Destroy(remainingGhost);
            }
        }

        _currentRound = 1;
        CurrentState = BattleState.Idle;
        _liveController.gameObject.SetActive(true);
    }
    private void OnGhostPlaybackComplete()
    {
        bool allGhostsCompleted = true;
        foreach (var ghost in _activeGhosts)
        {
            if (ghost != null && ghost.GetComponent<InputPlayback>().IsPlayingBack)
            {
                allGhostsCompleted = false;
                break;
            }
        }
        if (allGhostsCompleted)
        {
            HandleAllGhostsCompleted();
            _currentRound++;
        }
    }

    private void HandleAllGhostsCompleted()
    {
        _spawnPoint.position = _activeGhosts[0].transform.position;
        _spawnPoint.rotation = _activeGhosts[0].transform.rotation;

        _spawnPointSecond.position = _activeGhosts[1].transform.position;
        _spawnPointSecond.rotation = _activeGhosts[1].transform.rotation;

        foreach (var ghost in _activeGhosts)
        {
            if (ghost != null)
            {
                ghost.SetActive(false);
            }
        }


        _pastRecordings.Clear();
        _recordingTurn = 1;
        CurrentState = BattleState.Idle;

        _liveController.gameObject.SetActive(true);

        InitializeNextTurn();
    }

}