using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public interface IInputSource
{
    public event Action OnJumpPressed;
    public event Action OnMobilityAbilityPressed;
    public event Action OnFirePressed;
    public bool IsPlayback { get; }
    public Vector2 GetMoveInput();
    public Vector3 GetWorldMoveDirection();
    public Vector2 GetLookDelta();

    public Vector3 GetAimPoint();
    public void Tick();
}