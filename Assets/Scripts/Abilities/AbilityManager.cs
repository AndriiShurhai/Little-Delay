using UnityEngine;

// I renamed this to PlayerLoadout to match the code you just put in PlayerController.cs!
public class AbilityManager : MonoBehaviour
{
    [Header("Loadout Slots")]
    // Strict slots make it easy for the UI to read and the opponent to predict
    [SerializeField] private BaseAbility equippedMobility;

    // We will add equippedWeapon and equippedTactical here later!

    // The PlayerController passes the exact direction (either Live Mouse or Recorded Vector)
    public void TryUseMobility(Vector3 direction)
    {
        if (equippedMobility != null)
        {
            Debug.Log($"Using mobility ability: {equippedMobility.name} in direction {direction}");
            equippedMobility.Execute(direction);
        }
    }
}