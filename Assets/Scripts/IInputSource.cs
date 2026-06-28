using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public interface IInputSource
{
    public event Action OnJumpPressed;

    public bool IsPlayback { get; }
    public Vector2 GetMoveInput();
    public Vector3 GetWorldMoveDirection();
    public Vector2 GetLookDelta();
    public void Tick();
}