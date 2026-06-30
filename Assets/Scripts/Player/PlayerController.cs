using System;
using System.Linq.Expressions;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour, ICharacterPhysics
{
    public static PlayerController Instance { get; private set; }

    public bool IsDeflected { get; private set; }
    public bool IsPlayingBack { get; private set; }
    public Vector3 CurrentMoveDirection { get; private set; }
    public Vector3 StartPosition => _startPosition;

    Transform ICharacterPhysics.Transform => transform;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _dashFriction = 8f;

    [Header("Ground Detection")]
    [SerializeField] private float _playerHeight = 2f;
    [SerializeField] private LayerMask _groundLayer;

    private Vector3 _startPosition;

    private Rigidbody _rigidbody;
    private IInputSource _inputSource;

    private bool _isGrounded;
    private bool _pendingJump;
    private bool _pendingAbilityUse;
    private bool _pendingFireUse;
    private bool _forcePending;
    private bool _justRecovered;

    private Vector3 _dashVelocity;

    private AbilityManager _abilityManager;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _abilityManager = GetComponent<AbilityManager>();

        _rigidbody.freezeRotation = true;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        _startPosition = transform.position;
    }

    private void OnDestroy()
    {
        if (_inputSource != null)
        {
            _inputSource.OnJumpPressed -= OnJumpInput;
            _inputSource.OnMobilityAbilityPressed -= OnUseAbilityInput;
        }
    }

    public void SetInputSource(IInputSource inputSource)
    {
        if (_inputSource != null)
        {
            _inputSource.OnJumpPressed -= OnJumpInput;
            _inputSource.OnMobilityAbilityPressed -= OnUseAbilityInput;
        }
        _inputSource = inputSource;
        _inputSource.OnJumpPressed += OnJumpInput;
        _inputSource.OnMobilityAbilityPressed += OnUseAbilityInput;
        _inputSource.OnFirePressed += OnFireInput;

        if (_inputSource is PlayerInputSource)
        {
            IsPlayingBack = false;
        }
        else if (_inputSource is InputPlayback)
        {
            IsPlayingBack = true;
        }   
    }

    public IInputSource InputSource => _inputSource;

    private void OnJumpInput()
    {
        _pendingJump = true;
    }

    private void OnUseAbilityInput()
    {
        _pendingAbilityUse = true;
    }

    private void OnFireInput()
    {
        _pendingFireUse = true;
    }

    private void FixedUpdate()
    {
        UpdateGroundState();
        ResolveDeflectionRecovery();
        ResolveJump();
        HandleMovement();
    }

    private void UpdateGroundState()
    {
        Vector3 rayStart = transform.position + Vector3.down * (_playerHeight * 0.4f);
        _isGrounded = Physics.Raycast(rayStart, Vector3.down, 0.4f, _groundLayer, QueryTriggerInteraction.Ignore);
        _justRecovered = false;
    }

    private void ResolveDeflectionRecovery()
    {
        if (!IsDeflected) return;
        if (_forcePending)
        {
            _forcePending = false;
            return;
        }

        Vector2 horizontalVelocity = new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z);
        bool slowedDown = horizontalVelocity.magnitude <= _moveSpeed + 1f;
        bool notRocketingUp = _rigidbody.linearVelocity.y <= 1f;

        if (_isGrounded && slowedDown && notRocketingUp)
        {
            IsDeflected = false;
            _justRecovered = true;
            _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
        }
    }

    private void ResolveJump()
    {
        if (!_pendingJump) return;
        _pendingJump = false;

        if (_isGrounded && !IsDeflected)
        {
            _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    private void HandleMovement()
    {
        if (IsDeflected || _justRecovered)
        {
            CurrentMoveDirection = Vector3.zero;
            return; 
        }

        _inputSource.Tick();

        _dashVelocity = Vector3.MoveTowards(_dashVelocity, Vector3.zero, _dashFriction * Time.fixedDeltaTime);

        Vector3 moveDirection = _inputSource.GetWorldMoveDirection();
        CurrentMoveDirection = moveDirection;

        _rigidbody.linearVelocity = new Vector3(
            moveDirection.x * _moveSpeed + _dashVelocity.x,
            _rigidbody.linearVelocity.y,
            moveDirection.z * _moveSpeed + _dashVelocity.z
        );

        //_rigidbody.linearVelocity = new Vector3(moveDirection.x * _moveSpeed, _rigidbody.linearVelocity.y, moveDirection.z * _moveSpeed);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, 10f * Time.fixedDeltaTime));
        }

        if (_pendingAbilityUse)
        {
            _pendingAbilityUse = false;
            Vector3 abilityDirection = moveDirection == Vector3.zero ? transform.forward : moveDirection;
            _abilityManager?.TryUseMobility(abilityDirection);
        }

        if (_pendingFireUse)
        {
            _pendingFireUse = false;
            _abilityManager?.TryUseFire(_inputSource.GetAimPoint());
        }
    }

    public void ApplyDeflectionForce(Vector3 force)
    {
        Debug.Assert(!_forcePending,
            "[PHYSICS FATAL] A new deflection force was applied before the previous one finished pending!");

        IsDeflected = true;
        _forcePending = true;
        _pendingJump = false;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.AddForce(force, ForceMode.VelocityChange);
    }

    public void ApplyDash(Vector3 force)
    {
        _dashVelocity += force;
    }

    private void OnDrawGizmos()
    {
        Vector3 rayStart = transform.position + Vector3.down * (_playerHeight * 0.4f);
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(rayStart, 0.1f);
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 0.4f);
    }

}