using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using TMPro;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Mouse/stick look for the third-person camera target. Pure presentation: it accumulates yaw/pitch from
    /// GameInput and exposes Yaw so PlayerSimInputSource can turn it into the fighter's aim direction.
    /// At the start of every recording the yaw snaps to the fighter's spawn facing so the camera starts looking
    /// where the fighter looks.
    /// </summary>
    public sealed class CameraTargetController : MonoBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float lookSensitivity = 0.5f;
        [SerializeField] private float topClamp = -70f;
        [SerializeField] private float bottomClamp = 70f;

        private float pitch; // Up/Down (X axis)
        private float yaw;   // Left/Right (Y axis)
        private GameInput _input;
        private System.IDisposable _worldSub;

        public float Yaw => yaw;
        public float Pitch => pitch;

        public void Initialize(IEventBus eventBus, GameInput input)
        {
            _input = input;
            _worldSub = eventBus.Subscribe<SimWorldCreatedEvent>(e =>
            {
                if (e.Mode != SimWorldMode.Recording) return;

                Vector3 aim = e.World.Player.AimDirection;
                float flatLen = Mathf.Sqrt(aim.x * aim.x + aim.z * aim.z);
                float startPitch = Mathf.Clamp(-Mathf.Atan2(aim.y, flatLen) * Mathf.Rad2Deg, topClamp, bottomClamp);
                float startYaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
                SetRotation(new Vector3(startPitch, startYaw, 0f));
            });
        }

        private void OnDestroy() => _worldSub?.Dispose();

        public void SetRotation(Vector3 angles)
        {
            pitch = Mathf.Clamp(angles.x, topClamp, bottomClamp);
            yaw = angles.y;

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public Vector3 GetCurrentRotation()
        {
            return new Vector3(pitch, yaw, 0f);
        }

        private void Update()
        {
            if (_input == null) return;

            Vector2 lookInput = _input.GetLookVector();

            yaw += lookInput.x * lookSensitivity;
            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, topClamp, bottomClamp);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
