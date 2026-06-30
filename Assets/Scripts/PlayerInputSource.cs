using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerInputSource : MonoBehaviour, IInputSource
{
    public event Action OnFirePressed;
    public event Action OnJumpPressed;
    public event Action OnMobilityAbilityPressed;
    public bool IsPlayback => false;
    [SerializeField] private Transform _cameraTarget;

    private void Start()
    {
        GameInput.Instance.OnFireAction += HandleFire;
        GameInput.Instance.OnJumpAction += HandleJump;
        GameInput.Instance.OnUseAbilityAction += HandleMobilityAbility;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnFireAction -= HandleFire;
            GameInput.Instance.OnJumpAction -= HandleJump;
            GameInput.Instance.OnUseAbilityAction -= HandleMobilityAbility;
        }
    }

    private void HandleFire()
    {
        OnFirePressed?.Invoke();
    }
    private void HandleJump()
    {
        OnJumpPressed?.Invoke();
    }

    private void HandleMobilityAbility()
    {
        OnMobilityAbilityPressed?.Invoke();
    }

    public Vector3 GetAimPoint()
    {
        // The center of the screen/canvas is always (0.5, 0.5) in viewport space.
        Ray ray = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));

        // Raycast into the world to find what the crosshair is overlapping
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point; // Shoot at what we hit
        }

        // If looking at the sky (no hit), pick a point far away along the ray
        return ray.GetPoint(100f);
    }
    public Vector2 GetMoveInput()
    {
        return GameInput.Instance.GetMovementVectorNormalized();
    }

    public Vector3 GetWorldMoveDirection()
    {
        Vector2 input = GameInput.Instance.GetMovementVectorNormalized();
        if (input == Vector2.zero) return Vector3.zero;
        
        Vector3 forward = _cameraTarget.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = _cameraTarget.right;
        right.y = 0f;
        right.Normalize();

        return (forward * input.y) + (right * input.x); 
    }

    public Vector2 GetLookDelta()
    {
        return GameInput.Instance.GetLookVector();
    }

    public void Tick() { }
}