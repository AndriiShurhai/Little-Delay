using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 0.5f;
    [SerializeField] private float topClamp = -70f;
    [SerializeField] private float bottomClamp = 70f;

    private float pitch; // Up/Down (X axis)
    private float yaw;   // Left/Right (Y axis)

    private PlayerController _ownerController;

    public float Yaw => yaw;

    private void Awake()
    {
        _ownerController = GetComponentInParent<PlayerController>();
    }

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
        IInputSource inputSource = _ownerController != null ? _ownerController.InputSource : null;
        if (inputSource == null) return;

        Vector2 lookInput = inputSource.GetLookDelta();

        yaw += lookInput.x * lookSensitivity;
        pitch -= lookInput.y * lookSensitivity;

        pitch = Mathf.Clamp(pitch, topClamp, bottomClamp);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}