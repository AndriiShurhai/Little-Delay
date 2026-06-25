using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public interface IInputSource
{
    event Action OnJumpPressed;
    Vector2 GetMoveDirection();
    Vector3 GetWorldMoveDirection();
    Vector2 GetLookDelta();
}