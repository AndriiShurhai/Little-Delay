using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using UnityEngine;

public class ArenaController : MonoBehaviour
{
    [Header("Shrink Settings")]
    [SerializeField] private Transform _safeZone;
    [SerializeField] private Vector3 _initialScale = new Vector3(30f, 10f, 30f);
    [SerializeField] private float _shrinkPerRound = 4f;
    [SerializeField] private float _minSize = 8f;
    [SerializeField] private float _shrinkDuration = 2f;

    [Header("Hazard Settings")]
    [SerializeField] private float _outOfBoundsDamagePerSecond = 15f;
    [SerializeField] private LayerMask _fighterLayer;

    private Coroutine _shrinkRoutine;
    private BoxCollider _safeZoneCollider;

    private void Awake()
    {
        if (_safeZone != null)
        {
            _safeZone.localScale = _initialScale;
            _safeZoneCollider = _safeZone.GetComponent<BoxCollider>();
        }
    }

    private void Update()
    {
        if (_safeZoneCollider == null) return;

        Collider[] hits = Physics.OverlapSphere(_safeZone.position, GetSafeZoneRadius() * 2f, _fighterLayer);
        foreach (Collider hit in hits)
        {
            if (IsOutOfBounds(hit.transform.position) && hit.TryGetComponent<HealthComponent>(out var health))
            {
                health.TakeDamage(_outOfBoundsDamagePerSecond * Time.deltaTime);
            }
        }
    }

    private bool IsOutOfBounds(Vector3 worldPosition)
    {
        Vector3 local = _safeZone.InverseTransformPoint(worldPosition);
        Vector3 halfExtents = _safeZoneCollider.size * 0.5f;
        return Mathf.Abs(local.x) > halfExtents.x || Mathf.Abs(local.z) > halfExtents.z;
    }

    private float GetSafeZoneRadius()
    {
        return Mathf.Max(_safeZone.localScale.x, _safeZone.localScale.z);
    }

    public void ResetArena()
    {
        if (_shrinkRoutine != null) StopCoroutine(_shrinkRoutine);
        if (_safeZone != null) _safeZone.localScale = _initialScale;
    }

    public void ShrinkForRound(int roundNumber)
    {
        if (_safeZone == null) return;

        float reduction = _shrinkPerRound * (roundNumber - 1);
        float targetX = Mathf.Max(_minSize, _initialScale.x - reduction);
        float targetZ = Mathf.Max(_minSize, _initialScale.z - reduction);
        Vector3 target = new Vector3(targetX, _initialScale.y, targetZ);

        if (_shrinkRoutine != null) StopCoroutine(_shrinkRoutine);
        _shrinkRoutine = StartCoroutine(ShrinkTo(target));
    }

    private IEnumerator ShrinkTo(Vector3 target)
    {
        Vector3 start = _safeZone.localScale;

        float elapsed = 0f;

        while (elapsed < _shrinkDuration)
        {
            elapsed += Time.deltaTime;
            _safeZone.localScale = Vector3.Lerp(start, target, elapsed / _shrinkDuration);
            yield return null;
        }

        _safeZone.localScale = target;
    }
}