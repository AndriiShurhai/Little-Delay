
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;
using System.Runtime.CompilerServices;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public enum BattleState
    {
        ShowdownPreview,
        Idle,
        CameraTransition,
        Countdown,
        Recording,
        PreBattleCountdown,
        BattlePhase,
        MatchOver
    }

    public BattleState CurrentState { get; private set; } = BattleState.ShowdownPreview;

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
    [SerializeField] private float _showdownPreviewDuration = 3f;
    [SerializeField] private float _countdownDuration = 3f;
    [SerializeField] private float _recordingDuration = 15f;
    [SerializeField] private float _battleCountdownDuration = 3f;

    [Header("Cameras")]
    [Tooltip("The static overview camera for the battle phase")]
    [SerializeField] private GameObject _arenaCamera;
    [SerializeField] private GameObject _playerCamera;
    [Tooltip("The CinemachineBrain that blends between _arenaCamera and _playerCamera. Assign the one on your Main Camera.")]
    [SerializeField] private CinemachineBrain _cinemachineBrain;

    [Header("Anti-passivity Punishment")]
    [SerializeField] private float _passivityDamagePerTick = 15f;
    [SerializeField] private int _passivityDamageTicks = 3;

    [Header("UI Feedback Events")]
    public UnityEvent OnShowdownPreviewStarted;
    public UnityEvent<int> OnCountdownTick;
    public UnityEvent<float> OnRecordingTick;
    public UnityEvent OnRecordingFinished;
    public UnityEvent OnBattlePhaseStarted;
    public UnityEvent<int> OnRoundContinues;
    public UnityEvent<int> OnFighterWon;
    public UnityEvent OnMatchDraw;
    public UnityEvent<int> OnRoundStarts;
    public UnityEvent OnRecordingStarts;

    [SerializeField] private bool _isPassivityPunishmentOn = true;

    public event Action<Fighter> OnFighterSpawned;
    public event Action OnMatchStarted;

    public PlayerController LivePlayer => _liveController;
    public Transform PlayerSpawnPoint => _spawnPoint;

    private readonly List<Recording> _pastRecordings = new();
    private readonly List<Fighter> _fighters = new();
    private readonly List<Coroutine> _activePenaltyRoutines = new();

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
        _botController.SetInputSource(_botInputSource);
        _fighterSpawner.OnGhostCreated += HandleFighterSpawned;
        _roundManager.OnMatchWon += HandleMatchWon;
        _roundManager.OnMatchDraw += HandleMatchDraw;
        _roundManager.OnRoundContinues += HandleRoundContinues;
        _roundManager.OnRoundStarts += HandleRoundStarts;
        if (_revealSystem != null)
        {
            _revealSystem.OnEnemyPositionRevealed += HandleEnemyPositionRevealed;
        }

        StartShowdownPreview();
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnRoundStartAction -= InitializeNextTurn;
        }
        _fighterSpawner.OnGhostCreated -= HandleFighterSpawned;
        _roundManager.OnMatchWon -= HandleMatchWon;
        _roundManager.OnMatchDraw -= HandleMatchDraw;
        _roundManager.OnRoundContinues -= HandleRoundContinues;
        _roundManager.OnRoundStarts -= HandleRoundStarts;

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
        if (CurrentState == BattleState.ShowdownPreview)
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                EndShowdownPreview();
            }
        }
        else if (CurrentState == BattleState.Countdown)
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

        // BattleState.CameraTransition and BattleState.MatchOver are advanced
        // externally (by a coroutine / a player-driven "continue" call), not by a timer here.
    }

    private void StartShowdownPreview()
    {
        CurrentState = BattleState.ShowdownPreview;
        _stateTimer = _showdownPreviewDuration;
        OnShowdownPreviewStarted?.Invoke();
    }

    private void EndShowdownPreview()
    {
        OnMatchStarted?.Invoke();
        InitializeNextTurn();
    }

    private void InitializeNextTurn()
    {
        if (CurrentState != BattleState.Idle && CurrentState != BattleState.ShowdownPreview)
            return;

        Debug.Log("Initializing next turn: " + _recordingTurn);

        _liveController.Teleport(_spawnPoint.position, _spawnPoint.rotation);
        _botController.Teleport(_spawnPointSecond.position, _spawnPointSecond.rotation);

        _liveController.SetInactiveState(true);
        _botController.SetInactiveState(true);

        _botInputSource.ResetKnowledge();
        _playerCamera.SetActive(true);

        CurrentState = BattleState.CameraTransition;
        StartCoroutine(WaitForCameraBlend(BeginPreRecordingCountdown));
    }

    private void BeginPreRecordingCountdown()
    {
        CurrentState = BattleState.Countdown;
        _stateTimer = _countdownDuration;
        _lastCountdownSecond = -1;
        _revealSystem?.RevealEnemyForDuration(_botController.transform, _countdownDuration);
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
        OnRecordingStarts?.Invoke();
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

        CurrentState = BattleState.CameraTransition;
        StartCoroutine(WaitForCameraBlend(BeginPreBattleCountdown));
    }

    private void BeginPreBattleCountdown()
    {
        CurrentState = BattleState.PreBattleCountdown;
        _stateTimer = _battleCountdownDuration;
        _lastCountdownSecond = -1;
    }

    /// <summary>
    /// Waits for the CinemachineBrain to finish blending between _arenaCamera and _playerCamera
    /// before running onComplete. This keeps countdown timers (and any visual feedback tied to
    /// them, like a spawn-point pulse) from starting mid-cut.
    /// </summary>
    private IEnumerator WaitForCameraBlend(Action onComplete)
    {
        // Give the Brain a frame to register the priority/active change we just made.
        yield return null;

        if (_cinemachineBrain != null)
        {
            yield return new WaitUntil(() => !_cinemachineBrain.IsBlending);
        }

        onComplete?.Invoke();
    }

    private void StartBattlePhase()
    {
        CurrentState = BattleState.BattlePhase;
        OnBattlePhaseStarted?.Invoke();

        foreach (var fighter in _fighters)
        {
            fighter.GhostObject.GetComponent<PlayerController>().SetInvincible(false);
            // fighter.GhostObject.GetComponent<PlayerController>().ResetAllDebuffs();
        }

        for (int i = 0; i < _fighters.Count; i++)
        {
            // Assuming your Recording class has a WasPassive or IsPassive boolean
            if (_pastRecordings[i].WasPassive)
            {
                Debug.Log($"Fighter {i} was passive in the last round. Applying damage ticks.");
                Coroutine penaltyRoutine = StartCoroutine(PassivityDamageRoutine(_fighters[i]));
                _activePenaltyRoutines.Add(penaltyRoutine);
            }
        }
        _fighterSpawner.ResumeALL(_fighters);
    }

    private IEnumerator PassivityDamageRoutine(Fighter fighter)
    {
        if (!_isPassivityPunishmentOn)
        {
            yield break;
        }


        float tickInterval = _recordingDuration / (_passivityDamageTicks + 1);

        for (int i = 0; i < _passivityDamageTicks; i++)
        {
            yield return new WaitForSeconds(tickInterval);

            if (fighter != null && fighter.IsAlive)
            {
                fighter.Health.TakeDamage(_passivityDamagePerTick);
                Debug.Log($"<color=red>Passivity Penalty!</color> Fighter {fighter.ID} took {_passivityDamagePerTick} damage.");
            }
        }
    }

    private void HandleRoundContinues(int nextRound)
    {
        ClearPenaltyRoutines();

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

    private void HandleRoundStarts(int roundNumber)
    {
        OnRoundStarts?.Invoke(roundNumber);
    }

    private void HandleMatchWon(Fighter winner)
    {
        CurrentState = BattleState.MatchOver;
        OnFighterWon?.Invoke(winner.ID);
    }

    private void HandleMatchDraw()
    {
        CurrentState = BattleState.MatchOver;
        OnMatchDraw?.Invoke();
    }

    /// <summary>
    /// Call this from the game-over screen's "continue" / "rematch" button once the
    /// player has actually seen the result. Tearing the arena down happens here,
    /// not the moment the round ends.
    /// </summary>
    public void ContinueAfterMatchOver()
    {
        if (CurrentState != BattleState.MatchOver) return;

        ResetForNextMatch();
        StartShowdownPreview();
    }

    private void ResetForNextMatch()
    {
        ClearPenaltyRoutines();
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
        _playerCamera.SetActive(true);
    }

    private void ClearPenaltyRoutines()
    {
        foreach (var routine in _activePenaltyRoutines)
        {
            if (routine != null) StopCoroutine(routine);
        }
        _activePenaltyRoutines.Clear();
    }
}
