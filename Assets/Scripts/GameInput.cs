using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    private const string PLAYER_PREFS_BINDINGS = "InputBindings";

    private InputSystem_Actions inputActions;

    public event Action OnPauseAction;
    public event Action OnBindingRebinding;
    public event Action OnJumpAction;
    public event Action OnRecordAction;
    public event Action OnRecordStopAction;
    public event Action OnPlaybackStopAction;
    public event Action OnPlaybackPauseAction;
    public event Action OnPreviousAbilityAction;
    public event Action OnNextAbilityAction;
    public event Action OnUseAbilityAction;
    public event Action OnFireAction;

    public enum Binding
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Jump,
        Pause,
        GamepadPause
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        inputActions = new InputSystem_Actions();

        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
            inputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));

        inputActions.Player.Enable();

        inputActions.Player.Jump.performed += OnJump_performed;
        inputActions.Player.RecordStart.performed += OnRecordStart_performed;
        inputActions.Player.RecordFinish.performed += OnRecordStop_performed;
        inputActions.Player.PlaybackStop.performed += OnPlaybackStop_performed;
        inputActions.Player.PlaybackPause.performed += OnPlaybackPause_performed;
        inputActions.Player.PreviousAbility.performed += OnPreviousAbility_performed;
        inputActions.Player.NextAbility.performed += OnNextAbility_performed;
        inputActions.Player.UseAbility.performed += OnUseAbility_performed;
        inputActions.Player.Fire.performed += OnFire_performed;
    }

    private void OnJump_performed(InputAction.CallbackContext ctx) => OnJumpAction?.Invoke();
    private void OnRecordStart_performed(InputAction.CallbackContext ctx) => OnRecordAction?.Invoke();
    private void OnRecordStop_performed(InputAction.CallbackContext ctx) => OnRecordStopAction?.Invoke();
    private void OnPlaybackStop_performed(InputAction.CallbackContext ctx) => OnPlaybackStopAction?.Invoke();
    private void OnPlaybackPause_performed(InputAction.CallbackContext ctx) => OnPlaybackPauseAction?.Invoke();
    private void OnPreviousAbility_performed(InputAction.CallbackContext ctx) => OnPreviousAbilityAction?.Invoke();
    private void OnNextAbility_performed(InputAction.CallbackContext ctx) => OnNextAbilityAction?.Invoke();
    private void OnUseAbility_performed(InputAction.CallbackContext ctx) => OnUseAbilityAction?.Invoke();
    private void OnFire_performed(InputAction.CallbackContext ctx) => OnFireAction?.Invoke();

    public Vector2 GetMovementVectorNormalized()
    {
        return inputActions.Player.Move.ReadValue<Vector2>().normalized;
    }

    public Vector2 GetLookVector()
    {
        return inputActions.Player.Look.ReadValue<Vector2>();
    }

    private void OnDestroy()
    {
        if (inputActions == null) return;

        inputActions.Player.Jump.performed -= OnJump_performed;
        inputActions.Player.RecordStart.performed -= OnRecordStart_performed;
        inputActions.Player.RecordFinish.performed -= OnRecordStop_performed;
        inputActions.Player.PlaybackStop.performed -= OnPlaybackStop_performed;
        inputActions.Player.PlaybackPause.performed -= OnPlaybackPause_performed;
        inputActions.Player.PreviousAbility.performed -= OnPreviousAbility_performed;
        inputActions.Player.NextAbility.performed -= OnNextAbility_performed;
        inputActions.Player.UseAbility.performed -= OnUseAbility_performed;
        inputActions.Player.Fire.performed -= OnFire_performed; 
        inputActions.Player.Disable();
        inputActions.Dispose();

        OnPauseAction = null;
        OnBindingRebinding = null;
        OnJumpAction = null;
        OnRecordAction = null;
        OnRecordStopAction = null;
        OnPlaybackStopAction = null;
        OnPlaybackPauseAction = null;
        OnPreviousAbilityAction = null;
        OnNextAbilityAction = null;
        OnUseAbilityAction = null;
        OnFireAction = null;
    }

    public string GetBindingText(Binding binding)
    {
        return binding switch
        {
            Binding.MoveUp => inputActions.Player.Move.bindings[1].ToDisplayString(),
            Binding.MoveDown => inputActions.Player.Move.bindings[3].ToDisplayString(),
            Binding.MoveLeft => inputActions.Player.Move.bindings[5].ToDisplayString(),
            Binding.MoveRight => inputActions.Player.Move.bindings[7].ToDisplayString(),
            Binding.Jump => inputActions.Player.Jump.bindings[0].ToDisplayString(),
            _ => string.Empty
        };
    }

    public void RebindBinding(Binding binding, Action onComplete = null)
    {
        inputActions.Player.Disable();

        InputActionRebindingExtensions.RebindingOperation rebindOp = binding switch
        {
            Binding.MoveUp => inputActions.Player.Move.PerformInteractiveRebinding(1),
            Binding.MoveDown => inputActions.Player.Move.PerformInteractiveRebinding(3),
            Binding.MoveLeft => inputActions.Player.Move.PerformInteractiveRebinding(5),
            Binding.MoveRight => inputActions.Player.Move.PerformInteractiveRebinding(7),
            Binding.Jump => inputActions.Player.Jump.PerformInteractiveRebinding(0),
            _ => null
        };

        rebindOp?
            .OnComplete(callback =>
            {
                inputActions.Player.Enable();
                onComplete?.Invoke();
                callback.Dispose();
                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, inputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
                OnBindingRebinding?.Invoke();
            })
            .OnCancel(callback =>
            {
                inputActions.Player.Enable();
                callback.Dispose();
            })
            .Start();
    }
}