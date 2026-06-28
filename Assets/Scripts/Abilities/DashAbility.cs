using UnityEngine;

public class DashAbility : BaseAbility
{
    [SerializeField] private float _dashForce = 1f;

    public override void Execute(Vector3 direction)
    {
        if (!IsReady) return;

        // Flatten so we never dash into the ground or sky.
        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;

        Owner.ApplyDeflectionForce(flatDirection * _dashForce);

        StartCooldown();
    }
}