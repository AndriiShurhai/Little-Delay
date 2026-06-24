using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 50f;
    [SerializeField] private float topClamp = -70f;
    [SerializeField] private float bottomClamp = 70f;

    private float pitch; // Up/Down (X axis)
    private float yaw;   // Left/Right (Y axis)

    private void Update()
    {
        Vector2 lookInput = GameInput.Instance.GetLookVector();

        yaw += lookInput.x * lookSensitivity * Time.deltaTime;
        pitch -= lookInput.y * lookSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, topClamp, bottomClamp);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}