using System;
using System.Collections;
using UnityEngine;

public class RevealVisualizer : MonoBehaviour
{
    public enum RevealVisualState { Hidden, Revealed, Fading }

    [Header("References")]
    [SerializeField] private RevealSystem _revealSystem;

    [Header("Settings")]
    [SerializeField] private float _revealDuration = 1.5f;
    [SerializeField] private float _fadeDuration = 1f;
    [SerializeField] private Color _lineColor = new Color(0f, 0.8f, 1f); // Default to a neon blue
    [SerializeField] private float _lineWidth = 2f;

    private RevealVisualState _currentState = RevealVisualState.Hidden;
    private LineRenderer _recordedLine;
    private static Material _lineMaterial;
    private Transform _enemy;

    private Coroutine _activeSequence;

    private void Start()
    {
        if (_revealSystem != null)
            _revealSystem.OnEnemyPositionRevealed += HandleEnemyPositionRevealed;

        _recordedLine = CreateLine("Enemy Trail", _lineColor, _lineWidth);
    }

    private void OnDestroy()
    {
        if (_revealSystem != null)
            _revealSystem.OnEnemyPositionRevealed -= HandleEnemyPositionRevealed;
    }

    private void HandleEnemyPositionRevealed(Transform aiTransform)
    {
        _enemy = aiTransform;

        if (_currentState == RevealVisualState.Hidden)
        {
            _recordedLine.positionCount = 0;
            if (_activeSequence != null) StopCoroutine(_activeSequence);
            _activeSequence = StartCoroutine(RevealSequence());
        }
    }

    public void ForceStopVisuals()
    {
        if (_activeSequence != null)
        {
            StopCoroutine(_activeSequence);
            _activeSequence = null;
        }
        Reset();
    }

    private IEnumerator RevealSequence()
    {
        _currentState = RevealVisualState.Revealed;
        SetLineAlpha(1f);

        yield return new WaitForSeconds(_revealDuration);

        _currentState = RevealVisualState.Fading;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
            SetLineAlpha(currentAlpha);
            yield return null;
        }

        Reset();
    }

    private void SetLineAlpha(float masterAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(_lineColor, 0f), new GradientColorKey(_lineColor, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(masterAlpha, 1f)
            }
        );
        _recordedLine.colorGradient = gradient;
    }

    private void Reset()
    {
        _currentState = RevealVisualState.Hidden;
        _recordedLine.positionCount = 0;
        _enemy = null;
    }

    private void FixedUpdate()
    {
        if (_currentState == RevealVisualState.Revealed && _enemy != null)
        {
            AppendPoint(_recordedLine, _enemy.position);
        }
        else
        {
            Debug.Log($"RevealVisualizer: Current state is {_currentState}, no enemy to track.");
        }
    }

    private LineRenderer CreateLine(string objectName, Color color, float width)
    {
        if (_lineMaterial == null)
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));

        var go = new GameObject(objectName);
        go.transform.SetParent(transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.material = _lineMaterial;

        lr.numCapVertices = 5;
        lr.numCornerVertices = 5;

        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),     // Tail is width 0
            new Keyframe(1f, width)   // Head is full width
        );

        lr.positionCount = 0;
        return lr;
    }

    private static void AppendPoint(LineRenderer lr, Vector3 position)
    {
        Vector3 targetPos = position + Vector3.up * 0.1f;

        if (lr.positionCount > 0)
        {
            float distance = Vector3.Distance(lr.GetPosition(lr.positionCount - 1), targetPos);
            if (distance < 0.05f) return;
        }

        lr.positionCount++;
        lr.SetPosition(lr.positionCount - 1, targetPos);
    }
}