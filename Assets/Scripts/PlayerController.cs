using System;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Ground Detection")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("References")]
    [SerializeField] private Transform cameraTarget; 


    private Rigidbody rb;
    private IInputSource inputSource;
    private bool isGrounded;

    private Vector3 startPosition;
    private Vector3 startRotation;

    private bool isPlaybackActive = false;

    private Vector3 cachedForward;
    private Vector3 cachedRight;

    private bool pendingJump = false;

    public bool IsPlayingBack => isPlaybackActive;

    public Vector3 StartPosition => startPosition;
    public Vector3 StartRotation => startRotation;

    public IInputSource InputSource => inputSource;
    public Vector3 CurrentMoveDirection { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetInputSource(IInputSource source)
    {
        if (inputSource != null)
            inputSource.OnJumpPressed -= GameInput_OnJumpAction;

        isPlaybackActive = source is InputPlayback;
        inputSource = source;
        inputSource.OnJumpPressed += GameInput_OnJumpAction;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; 

        startPosition = transform.position;
        startRotation = transform.eulerAngles;
    }

    private void Update()
    {
        cachedForward = cameraTarget.forward;
        cachedForward.y = 0f;
        cachedForward.Normalize();

        cachedRight = cameraTarget.right;
        cachedRight.y = 0f;
        cachedRight.Normalize();
    }

    private void FixedUpdate()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, (playerHeight * 0.5f) + 0.2f, groundLayer);

        if (pendingJump)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            pendingJump = false; 
        }

        HandleMovement();
    }
    public void HandleMovement()
    {
        Vector3 moveDirection;

        if (isPlaybackActive)
        {
            moveDirection = inputSource.GetWorldMoveDirection();
        }
        else
        {
            Vector2 inputVector = inputSource.GetMoveDirection();
            moveDirection = (cachedForward * inputVector.y) + (cachedRight * inputVector.x);
        }

        CurrentMoveDirection = moveDirection;

        rb.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed
        );

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
        }
    }

    private void GameInput_OnJumpAction()
    {
        pendingJump = true;
    }

    private void OnDestroy()
    {
        if (inputSource != null)
        {
            inputSource.OnJumpPressed -= GameInput_OnJumpAction;
        }
    }
}