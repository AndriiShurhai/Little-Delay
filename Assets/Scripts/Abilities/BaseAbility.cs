using UnityEngine;

public abstract class BaseAbility : MonoBehaviour
{
    [SerializeField] protected float cooldownTime = 1f;
    protected float currentCooldown;

    // We MUST pass the direction in, so the Ghost can use its recorded direction!
    public virtual void Execute(Vector3 direction) { }

    protected virtual void Update()
    {
        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
    }
}