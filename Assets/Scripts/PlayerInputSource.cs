using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerInputSource : MonoBehaviour, IInputSource
{

    public event Action OnJumpPressed;
    public bool IsPlayback => false;
    [SerializeField] private Transform _cameraTarget;

    private void Start()
    {
        GameInput.Instance.OnJumpAction += HandleJump;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnJumpAction -= HandleJump;
        }
    }

    private void HandleJump()
    {
        OnJumpPressed?.Invoke();
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