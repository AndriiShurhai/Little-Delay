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
    private bool _firePressedThisFixedFrame;

    private Vector2 _accumulatedLookDelta;

    private Vector3 _recordingStartPosition;
    private Quaternion _recordingStartBodyRotation;
    private Vector3 _recordingStartCameraRotation;
    private Vector3 _recordingStartVelocity;
    private Vector3 _recordingStartAngularVelocity;


    private PlayerController _target;
    private IInputSource _source;
    private Rigidbody _targetRb;


    private CameraTargetController _cameraTarget;

    public void BeginRecording(PlayerController target, IInputSource source)
    {
        UnbindSource();

        _target = target;
        _source = source;

        _targetRb = _target.GetComponent<Rigidbody>();
        _cameraTarget = _target.GetComponentInChildren<CameraTargetController>();

        _source.OnFirePressed += HandleFire;
        _source.OnJumpPressed += HandleJump;
        _source.OnMobilityAbilityPressed += HandleMobilityAbility;


        _inputFrames.Clear();
        _accumulatedLookDelta = Vector3.zero;
        _jumpPressedThisFixedFrame = false;
        _firePressedThisFixedFrame = false;
        _mobilityAbilityPressedThisFixedFrame = false;

        _isRecording = true;

        _recordingStartPosition = _targetRb.position;
        _recordingStartBodyRotation = _targetRb.rotation;
        _recordingStartCameraRotation = _cameraTarget != null ? _cameraTarget.GetCurrentRotation() : Vector3.zero;
        _recordingStartVelocity = _targetRb.linearVelocity;
        _recordingStartAngularVelocity = _targetRb.angularVelocity;
    }

    public Recording StopRecording()
    {
        _isRecording = false;
        UnbindSource();
        return new Recording(
            new List<FrameInput>(_inputFrames),
            _recordingStartPosition,
            _recordingStartBodyRotation,
            _recordingStartCameraRotation,
            _recordingStartVelocity,
            _recordingStartAngularVelocity
        );
    }

    private void UnbindSource()
    {
        if (_source == null) return;

        _source.OnFirePressed -= HandleFire;
        _source.OnJumpPressed -= HandleJump;
        _source.OnMobilityAbilityPressed -= HandleMobilityAbility;
        _source = null;
    }

    private void HandleFire()
    {
        _firePressedThisFixedFrame = true;
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

        _accumulatedLookDelta += _source.GetLookDelta();
    }

    private void FixedUpdate()
    {
        if (!_isRecording) return;

        _inputFrames.Add(new FrameInput
        {
            moveInput = _source.GetMoveInput(),
            worldMoveDirection = _target.CurrentMoveDirection,
            lookDelta = _accumulatedLookDelta,
            aimPoint = _source.GetAimPoint(),
            jumpPressed = _jumpPressedThisFixedFrame,
            firePressed = _firePressedThisFixedFrame,
            abilityPressed = _mobilityAbilityPressedThisFixedFrame
        });

        _accumulatedLookDelta = Vector2.zero;
        _jumpPressedThisFixedFrame = false;
        _firePressedThisFixedFrame = false;
        _mobilityAbilityPressedThisFixedFrame = false;
    }
}