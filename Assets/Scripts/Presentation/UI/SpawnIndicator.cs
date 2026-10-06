using System;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Events;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>Shows a marker on the enemy's spawn point for the length of the pre-recording countdown (SpawnRevealedEvent).</summary>
    public class SpawnIndicator : MonoBehaviour
    {
        [Tooltip("The actual visual object (e.g., a cylinder, sprite, or particle system)")]
        [SerializeField] private GameObject _indicatorVisual;

        private float _hideAt = -1f;
        private readonly System.Collections.Generic.List<IDisposable> _subs = new();

        public void Initialize(IEventBus bus)
        {
            _subs.Add(bus.Subscribe<SpawnRevealedEvent>(e => { if (e.Fighter == FighterSlot.Opponent) ShowIndicator(e.Position, e.Duration); }));
            _subs.Add(bus.Subscribe<RecordingStartedEvent>(_ => HideIndicator()));
            HideIndicator();
        }

        private void OnDestroy()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        public void ShowIndicator(Vector3 position, float duration)
        {
            transform.position = position;
            _hideAt = Time.unscaledTime + duration;
            if (_indicatorVisual != null) _indicatorVisual.SetActive(true);
        }

        public void HideIndicator()
        {
            _hideAt = -1f;
            if (_indicatorVisual != null) _indicatorVisual.SetActive(false);
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.unscaledTime >= _hideAt) HideIndicator();
        }
    }
}
