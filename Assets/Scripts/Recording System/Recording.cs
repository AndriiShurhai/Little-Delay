using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Recording
{
    public IReadOnlyList<FrameInput> Frames { get; }
    public Vector3 StartPosition { get; }
    public Quaternion StartBodyRotation { get; }
    public Vector3 StartCameraRotation { get; }
    public Vector3 StartVelocity { get; }
    public Vector3 StartAngularVelocity { get; }

    public bool WasPassive { get; }

    public Recording(
        List<FrameInput> frames,
        Vector3 startPosition,
        Quaternion startBodyRotation,
        Vector3 startCameraRotation,
        Vector3 startVelocity,
        Vector3 startAngularVelocity,
        bool wasPassive)
    {
        Frames = frames.AsReadOnly();
        StartPosition = startPosition;
        StartBodyRotation = startBodyRotation;
        StartCameraRotation = startCameraRotation;
        StartVelocity = startVelocity;
        StartAngularVelocity = startAngularVelocity;
        WasPassive = wasPassive;
    }
}