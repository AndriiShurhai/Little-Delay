using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class AIInputSource : MonoBehaviour, IInputSource
{
    public event Action OnJumpPressed;
    public event Action OnMobilityAbilityPressed;
    public event Action OnFirePressed;
    public bool IsPlayback => false;

    [SerializeField] private float _stopDistance = 1.5f;
    [SerializeField] private float _fireInterval = 0.6f;

    private Vector3 _lastKnownEnemyPosition;
    private bool _hasTarget;
    private float _fireCooldown;

    public void ReceiveReveal(Vector3 enemyWorldPosition)
    {
        _lastKnownEnemyPosition = enemyWorldPosition;
        _hasTarget = true;
    }

    public void ResetKnowledge()
    {
        _hasTarget = false;
        _fireCooldown = 0;
    }

    public void Tick()
    {
        _fireCooldown -= Time.fixedDeltaTime;

        if (!_hasTarget) return;

        if (_fireCooldown < 0f)
        {
            OnFirePressed?.Invoke();
            _fireCooldown = _fireInterval;
        }

    }

    public Vector2 GetMoveInput()
    {
        return WantsToMove() ? Vector2.up : Vector2.zero;
    }

    public Vector3 GetWorldMoveDirection()
    {
        if (!WantsToMove()) return Vector3.zero;

        Vector3 toTarget = _lastKnownEnemyPosition - transform.position;
        toTarget.y = 0f;

        return toTarget.normalized;
    }

    public Vector2 GetLookDelta()
    {
        return Vector2.zero;
    }

    public Vector3 GetAimPoint()
    {
        return _hasTarget ? _lastKnownEnemyPosition : transform.position + transform.forward * 10f;
    }

    private bool WantsToMove()
    {
        if (!_hasTarget) return false;

        Vector3 toTarget = _lastKnownEnemyPosition - transform.position;

        toTarget.y = 0f;

        return toTarget.magnitude > _stopDistance;
    }
}