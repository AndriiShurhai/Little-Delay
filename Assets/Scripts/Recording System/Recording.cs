using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Recording
{
    public List<FrameInput> Frames;
    public Vector3 StartPosition;
    public Quaternion StartBodyRotation; 
    public Vector3 StartCameraRotation;
    public Vector3 StartVelocity;        
    public Vector3 StartAngularVelocity; 

    public Recording(
        List<FrameInput> frames,
        Vector3 startPosition,
        Quaternion startBodyRotation,
        Vector3 startCameraRotation,
        Vector3 startVelocity,
        Vector3 startAngularVelocity)
    {
        Frames = new List<FrameInput>(frames);
        StartPosition = startPosition;
        StartBodyRotation = startBodyRotation;
        StartCameraRotation = startCameraRotation;
        StartVelocity = startVelocity;
        StartAngularVelocity = startAngularVelocity;
    }
}