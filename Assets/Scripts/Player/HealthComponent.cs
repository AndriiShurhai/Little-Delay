using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;

    /// <summary>Fired when health changes. Args: currentHealth, maxHealth.</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Fired once when health reaches zero. Subscriber (RoundManager) handles round resolution.</summary>
    public event Action OnDeath;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => _maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
    }

    /// <summary>Call at the start of each round to restore the fighter to full health.</summary>
    public void ResetHealth()
    {
        IsDead = false;
        CurrentHealth = _maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
    }
}