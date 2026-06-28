using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


[System.Serializable]
public struct FrameInput
{
    public Vector2 moveInput;

    public Vector3 worldMoveDirection;

    public Vector2 lookDelta;
    public bool jumpPressed;

    public bool firePressed;
    public bool abilityPressed;
}