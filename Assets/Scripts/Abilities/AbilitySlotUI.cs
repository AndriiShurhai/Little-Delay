using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AbilitySlot { Mobility, Attack }

public class AbilitySlotUI : MonoBehaviour
{
    [SerializeField] private AbilityManager _abilityManager;
    [SerializeField] private AbilitySlot _slot;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _cooldownOverlay; // Image.Type.Filled
    [SerializeField] private TMP_Text _keybindText;

    private BaseAbility _bound;

    private void Start()
    {
        _bound = _slot == AbilitySlot.Mobility
            ? _abilityManager.EquippedMobility
            : _abilityManager.EquippedAttack;

        if (_bound == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _icon.sprite = _bound.Icon;
        _keybindText.text = _bound.DisplayKey.ToString();
        _bound.OnUsed += HandleUsed;
    }

    private void OnDestroy()
    {
        if (_bound != null) _bound.OnUsed -= HandleUsed;
    }

    private void Update()
    {
        if (_bound == null) return;
        _cooldownOverlay.fillAmount = _bound.CooldownNormalized;
    }

    private void HandleUsed()
    {
        // punch/flash animation trigger, if you want one
    }
}   