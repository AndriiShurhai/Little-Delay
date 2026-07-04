using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class FighterHealthBarUI : MonoBehaviour
{
    [SerializeField] private int _fighterId;

    [SerializeField] private Image _fillImage;
    [SerializeField] private CanvasGroup _canvasGroup;

    private HealthComponent _boundHealth;
    private GhostLifecycle _boundLifecycle;

    private void Start()
    {
        SetVisible(false);

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnFighterSpawned += HandleFighterSpawned;
            BattleManager.Instance.OnMatchStarted += RestartHealth;
        }
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnFighterSpawned -= HandleFighterSpawned;
            BattleManager.Instance.OnMatchStarted -= RestartHealth;
        }
        Unbind();
    }

    private void HandleFighterSpawned(Fighter fighter)
    {
        if (fighter.ID != _fighterId) return;

        Unbind();

        _boundHealth = fighter.Health;
        _boundLifecycle = fighter.Lifecycle;

        _boundHealth.OnHealthChanged += HandleHealthChanged;
        _boundLifecycle.OnGhostDied += HandleGhostDied;

        HandleHealthChanged(_boundHealth.CurrentHealth, _boundHealth.MaxHealth);
        SetVisible(true);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (_fillImage != null)
        {
            _fillImage.fillAmount = max > 0f ? current / max : 0f;
        }
    }

    private void RestartHealth()
    {
        _fillImage.fillAmount = 1f;
    }

    private void HandleGhostDied(GameObject ghost)
    {
        if (_fillImage != null)
        {
            _fillImage.fillAmount = 0f;
        }
    }

    private void Unbind()
    {
        if (_boundHealth != null) _boundHealth.OnHealthChanged -= HandleHealthChanged;
        if (_boundLifecycle != null) _boundLifecycle.OnGhostDied -= HandleGhostDied;
    }
    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}