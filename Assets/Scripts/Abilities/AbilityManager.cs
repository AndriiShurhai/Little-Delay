using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public BaseAbility EquippedMobility => _equippedMobility;
    public BaseAbility EquippedAttack => _equippedAttack;

    [Header("Loadout Slots")]
    [SerializeField] private BaseAbility _equippedMobility;
    [SerializeField] private BaseAbility _equippedAttack;


    private void Awake()
    {
        ICharacterPhysics owner = GetComponentInParent<ICharacterPhysics>();

        if (owner == null)
        {
            Debug.LogError($"[AbilityManager] No ICharacterPhysics found on {gameObject.name} or its parents.", this);
            return;
        }

        if (_equippedMobility != null)
            _equippedMobility.Initialize(owner);

        if (_equippedAttack != null)
            _equippedAttack.Initialize(owner);
    }
    private void Start()
    {
    }

    public void TryUseMobility(Vector3 direction)
    {
        _equippedMobility?.Execute(direction);
    }

    public void TryUseFire(Vector3 direction)
    {
        _equippedAttack?.Execute(direction);
    }
}