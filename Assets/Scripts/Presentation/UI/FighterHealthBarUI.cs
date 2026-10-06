using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Health bar for one fighter slot. Reads the starting value from the world when it appears and then follows
    /// HealthChangedEvent. No references to fighter objects, ghosts or health components.
    /// </summary>
    public class FighterHealthBarUI : MonoBehaviour
    {
        [Tooltip("0 = Player, 1 = Opponent.")]
        [SerializeField] private int _fighterId;

        [SerializeField] private Image _fillImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        private FighterSlot Slot => (FighterSlot)_fighterId;
        private readonly System.Collections.Generic.List<System.IDisposable> _subs = new();

        public void Initialize(IEventBus bus)
        {
            SetVisible(false);

            _subs.Add(bus.Subscribe<SimWorldCreatedEvent>(e =>
            {
                var fighter = e.World.GetFighter(Slot);
                SetFill(fighter.CurrentHealth, fighter.MaxHealth);
                SetVisible(true);
            }));
            _subs.Add(bus.Subscribe<HealthChangedEvent>(e => { if (e.Fighter == Slot) SetFill(e.CurrentHealth, e.MaxHealth); }));
        }

        private void OnDestroy()
        {
            foreach (System.IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void SetFill(float current, float max)
        {
            if (_fillImage != null) _fillImage.fillAmount = max > 0f ? current / max : 0f;
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
