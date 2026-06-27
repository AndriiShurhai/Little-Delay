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

    private bool forcePending = false;
    private bool justRecovered = false;

    private AbilityManager abilityManager;
    private bool pendingAbilityUse = false;
    public bool IsDeflected { get; private set; }

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
        abilityManager = GetComponent<AbilityManager>();

        GameInput.Instance.OnUseAbilityAction += Instance_OnUseAbilityAction;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; 

        startPosition = transform.position;
        startRotation = transform.eulerAngles;
    }

    private void Instance_OnUseAbilityAction()
    {
        pendingAbilityUse = true;
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
        Vector3 rayStart = transform.position + (Vector3.down * (playerHeight * 0.4f));

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 0.4f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
        justRecovered = false; 

        if (IsDeflected)
        {
            if (forcePending)
            {
                forcePending = false;
            }
            else
            {
                Vector2 horizontalVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);

                // --- THE FIX ---
                // We added:  && rb.linearVelocity.y <= 1f
                // This means: "Only recover if we are touching the ground, not sliding fast, 
                // AND we are not currently rocketing upwards into the sky."
                if (isGrounded && horizontalVelocity.magnitude <= moveSpeed + 1f && rb.linearVelocity.y <= 1f)
                {
                    IsDeflected = false;
                    justRecovered = true;

                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
            }
        }

        if (pendingJump)
        {
            if (isGrounded && !IsDeflected)
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
        Vector3 moveDirection = Vector3.zero;

        if ((IsDeflected || justRecovered))
        {
            CurrentMoveDirection = Vector3.zero;
            return; // Yield to physics. Do NOT pull a frame from the timeline.
        }

        bool tryAbilityUse = false;

        if (isPlaybackActive)
        {
            if (inputSource is InputPlayback playback)
            {
                playback.AdvanceTimeline();
            }

            moveDirection = inputSource.GetWorldMoveDirection();
        }
        else
        {
            Vector2 inputVector = inputSource.GetMoveDirection();
            moveDirection = (cachedForward * inputVector.y) + (cachedRight * inputVector.x);

            if (pendingAbilityUse)
            {
                tryAbilityUse = true;
                pendingAbilityUse = false;
            }
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

        if (tryAbilityUse)
        {
            Debug.Log("Attempting to use ability");
            abilityManager.TryUseMobility(moveDirection == Vector3.zero ? transform.forward : moveDirection);
        }
    }

    public void ApplyDeflectionForce(Vector3 force)
    {

        Debug.Assert(!forcePending,
            "[PHYSICS FATAL] A new deflection force was applied before the previous one finished pending!");

        IsDeflected = true;
        forcePending = true;
        pendingJump = false; // Cancel any jump queue so they don't double-jump on landing

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.VelocityChange);
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

    private void OnDrawGizmos()
    {
        // Draw the Raycast start position and length
        Vector3 rayStart = transform.position + (Vector3.down * (playerHeight * 0.4f));
        float rayLength = 0.4f;

        Gizmos.color = isGrounded ? Color.green : Color.red;

        // Draw a little sphere at the start point
        Gizmos.DrawWireSphere(rayStart, 0.1f);

        // Draw the actual ray pointing down
        Gizmos.DrawLine(rayStart, rayStart + (Vector3.down * rayLength));
    }
}