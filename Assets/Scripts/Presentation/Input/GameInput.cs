using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleDelay.Presentation
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        private const string PLAYER_PREFS_BINDINGS = "InputBindings";

        private InputSystem_Actions inputActions;

        public event Action OnPauseAction;
        public event Action OnBindingRebinding;
        public event Action OnJumpAction;
        public event Action OnUseAbilityAction;
        public event Action OnFireAction;

        public enum Binding
        {
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,
            Fire,
            UseAbility,
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
            inputActions.Player.UseAbility.performed += OnUseAbility_performed;
            inputActions.Player.Fire.performed += OnFire_performed;
        }

        private void OnJump_performed(InputAction.CallbackContext ctx) => OnJumpAction?.Invoke();
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

        public bool IsFireHeld()
        {
            return inputActions.Player.Fire.IsPressed();
        }

        private void OnDestroy()
        {
            if (inputActions == null) return;

            inputActions.Player.Jump.performed -= OnJump_performed;
            inputActions.Player.UseAbility.performed -= OnUseAbility_performed;
            inputActions.Player.Fire.performed -= OnFire_performed;
            inputActions.Player.Disable();
            inputActions.Dispose();

            OnPauseAction = null;
            OnBindingRebinding = null;
            OnJumpAction = null;
            OnUseAbilityAction = null;
            OnFireAction = null;
        }

        public string GetBindingText(Binding binding)
        {
            if (!TryResolveBinding(binding, out InputAction action, out int bindingIndex))
                return string.Empty;

            return action.bindings[bindingIndex].ToDisplayString();
        }

        public void RebindBinding(Binding binding, Action onComplete = null)
        {
            if (!TryResolveBinding(binding, out InputAction action, out int bindingIndex))
                return;

            inputActions.Player.Disable();

            action.PerformInteractiveRebinding(bindingIndex)
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

        private bool TryResolveBinding(Binding binding, out InputAction action, out int bindingIndex)
        {
            switch (binding)
            {
                case Binding.MoveUp:
                    action = inputActions.Player.Move;
                    bindingIndex = FindCompositePartIndex(action, "up", fallback: 1);
                    return true;
                case Binding.MoveDown:
                    action = inputActions.Player.Move;
                    bindingIndex = FindCompositePartIndex(action, "down", fallback: 3);
                    return true;
                case Binding.MoveLeft:
                    action = inputActions.Player.Move;
                    bindingIndex = FindCompositePartIndex(action, "left", fallback: 5);
                    return true;
                case Binding.MoveRight:
                    action = inputActions.Player.Move;
                    bindingIndex = FindCompositePartIndex(action, "right", fallback: 7);
                    return true;
                case Binding.Fire:
                    action = inputActions.Player.Fire;
                    bindingIndex = 0;
                    return true;
                case Binding.UseAbility:
                    action = inputActions.Player.UseAbility;
                    bindingIndex = 0;
                    return true;
                case Binding.Jump:
                    action = inputActions.Player.Jump;
                    bindingIndex = 0;
                    return true;
                default:
                    action = null;
                    bindingIndex = -1;
                    return false;
            }
        }

        private static int FindCompositePartIndex(InputAction action, string partName, int fallback)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isPartOfComposite &&
                    string.Equals(action.bindings[i].name, partName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return fallback;
        }
    }
}