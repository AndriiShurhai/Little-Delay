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
        // Notice we changed this from OnRoundStart to InitializeNextTurn
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
                OnCountdownTick?.Invoke(currentSecond); // Send 3, 2, 1 to UI
            }

            if (_stateTimer <= 0f)
            {
                StartRecordingPhase();
            }
        }
        else if (CurrentState == BattleState.Recording)
        {
            _stateTimer -= Time.deltaTime;
            OnRecordingTick?.Invoke(_stateTimer); // Send 9.99, 9.98 to UI

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
                // We can reuse the same UI event for both countdowns
                OnCountdownTick?.Invoke(currentSecond);
            }

            if (_stateTimer <= 0f)
            {
                StartBattlePhase();
            }
        }
    }

    /// <summary>
    /// Called via input to start the next sequence (Turn 1 or Turn 2)
    /// </summary>
    private void InitializeNextTurn()
    {
        if (CurrentState == BattleState.Countdown || CurrentState == BattleState.Recording) 
            return; // Prevent triggering while already in progress

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
            return; // We already did both turns
        }

        // Lock the player in place during the countdown
        _liveController.SetInactiveState(true);

        CurrentState = BattleState.Countdown;
        _stateTimer = _countdownDuration;
        _lastCountdownSecond = -1;
    }

    private void StartRecordingPhase()
    {
        CurrentState = BattleState.Recording;
        _stateTimer = _recordingDuration;
        _liveController.SetInactiveState(false); // Allow player to move and record inputs

        // Give the player their controls back
        _liveController.SetInputSource(FindFirstObjectByType<PlayerInputSource>());
        _inputRecorder.StartRecording();
    }

    private void EndRecordingPhase()
    {
        Recording recording = _inputRecorder.StopRecording();
        _pastRecordings.Add(recording);
        
        CurrentState = BattleState.Idle;
        OnRecordingFinished?.Invoke();

        // Lock player again after recording finishes
        _liveController.SetInactiveState(true);

        if (_recordingTurn == 1)
        {
            _recordingTurn++;
            // Automatically initialize turn 2, or you can wait for user input
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

        foreach (var pastRecording in _pastRecordings)
        {
            GameObject ghost = Instantiate(_ghostPrefab, pastRecording.StartPosition, pastRecording.StartBodyRotation);

            InputPlayback ghostPlayback = ghost.GetComponent<InputPlayback>();
            PlayerController ghostController = ghost.GetComponent<PlayerController>();

            ghostController.SetInputSource(ghostPlayback);
            ghostPlayback.StartPlayback(pastRecording);

            // Immediately pause so they stand still during the countdown
            ghostPlayback.PausePlayback();

            _activeGhosts.Add(ghost);
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

}