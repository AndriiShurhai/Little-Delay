using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class InputRecorder : MonoBehaviour
{
    public bool IsRecording => _isRecording;

    [SerializeField] private float _passivityThreshold = 3f;
    [SerializeField] private float _maxIdlePercentage = 0.6f;

    private float _minX, _maxX, _minZ, _maxZ;
    private float _idleTime;

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

        _minX = _maxX = _targetRb.position.x;
        _minZ = _maxZ = _targetRb.position.z;
        _idleTime = 0f;
    }

    public Recording StopRecording()
    {
        _isRecording = false;
        UnbindSource();

        float widthMoved = _maxX - _minX;
        float depthMoved = _maxZ - _minZ;
        bool failedAreaTest = (widthMoved < _passivityThreshold) && (depthMoved < _passivityThreshold);

        float totalRecordingDuration = _inputFrames.Count * Time.fixedDeltaTime;
        bool failedIdleTest = totalRecordingDuration > 0f
            && (_idleTime / totalRecordingDuration) > _maxIdlePercentage;

        bool isPassive = failedAreaTest || failedIdleTest;

        return new Recording(
            new List<FrameInput>(_inputFrames),
            _recordingStartPosition,
            _recordingStartBodyRotation,
            _recordingStartCameraRotation,
            _recordingStartVelocity,
            _recordingStartAngularVelocity,
            isPassive
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

        Vector3 pos = _targetRb.position;
        if (pos.x < _minX) _minX = pos.x;
        if (pos.x > _maxX) _maxX = pos.x;
        if (pos.z < _minZ) _minZ = pos.z;
        if (pos.z > _maxZ) _maxZ = pos.z;


        if (_source.GetMoveInput().sqrMagnitude < 0.01f)
        {
            _idleTime += Time.fixedDeltaTime;
        }
    }
}