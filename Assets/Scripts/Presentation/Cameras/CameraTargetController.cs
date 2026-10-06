using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
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
        private System.IDisposable _worldSub;

        public float Yaw => yaw;

        public void Initialize(IEventBus eventBus)
        {
            _worldSub = eventBus.Subscribe<SimWorldCreatedEvent>(e =>
            {
                if (e.Mode != SimWorldMode.Recording) return;

                Vector3 aim = e.World.Player.AimDirection;
                SetRotation(new Vector3(pitch, Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, 0f));
            });
        }

        private void OnDestroy() => _worldSub?.Dispose();

        public void SetRotation(Vector3 angles)
        {
            yaw = angles.y;
            pitch = angles.x;

            transform.rotation = Quaternion.Euler(angles);
        }

        public Vector3 GetCurrentRotation()
        {
            return new Vector3(pitch, yaw, 0f);
        }

        private void Update()
        {
            if (GameInput.Instance == null) return;

            Vector2 lookInput = GameInput.Instance.GetLookVector();

            yaw += lookInput.x * lookSensitivity;
            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, topClamp, bottomClamp);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
