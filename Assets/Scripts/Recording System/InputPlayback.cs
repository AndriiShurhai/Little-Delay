using System;
using Unity.VisualScripting;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class InputPlayback : MonoBehaviour, IInputSource
{
    public event Action OnJumpPressed;

    public event Action OnPlaybackCompleted;

    public enum PlaybackState { Playing, Paused, Stopped }
    public bool IsPlayback => true;
    public bool IsPlayingBack => _playbackState == PlaybackState.Playing;

    private Recording _recording;
    private int _currentFrameIndex;
    private int _lastConsumedIndex;
    private Vector2 _pendingLookDelta;
    private PlaybackState _playbackState = PlaybackState.Stopped;

    public Vector2 GetMoveInput()
    {
        if (!IsPlayingBack || _recording == null || _lastConsumedIndex >= _recording.Frames.Count)
            return Vector2.zero;

        return _recording.Frames[_lastConsumedIndex].moveInput;
    }

    public Vector3 GetWorldMoveDirection()
    {
        if (!IsPlayingBack || _recording == null || _lastConsumedIndex >= _recording.Frames.Count)
            return Vector3.zero;

        if (PlayerController.Instance.IsDeflected)
            return Vector3.zero;

        return _recording.Frames[_lastConsumedIndex].worldMoveDirection;
    }

    public Vector2 GetLookDelta()
    {
        if (!IsPlayingBack || _recording == null)
            return Vector2.zero;

        Vector2 delta = _pendingLookDelta;
        _pendingLookDelta = Vector2.zero;
        return delta;
    }

    public void Tick()
    {
        AdvanceTimeline();
    }

    public void StartPlayback(Recording recording)
    {
        _recording = recording;
        _currentFrameIndex = 0;
        _lastConsumedIndex = 0;
        _pendingLookDelta = Vector2.zero;

        Rigidbody rb = PlayerController.Instance.GetComponent<Rigidbody>();
        rb.position = recording.StartPosition;
        rb.rotation = recording.StartBodyRotation;
        rb.linearVelocity = recording.StartVelocity;
        rb.angularVelocity = recording.StartAngularVelocity;

        PlayerController.Instance
            .GetComponentInChildren<CameraTargetController>()
            .SetRotation(recording.StartCameraRotation);

        Physics.SyncTransforms();

        _playbackState = PlaybackState.Playing;
    }

    public void PausePlayback()
    {
        if (_playbackState == PlaybackState.Playing)
        {
            _playbackState = PlaybackState.Paused;
        }
    }

    public void ResumePlayback()
    {
        if (_playbackState == PlaybackState.Paused)
        {
            _playbackState = PlaybackState.Playing;
        }
    }

    public void StopPlayback()
    {
        Debug.Assert(
            _recording != null && _currentFrameIndex == _recording.Frames.Count,
            $"[PLAYBACK FATAL] Frame count mismatch! Expected {_recording?.Frames.Count}, stopped at {_currentFrameIndex}.");

        _playbackState = PlaybackState.Stopped;
    }

    private void AdvanceTimeline()
    {
        if (!IsPlayingBack || _recording == null || _currentFrameIndex >= _recording.Frames.Count)
            return;


        Debug.Assert(!PlayerController.Instance.IsDeflected,
            "[PLAYBACK FATAL] The timeline advanced while the player was in the Deflected state!");

        FrameInput currentFrame = _recording.Frames[_currentFrameIndex];

        _pendingLookDelta += currentFrame.lookDelta;

        if (currentFrame.jumpPressed)
        {
            OnJumpPressed?.Invoke();
        }
        
        _lastConsumedIndex = _currentFrameIndex;
        _currentFrameIndex++;

        if (_currentFrameIndex >= _recording.Frames.Count)
        {
            StopPlayback();
            OnPlaybackCompleted?.Invoke();
        }
    }

}