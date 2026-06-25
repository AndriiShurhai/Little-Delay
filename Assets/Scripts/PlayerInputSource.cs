using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerInputSource : MonoBehaviour, IInputSource
{
    public event Action OnJumpPressed;

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
    private void HandleJump(object sender, EventArgs e)
    {
        OnJumpPressed?.Invoke();
    }
    public Vector2 GetMoveDirection()
    {
        return GameInput.Instance.GetMovementVectorNormalized();
    }
    public Vector3 GetWorldMoveDirection()
    {
        return Vector3.zero;
    }
    public Vector2 GetLookDelta()
    {
        return GameInput.Instance.GetLookVector();
    }
    public bool GetJump()
    {
        return GameInput.Instance.IsJumpPressed;
    }
}