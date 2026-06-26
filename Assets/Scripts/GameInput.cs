using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";

    private InputSystem_Actions inputAction;

    public event EventHandler OnPauseAction;
    public event EventHandler OnBindingRebinding;

    public event EventHandler OnJumpAction;
    public event EventHandler OnRecordAction;
    public event EventHandler OnRecordStopAction;
    public event EventHandler OnPlaybackStopAction;
    public event EventHandler OnPlaybackPauseAction;
    public bool IsJumpPressed => inputAction.Player.Jump.triggered;

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
    public static GameInput Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        inputAction = new InputSystem_Actions();

        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
        {
            inputAction.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        inputAction.Player.Enable();

        inputAction.Player.Jump.performed += Jump_performed;
        inputAction.Player.RecordStart.performed += RecordStart_performed;
        inputAction.Player.RecordFinish.performed += RecordStop_performed;
        inputAction.Player.PlaybackStop.performed += PlaybackStop_performed;
        inputAction.Player.PlaybackPause.performed += PlaybackPause_performed;
    }

    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void RecordStop_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnRecordStopAction?.Invoke(this, EventArgs.Empty);
    }

    private void PlaybackPause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnPlaybackPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void RecordStart_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnRecordAction?.Invoke(this, EventArgs.Empty);
    }

    private void PlaybackStop_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnPlaybackStopAction?.Invoke(this, EventArgs.Empty);
    }
    private void Jump_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnJumpAction?.Invoke(this, EventArgs.Empty);
    }

    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = inputAction.Player.Move.ReadValue<Vector2>();

        inputVector = inputVector.normalized;

        return inputVector;
    }

    public Vector2 GetLookVector()
    {
        return inputAction.Player.Look.ReadValue<Vector2>();
    }

    private void OnDestroy()
    {
        OnPauseAction = null;

        if (inputAction != null)
        {
            inputAction.Player.Jump.performed -= Jump_performed;

            inputAction.Player.Disable();
            inputAction.Dispose();
        }

        OnPauseAction = null;
        OnJumpAction = null; 
    }

    public string GetBindingText(Binding binding)
    {
        switch (binding)
        {
            default:

            case Binding.MoveUp:
                return inputAction.Player.Move.bindings[1].ToDisplayString();

            case Binding.MoveDown:
                return inputAction.Player.Move.bindings[3].ToDisplayString();

            case Binding.MoveLeft:
                return inputAction.Player.Move.bindings[5].ToDisplayString();

            case Binding.MoveRight:
                return inputAction.Player.Move.bindings[7].ToDisplayString();

            case Binding.Jump:
                return inputAction.Player.Jump.bindings[0].ToDisplayString();
        }
    }

    public void RebindBinding(Binding binding, Action OnComplete = null)
    {
        inputAction.Player.Disable();

        InputActionRebindingExtensions.RebindingOperation rebindingOperation = null;

        switch (binding)
        {
            default:
            case Binding.MoveUp:
                rebindingOperation = inputAction.Player.Move.PerformInteractiveRebinding(1);
                break;
            case Binding.MoveDown:
                rebindingOperation = inputAction.Player.Move.PerformInteractiveRebinding(3);
                break;
            case Binding.MoveLeft:
                rebindingOperation = inputAction.Player.Move.PerformInteractiveRebinding(5);
                break;
            case Binding.MoveRight:
                rebindingOperation = inputAction.Player.Move.PerformInteractiveRebinding(7);
                break;

            case Binding.Jump:
                rebindingOperation = inputAction.Player.Jump.PerformInteractiveRebinding(0);
                break;
        }

        rebindingOperation?
            .OnComplete((callback) =>
            {
                inputAction.Player.Enable();
                OnComplete?.Invoke();
                callback.Dispose();

                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, inputAction.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
                OnBindingRebinding?.Invoke(this, EventArgs.Empty);
            })
            .OnCancel((callback) =>
            {
                inputAction.Player.Enable();
                callback.Dispose();
            })
            .Start();
    }

}