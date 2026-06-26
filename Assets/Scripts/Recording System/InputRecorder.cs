using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class InputRecorder : MonoBehaviour
{
    public bool IsRecording => isRecording;

    private List<FrameInput> inputFrames = new();
    private bool isRecording = false;
    private bool jumpPressedThisFixedFrame = false;
    private Vector2 accumulatedLookDelta = Vector2.zero;

    private Vector3 recordingStartPosition;

    private Quaternion recordingStartBodyRotation;
    private Vector3 recordingStartCameraRotation;
    private Vector3 recordingStartVelocity;
    private Vector3 recordingStartAngularVelocity;

    private void Start()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnJumpAction += HandleJump;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnJumpAction -= HandleJump;
    }

    private void HandleJump(object sender, EventArgs e)
    {
        if (isRecording)
            jumpPressedThisFixedFrame = true;
    }

    private void Update()
    {
        if (!isRecording) return;
        accumulatedLookDelta += GameInput.Instance.GetLookVector(); // accumulate every Update tick
    }

    public void StartRecording()
    {
        inputFrames.Clear();
        accumulatedLookDelta = Vector2.zero;
        isRecording = true;
        recordingStartPosition = PlayerController.Instance.transform.position;
        recordingStartBodyRotation = PlayerController.Instance.transform.rotation;
        recordingStartCameraRotation = PlayerController.Instance.transform.GetComponentInChildren<CameraTargetController>().GetCurrentRotation();
        recordingStartVelocity = PlayerController.Instance.GetComponent<Rigidbody>().linearVelocity;
        recordingStartAngularVelocity = PlayerController.Instance.GetComponent<Rigidbody>().angularVelocity;
    }

    public Recording StopRecording()
    {
        isRecording = false;
        return new Recording(
            inputFrames,
            recordingStartPosition,
            recordingStartBodyRotation,
            recordingStartCameraRotation,
            recordingStartVelocity,
            recordingStartAngularVelocity
        );
    }

    private void FixedUpdate()
    {
        if (!isRecording) return;

        inputFrames.Add(new FrameInput
        {
            moveDirection = GameInput.Instance.GetMovementVectorNormalized(),

            worldMoveDirection = PlayerController.Instance.CurrentMoveDirection,

            lookDelta = accumulatedLookDelta,
            jumpPressed = jumpPressedThisFixedFrame,
            firePressed = false,
            abilityPressed = false
        });

        accumulatedLookDelta = Vector2.zero;
        jumpPressedThisFixedFrame = false;
    }
}