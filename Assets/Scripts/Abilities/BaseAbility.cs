using UnityEngine;
using System;

public abstract class BaseAbility : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] protected string _displayName;
    [SerializeField] protected Sprite _icon;
    [SerializeField] protected KeyCode _displayKey; 

    [SerializeField] protected float _cooldownTime = 1f;

    protected float CurrentCooldown { get; private set; }
    protected ICharacterPhysics Owner { get; private set; }

    public event Action OnUsed;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public KeyCode DisplayKey => _displayKey;
    public float CooldownDuration => _cooldownTime;
    public float CooldownRemaining => CurrentCooldown;
    public float CooldownNormalized => _cooldownTime <= 0f ? 0f : Mathf.Clamp01(CurrentCooldown / _cooldownTime);
    public bool IsReady => CurrentCooldown <= 0f; // was protected, just widen this

    public void Initialize(ICharacterPhysics owner) => Owner = owner;

    public abstract void Execute(Vector3 direction);

    protected void StartCooldown()
    {
        CurrentCooldown = _cooldownTime;
        OnUsed?.Invoke();
    }

    protected virtual void Update()
    {
        if (CurrentCooldown > 0f)
            CurrentCooldown -= Time.deltaTime;
    }
}