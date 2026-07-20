using System.Collections;
using UnityEngine;

public class SpawnIndicator : MonoBehaviour
{
    [Tooltip("The actual visual object (e.g., a cylinder, sprite, or particle system)")]
    [SerializeField] private GameObject _indicatorVisual;

    private Coroutine _activeRoutine;

    private void Awake()
    {
        if (_indicatorVisual != null)
        {
            _indicatorVisual.SetActive(false);
        }
    }

    public void ShowIndicator(Vector3 position, float duration)
    {
        // Move the indicator to the exact spawn location
        transform.position = position;

        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
        }

        _activeRoutine = StartCoroutine(IndicatorRoutine(duration));
    }

    public void HideIndicator()
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
        }

        if (_indicatorVisual != null)
        {
            _indicatorVisual.SetActive(false);
        }
    }

    private IEnumerator IndicatorRoutine(float duration)
    {
        if (_indicatorVisual != null)
        {
            _indicatorVisual.SetActive(true);
        }

        yield return new WaitForSeconds(duration);

        if (_indicatorVisual != null)
        {
            _indicatorVisual.SetActive(false);
        }
    }
}