using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("Loadout Slots")]
    [SerializeField] private BaseAbility _equippedMobility;

    private void Start()
    {
        ICharacterPhysics owner = GetComponentInParent<ICharacterPhysics>();

        if (owner == null)
        {
            Debug.LogError($"[AbilityManager] No ICharacterPhysics found on {gameObject.name} or its parents.", this);
            return;
        }

        if (_equippedMobility != null)
            _equippedMobility.Initialize(owner);
    }

    public void TryUseMobility(Vector3 direction)
    {
        _equippedMobility?.Execute(direction);
    }
}