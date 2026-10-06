using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Abilities;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDelay.Presentation
{
    public enum AbilitySlot { Mobility, Attack }

    /// <summary>
    /// One ability icon with a cooldown fill. Pull model: it samples SimAbility.NormalizedRemaining every frame
    /// (the toolkit Cooldown's own progress), so there is no cooldown event wiring and no per-frame allocation.
    /// Rebinds whenever a new SimWorld appears.
    /// </summary>
    public class AbilitySlotUI : MonoBehaviour
    {
        [SerializeField] private AbilitySlot _slot;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _cooldownOverlay; // Image.Type.Filled
        [SerializeField] private TMP_Text _keybindText;

        [Header("Display (the simulation ability holds no art)")]
        [SerializeField] private Sprite _iconSprite;
        [SerializeField] private string _keybindLabel;

        private SimAbility _bound;
        private System.IDisposable _worldSub;

        public void Initialize(IEventBus bus)
        {
            if (_iconSprite != null) _icon.sprite = _iconSprite;
            if (_keybindText != null) _keybindText.text = _keybindLabel;
            _cooldownOverlay.fillAmount = 0f;

            _worldSub = bus.Subscribe<SimWorldCreatedEvent>(e =>
            {
                string id = _slot == AbilitySlot.Mobility ? SimDashAbility.Id : SimBlastAbility.Id;
                e.World.Player.TryGetAbility(id, out _bound);
            });
        }

        private void OnDestroy() => _worldSub?.Dispose();

        private void Update()
        {
            if (_bound != null) _cooldownOverlay.fillAmount = _bound.NormalizedRemaining;
        }
    }
}
