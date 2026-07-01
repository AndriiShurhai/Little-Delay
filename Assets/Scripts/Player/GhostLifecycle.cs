using UnityEngine;

public class GhostLifecycle : MonoBehaviour
{
    private HealthComponent _health;
    private InputPlayback _playback;
    private PlayerController _controller;
    private Collider _collider;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _playback = GetComponent<InputPlayback>();
        _controller = GetComponent<PlayerController>();
        _collider = GetComponent<Collider>();

        _health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        _playback?.StopPlayback();

        _controller.enabled = false;

        if (_collider != null) _collider.enabled = false;

        Debug.Log("<color=red>A Ghost has been killed and its timeline severed.</color>");

        // 4. Trigger ragdoll or destruction
        // Destroy(gameObject, 2f); 
    }
}