using UnityEngine;

public abstract class BaseAbility : MonoBehaviour
{
    [SerializeField] protected float _cooldownTime = 1f;

    protected float CurrentCooldown { get; private set; }
    protected ICharacterPhysics Owner { get; private set; }

    
    public void Initialize(ICharacterPhysics owner)
    {
        Owner = owner;
    }

    public abstract void Execute(Vector3 direction);    

    protected void StartCooldown()
    {
        CurrentCooldown = _cooldownTime;
    }

    protected bool IsReady => CurrentCooldown <= 0f;

    protected virtual void Update()
    {
        if (CurrentCooldown > 0f)
        {
            CurrentCooldown -= Time.deltaTime;
        }
    }
}