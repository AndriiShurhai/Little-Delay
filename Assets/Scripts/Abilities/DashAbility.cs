using UnityEngine;

public class DashAbility : BaseAbility
{
    [SerializeField] private float dashForce = 1f; // Replaced distance/duration with raw physics force

    public override void Execute(Vector3 direction)
    {
        if (currentCooldown <= 0)
        {
            Debug.Log($"Executing DashAbility in direction {direction}");
            // 1. Flatten the direction so we don't dash into the ground or sky
            Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;

            // 2. We trigger our custom physics engine!
            // This applies the force, pauses the timeline, prevents wall clipping, 
            // and naturally slows down using friction.

            Debug.Log($"Applying dash force: {flatDirection * dashForce}");
            PlayerController.Instance.ApplyDeflectionForce(flatDirection * dashForce);

            // 3. Reset cooldown
            currentCooldown = cooldownTime;
        }
    }
}