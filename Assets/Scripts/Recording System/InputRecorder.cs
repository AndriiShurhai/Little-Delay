using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class InputRecorder : MonoBehaviour
{
    public bool IsRecording => _isRecording;

    private readonly List<FrameInput> _inputFrames = new();

    private bool _isRecording;
    private bool _jumpPressedThisFixedFrame;
    private bool _mobilityAbilityPressedThisFixedFrame;

    private Vector2 _accumulatedLookDelta;

    private Vector3 _recordingStartPosition;
    private Quaternion _recordingStartBodyRotation;
    private Vector3 _recordingStartCameraRotation;
    private Vector3 _recordingStartVelocity;
    private Vector3 _recordingStartAngularVelocity;

    private Rigidbody _playerRb;

    private CameraTargetController _cameraTarget;

    private void Start()
    {
        _playerRb = PlayerController.Instance.GetComponent<Rigidbody>();
        _cameraTarget = PlayerController.Instance.GetComponentInChildren<CameraTargetController>();

        GameInput.Instance.OnJumpAction += HandleJump;
        GameInput.Instance.OnUseAbilityAction += HandleMobilityAbility;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnJumpAction -= HandleJump;
            GameInput.Instance.OnUseAbilityAction -= HandleMobilityAbility;
        }
    }

    private void HandleJump()
    {
        if (_isRecording)
        {
            _jumpPressedThisFixedFrame = true;

        }
    }

    private void HandleMobilityAbility()
    {
        if (_isRecording)
        {
            _mobilityAbilityPressedThisFixedFrame = true;
        }
    }

    private void Update()
    {
        if (!_isRecording) return;

        _accumulatedLookDelta += GameInput.Instance.GetLookVector();
    }

    public void StartRecording()
    {
        _inputFrames.Clear();
        _accumulatedLookDelta = Vector2.zero;
        _isRecording = true;

        _recordingStartPosition = PlayerController.Instance.transform.position;
        _recordingStartBodyRotation = PlayerController.Instance.transform.rotation;
        _recordingStartCameraRotation = _cameraTarget.GetCurrentRotation();
        _recordingStartVelocity = _playerRb.linearVelocity;
        _recordingStartAngularVelocity = _playerRb.angularVelocity;
    }

    public Recording StopRecording()
    {
        _isRecording = false;
        return new Recording(
            _inputFrames,
            _recordingStartPosition,
            _recordingStartBodyRotation,
            _recordingStartCameraRotation,
            _recordingStartVelocity,
            _recordingStartAngularVelocity
        );
    }

    private void FixedUpdate()
    {
        if (!_isRecording) return;

        _inputFrames.Add(new FrameInput
        {
            moveInput = GameInput.Instance.GetMovementVectorNormalized(),
            worldMoveDirection = PlayerController.Instance.CurrentMoveDirection,
            lookDelta = _accumulatedLookDelta,
            jumpPressed = _jumpPressedThisFixedFrame,
            firePressed = false,
            abilityPressed = _mobilityAbilityPressedThisFixedFrame
        });

        _accumulatedLookDelta = Vector2.zero;
        _jumpPressedThisFixedFrame = false;
        _mobilityAbilityPressedThisFixedFrame = false;
    }
}