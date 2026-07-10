using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class ArenaController : MonoBehaviour
{
    [Header("Compacting Settings")]
    [SerializeField] private List<Transform> _wallsTransforms;
    [SerializeField] private float _inwardMoveDistance = 2f;
    [SerializeField] private float _inwardMoveDuration = 2f;

    private List<Vector3> _initialWallPositions = new List<Vector3>();
    private List<Vector3> _initialWallScales = new List<Vector3>();
    private List<Rigidbody> _wallRigidbodies = new List<Rigidbody>();

    private Coroutine _inwardMoveRoutine;

    private void Awake()
    {
        foreach (Transform t in _wallsTransforms)
        {
            _initialWallPositions.Add(t.position);
            _initialWallScales.Add(t.localScale);

            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                _wallRigidbodies.Add(rb);
            }
            else
            {
                Debug.LogWarning($"[ArenaController] Wall {t.name} is missing a Rigidbody! It will not push players cleanly.");
            }
        }
    }

    public void ResetArena()
    {
        if (_inwardMoveRoutine != null) StopCoroutine(_inwardMoveRoutine);

        for (int i = 0; i < _wallsTransforms.Count; i++)
        {
            _wallsTransforms[i].position = _initialWallPositions[i];
            _wallsTransforms[i].localScale = _initialWallScales[i];
        }
    }

    public void InwardMove(int roundNumber)
    {
        roundNumber--;
        List<Vector3> targetPositions = new List<Vector3>();
        List<Vector3> targetScales = new List<Vector3>();

        Debug.Log($"[ArenaController] InwardMove called for round {roundNumber}");

        for (int i = 0; i < _wallsTransforms.Count; i++)
        {
            Vector3 direction = _wallsTransforms[i].up;

            Vector3 targetPosition = _initialWallPositions[i] + direction * (_inwardMoveDistance * roundNumber);
            targetPosition.y = _initialWallPositions[i].y;
            targetPositions.Add(targetPosition);

            Vector3 targetScale = _initialWallScales[i];
            targetScale.x -= (_inwardMoveDistance * 2 * roundNumber);
            targetScale.x = Mathf.Max(1f, targetScale.x);
            targetScales.Add(targetScale);



            Debug.Log($"Target position for wall {i}: {targetPosition}");
        }

        if (_inwardMoveRoutine != null) StopCoroutine(_inwardMoveRoutine);
        _inwardMoveRoutine = StartCoroutine(MoveTo(targetPositions, targetScales));
    }

    private IEnumerator MoveTo(List<Vector3> targetPositions, List<Vector3> targetScales)
    {
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> startScales = new List<Vector3>();

        foreach (Transform t in _wallsTransforms)
        {
            startPositions.Add(t.position);
            startScales.Add(t.localScale);
        }

        float elapsedTime = 0f;

        while (elapsedTime < _inwardMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _inwardMoveDuration);

            for (int i = 0; i < _wallsTransforms.Count; i++)
            {
                Vector3 newPos = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                if (i < _wallRigidbodies.Count && _wallRigidbodies[i] != null)
                {
                    _wallRigidbodies[i].MovePosition(newPos);
                }
                else
                {
                    _wallsTransforms[i].position = newPos; // Fallback just in case
                }

                _wallsTransforms[i].localScale = Vector3.Lerp(startScales[i], targetScales[i], t);  
            }
            yield return null;
        }
    }
}