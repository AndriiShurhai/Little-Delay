using System;
using UnityEngine;
using UnityEngine.Serialization;

public class AIInputSource : MonoBehaviour, IInputSource
{
    public event Action OnJumpPressed;
    public event Action OnMobilityAbilityPressed;
    public event Action OnFirePressed;

    public bool IsPlayback => false;

    [Header("Engagement Ranges")]
    [Tooltip("Beyond this range the bot just closes the distance instead of strafing")]
    [SerializeField] private float _engageRange = 12f;
    [Tooltip("Ideal distance to hover around while strafing/fighting")]
    [SerializeField] private float _preferredRange = 6f;
    [FormerlySerializedAs("_stopDistance")]
    [Tooltip("Below this range the bot backs off instead of walking further in")]
    [SerializeField] private float _tooCloseRange = 2.5f;
    [Tooltip("Won't fire at a target further than this, even if it has one")]
    [SerializeField] private float _maxFireRange = 14f;

    [Header("Firing")]
    [SerializeField] private float _fireInterval = 0.6f;
    [SerializeField] private float _fireJitter = 0.15f;

    [Header("Strafing")]
    [SerializeField] private float _strafeSwitchInterval = 1.2f;
    [SerializeField] private float _strafeSwitchJitter = 0.5f;

    [Header("Dashing")]
    [Tooltip("The dash ability no-ops on cooldonw, so this can just be an opportunistic timer")]
    [SerializeField] private float _dashDecisionInterval = 1.5f;
    [SerializeField] private float _dashDecisionJitter = 0.5f;

    [Header("Prediction")]
    [Tooltip("How far past the last reveal we extrapolate the enemy's position, based on estimated velocity between the last two reveals. Keeps a stale reveal from being trusted forever")]
    [SerializeField] private float _predictionHorizon = 1.5f;

    [Header("Turning")]
    [Tooltip("Degrees/sec before the CameraTargetController's own sensitivity is applied. Raise this if the bot turns too slowly to face its target")]
    [SerializeField] private float _turnSpeed = 360f;

    [Header("Misc")]
    [Tooltip("Small chance per second to throw in a jump while it has a target, purely so it isn't perfectly predictable. Set to 0 to disable")]
    [SerializeField] private float _randomJumpChancePerSecond = 0.15f;

    private bool _hasTarget;

    private Vector3 _lastRevealPosition;
    private Vector3 _previousRevealPosition;
    private float _lastRevealTime;
    private float _previousRevealTime;
    private Vector3 _predictedPosition;

    private float _fireCooldown;
    private float _strafeSign = 1f;
    private float _nextStrafeSwitchTime;
    private float _nextDashDecisionTime;


    public void ReceiveReveal(Vector3 enemyWorldPosition)
    {
        _previousRevealPosition = _hasTarget ? _lastRevealPosition : enemyWorldPosition;
        _previousRevealTime = _hasTarget ? _lastRevealTime : Time.time;

        _lastRevealPosition = enemyWorldPosition;
        _lastRevealTime = Time.time;

        _hasTarget = true;
    }

    public void ResetKnowledge()
    {
        _hasTarget = false;
        _fireCooldown = 0f;

        _lastRevealPosition = transform.position;
        _previousRevealPosition = transform.position;
        _lastRevealTime = Time.time;
        _previousRevealTime = Time.time;
        _predictedPosition = transform.position;

        _nextStrafeSwitchTime = Time.time;
        _nextDashDecisionTime = Time.time;
    }

    public void Tick()
    {
        _fireCooldown -= Time.fixedDeltaTime;

        if (!_hasTarget) return;

        UpdatePredictedPosition();
        UpdateStrafeDirection();
        UpdateDashDecision();
        UpdateFiring();
        UpdateRandomJump();
    }

    private void UpdatePredictedPosition()
    {
        float revealDelta = _lastRevealTime - _previousRevealTime;
        Vector3 estimatedVelocity = Vector3.zero;

        if (revealDelta > 0.01f)
        {
            estimatedVelocity = (_lastRevealPosition - _previousRevealPosition) / revealDelta;
        }

        float timeSinceReveal = Mathf.Min(Time.time - _lastRevealTime, _predictionHorizon);
        _predictedPosition = _lastRevealPosition + estimatedVelocity * timeSinceReveal;
    }

    private void UpdateStrafeDirection()
    {
        if (Time.time < _nextStrafeSwitchTime) return;

        _strafeSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;

        _nextStrafeSwitchTime = Time.time + _strafeSwitchInterval 
            + UnityEngine.Random.Range(-_strafeSwitchJitter, _strafeSwitchJitter);
    }

    private void UpdateDashDecision()
    {
        if (Time.time < _nextDashDecisionTime) return;

        _nextDashDecisionTime = Time.time + _dashDecisionInterval
            + UnityEngine.Random.Range(-_dashDecisionJitter, _dashDecisionJitter);

        float distance = FlatDistance(transform.position, _predictedPosition);

        if (distance > _engageRange || distance < _tooCloseRange)
        {
            OnMobilityAbilityPressed?.Invoke();
        }
    }

    private void UpdateFiring()
    {
        if (_fireCooldown > 0f) return;
        float distance = FlatDistance(transform.position, _predictedPosition);
        if (distance > _maxFireRange) return;

        OnFirePressed?.Invoke();
        _fireCooldown = _fireInterval + UnityEngine.Random.Range(-_fireJitter, _fireJitter);
    }

    private void UpdateRandomJump()
    {
        if (_randomJumpChancePerSecond <= 0f) return;
        float jumpProbability = _randomJumpChancePerSecond * Time.fixedDeltaTime;
        if (UnityEngine.Random.value < jumpProbability)
        {
            OnJumpPressed?.Invoke();
        } 
    }

    public Vector2 GetMoveInput()
    {
        return GetWorldMoveDirection() == Vector3.zero ? Vector2.zero : Vector2.up;
    }

    public Vector3 GetWorldMoveDirection()
    {
        if (!_hasTarget) return Vector3.zero;
        
        Vector3 toTarget = _predictedPosition - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance < 0.05f) return Vector3.zero;

        Vector3 towardDir = toTarget.normalized;
        Vector3 perpDir = new Vector3(-towardDir.z, 0f, towardDir.x) * _strafeSign;
        
        if (distance > _engageRange)
        {
            return towardDir;
        }

        if (distance < _tooCloseRange)
        {
            // Too close : back off, with a little strafe mixed in so it's not a dead-straight retreat
            return (towardDir * -0.8f + perpDir * 0.25f).normalized;
        }

        // Comfortable combat range : circle the target while drifting toward the preferred distance.
        float rangeError = distance - _preferredRange;
        float approachWeight = Mathf.Clamp(rangeError / _engageRange, -1f, 1f);
        return (perpDir + towardDir * approachWeight).normalized;
    }

    public Vector2 GetLookDelta()
    {
        if (!_hasTarget) return Vector2.zero;

        Vector3 toTarget = _predictedPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f) return Vector2.zero;

        float desiredYaw = Quaternion.LookRotation(toTarget).eulerAngles.y;
        float currentYaw = transform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(currentYaw, desiredYaw);

        float maxStep = _turnSpeed * Time.deltaTime;
        return new Vector2(Mathf.Clamp(delta, -maxStep, maxStep), 0f);
    }

    public Vector3 GetAimPoint()
    {
        return _hasTarget ? _predictedPosition : transform.position + transform.forward * 10f;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}