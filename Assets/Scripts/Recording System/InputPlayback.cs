using System;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class InputPlayback : MonoBehaviour, IInputSource
{
    public event Action OnJumpPressed;

    public enum PlaybackState
    {
        Playing,
        Paused,
        Stopped
    }

    private Recording recording;
    private int currentFrameIndex = 0;
    private int lastConsumedIndex = 0;
    private bool isPlayingBack = false;

    private Vector2 pendingLookDelta = Vector2.zero;

    private PlaybackState playbackState;

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

        // Ensure no phantom inputs are sent while deflected
        if (PlayerController.Instance.IsDeflected)
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

        playbackState = PlaybackState.Playing;
        isPlayingBack = true;
    }

    public void PausePlayback()
    {
        if (isPlayingBack)
        {
            playbackState = PlaybackState.Paused;
            isPlayingBack = false;
        }
    }

    public void ResumePlayback()
    {
        if (playbackState == PlaybackState.Paused)
        {
            playbackState = PlaybackState.Playing;
            isPlayingBack = true;
        }
    }

    public void StopPlayback()
    {
        Debug.Assert(recording != null && currentFrameIndex == recording.Frames.Count,
            $"[PLAYBACK FATAL] Frame count mismatch! Expected {recording?.Frames.Count}, but stopped at {currentFrameIndex}.");
        isPlayingBack = false;
        playbackState = PlaybackState.Stopped;
    }

    public void AdvanceTimeline()
    {
        if (!isPlayingBack || recording == null || currentFrameIndex >= recording.Frames.Count)
            return;

        Debug.Assert(!PlayerController.Instance.IsDeflected,
            "[PLAYBACK FATAL] The timeline advanced while the player was in the Deflected state!");

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