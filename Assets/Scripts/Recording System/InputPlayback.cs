using System;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class InputPlayback : MonoBehaviour, IInputSource
{
    public event Action OnJumpPressed;

    private Recording recording;
    private int currentFrameIndex = 0;
    private int lastConsumedIndex = 0;
    private float playbackStartTime;
    private bool isPlayingBack = false;

    private Vector2 pendingLookDelta = Vector2.zero;

    public bool IsPlayingBack => isPlayingBack;

    public Vector2 GetMoveDirection()
    {
        if (!isPlayingBack || recording == null || lastConsumedIndex >= recording.Frames.Count)
            return Vector2.zero;
        return recording.Frames[lastConsumedIndex].moveDirection;
    }
    public Vector3 GetWorldMoveDirection()
    {
        if (!isPlayingBack || recording == null || lastConsumedIndex >= recording.Frames.Count)
            return Vector3.zero;
        return recording.Frames[lastConsumedIndex].worldMoveDirection;
    }

    public Vector2 GetLookDelta()
    {
        if (!isPlayingBack || recording == null)
            return Vector2.zero;

        Vector2 delta = pendingLookDelta;
        pendingLookDelta = Vector2.zero; 
        return delta;
    }

    public void StartPlayback(Recording recording)
    {
        this.recording = recording;
        currentFrameIndex = 0;
        lastConsumedIndex = 0;
        pendingLookDelta = Vector2.zero;

        Rigidbody rb = PlayerController.Instance.GetComponent<Rigidbody>();

        rb.position = recording.StartPosition;
        rb.rotation = recording.StartBodyRotation;
        rb.linearVelocity = recording.StartVelocity;
        rb.angularVelocity = recording.StartAngularVelocity;

        PlayerController.Instance.transform.GetComponentInChildren<CameraTargetController>().SetRotation(recording.StartCameraRotation);

        Physics.SyncTransforms();

        PlayerController.Instance.SetInputSource(this);
        isPlayingBack = true;
    }

    public void StopPlayback()
    {
        isPlayingBack = false;
    }

    private void FixedUpdate()
    {
        if (!isPlayingBack || recording == null || currentFrameIndex >= recording.Frames.Count)
            return;

        FrameInput currentFrame = recording.Frames[currentFrameIndex];

        pendingLookDelta += currentFrame.lookDelta;

        if (currentFrame.jumpPressed)
            OnJumpPressed?.Invoke();

        lastConsumedIndex = currentFrameIndex;

        currentFrameIndex++;

        if (currentFrameIndex >= recording.Frames.Count)
        {
            StopPlayback();
            Debug.Log("Playback finished.");
        }
    }
}