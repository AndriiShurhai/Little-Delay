using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
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


        [Header("Crosshair alignment")]
        [Tooltip("The rendered Main Camera (with CinemachineBrain). Defaults to Camera.main if left empty.")]
        [SerializeField] private Camera _aimCamera;
        [Tooltip("Optional. Drag your crosshair UI RectTransform here; if empty, uses the screen center.")]
        [SerializeField] private RectTransform _crosshairUI;
        [Tooltip("Distance (m) along the crosshair ray where shots converge. 15-25 matches arena combat range.")]
        [SerializeField] private float _aimConvergenceDistance = 20f;

        private GameInput _input;
        private SimMatchConfig _simMatchConfig;
        private SimFighter _playerFighter;
        private bool _recording;
        private bool _firePressed;
        private bool _dashPressed;
        private bool _jumpPressed;
        private System.IDisposable _worldSub;
        private System.IDisposable _startedSub;
        private System.IDisposable _completedSub;

        public void Initialize(IEventBus eventBus, GameInput input, SimMatchConfig config = null)
        {
            _simMatchConfig = config;
            _input = input;
            if (_aimCamera == null) _aimCamera = Camera.main;

            _worldSub = eventBus.Subscribe<SimWorldCreatedEvent>(e =>
            {
                _playerFighter = e.Mode == SimWorldMode.Recording ? e.World.Player : null;
            });

            _startedSub = eventBus.Subscribe<RecordingStartedEvent>(_ =>
            {
                _recording = true;
                _firePressed = false;
                _dashPressed = false;
                _jumpPressed = false;
            });
            _completedSub = eventBus.Subscribe<RecordingCompletedEvent>(_ => _recording = false);

            _input.OnFireAction += HandleFire;
            _input.OnUseAbilityAction += HandleDash;
            _input.OnJumpAction += HandleJump;
        }

        private void OnDestroy()
        {
            _worldSub?.Dispose();
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

            Quaternion flatYaw = Quaternion.Euler(0f, _look.Yaw, 0f);
            Vector3 flatForward = flatYaw * Vector3.forward;
            Vector3 flatRight = flatYaw * Vector3.right;

            Vector2 move2D = _input.GetMovementVectorNormalized();
            Vector3 move = flatForward * move2D.y + rightVector(flatRight, move2D.x);

            Vector3 aim3D = ComputeCrosshairAimDirection();

            bool fire = _firePressed || _input.IsFireHeld();
            bool dash = _dashPressed;
            bool jump = _jumpPressed;
            _firePressed = false;
            _dashPressed = false;
            _jumpPressed = false;

            return new SimInputFrame(move, aim3D, fire, dash, jump);
        }

        private static Vector3 rightVector(Vector3 flatRight, float x) => flatRight * x;

        private Vector3 ComputeCrosshairAimDirection()
        {
            if (_aimCamera == null) _aimCamera = Camera.main;

            if (_aimCamera != null && _playerFighter != null)
            {
                Ray crosshairRay = _crosshairUI != null
                    ? _aimCamera.ScreenPointToRay(_crosshairUI.position)
                    : _aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                float distance = Mathf.Max(5f, _aimConvergenceDistance);
                Vector3 targetPoint = crosshairRay.GetPoint(distance);

                float aimHeight = _simMatchConfig != null ? _simMatchConfig.FighterAimHeight : 1f;
                Vector3 muzzlePosition = _playerFighter.Position + Vector3.up * aimHeight;

                Vector3 toTarget = targetPoint - muzzlePosition;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    return toTarget.normalized;
                }
            }

            return Quaternion.Euler(_look.Pitch, _look.Yaw, 0f) * Vector3.forward;
        }
    }
}