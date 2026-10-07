using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Turns live device input into one SimInputFrame per simulation tick. This is the ONLY place in the project
    /// where raw input becomes gameplay input; the simulation never sees GameInput, cameras or screen space.
    /// Movement is camera-relative (using the look yaw), aim is the look direction flattened onto the XZ plane.
    /// Button presses (fire, dash, jump) are latched between ticks so a tap shorter than one tick (20 ms) is never lost.
    /// It only listens while a recording is running; presses during countdowns are discarded.
    /// </summary>
    public sealed class PlayerSimInputSource : MonoBehaviour, ISimInputSource
    {
        [SerializeField] private CameraTargetController _look;

        private GameInput _input;
        private bool _recording;
        private bool _firePressed;
        private bool _dashPressed;
        private bool _jumpPressed;
        private System.IDisposable _startedSub;
        private System.IDisposable _completedSub;

        public void Initialize(IEventBus eventBus, GameInput input)
        {
            _input = input;
            _startedSub = eventBus.Subscribe<RecordingStartedEvent>(_ => { _recording = true; _firePressed = false; _dashPressed = false; _jumpPressed = false; });
            _completedSub = eventBus.Subscribe<RecordingCompletedEvent>(_ => _recording = false);

            _input.OnFireAction += HandleFire;
            _input.OnUseAbilityAction += HandleDash;
            _input.OnJumpAction += HandleJump;
        }

        private void OnDestroy()
        {
            _startedSub?.Dispose();
            _completedSub?.Dispose();

            if (_input != null)
            {
                _input.OnFireAction -= HandleFire;
                _input.OnUseAbilityAction -= HandleDash;
                _input.OnJumpAction -= HandleJump;
            }
        }

        private void HandleFire() { if (_recording) _firePressed = true; }
        private void HandleDash() { if (_recording) _dashPressed = true; }
        private void HandleJump() { if (_recording) _jumpPressed = true; }

        public SimInputFrame GetInput(int tickIndex)
        {
            if (!_recording || _input == null) return SimInputFrame.None;

            Quaternion yaw = Quaternion.Euler(0f, _look.Yaw, 0f);
            Vector3 forward = yaw * Vector3.forward;
            Vector3 right = yaw * Vector3.right;

            Vector2 move2D = _input.GetMovementVectorNormalized();
            Vector3 move = forward * move2D.y + right * move2D.x;

            bool fire = _firePressed || _input.IsFireHeld();
            bool dash = _dashPressed;
            bool jump = _jumpPressed;
            _firePressed = false;
            _dashPressed = false;
            _jumpPressed = false;

            return new SimInputFrame(move, forward, fire, dash, jump);
        }
    }
}