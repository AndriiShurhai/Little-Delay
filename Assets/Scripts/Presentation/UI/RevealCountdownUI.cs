using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using TMPro;
using UnityEngine;

namespace LittleDelay.Presentation
{
    public sealed class RevealCountdownUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private SimMatch _match;
        private SimMatchConfig _config;
        private bool _active;
        private readonly List<IDisposable> _subs = new();

        public void Initialize(IEventBus bus, SimMatch match, SimMatchConfig config)
        {
            _match = match;
            _config = config;
            if (_label != null) _label.text = "";

            _subs.Add(bus.Subscribe<RecordingStartedEvent>(_ => _active = true));
            _subs.Add(bus.Subscribe<RecordingCompletedEvent>(_ =>
            {
                _active = false;
                if (_label != null) _label.text = "";
            }));
        }

        private void OnDestroy()
        {
            foreach (IDisposable s in _subs) s.Dispose();
            _subs.Clear();
        }

        private void Update()
        {
            if (!_active || _label == null || _match?.CurrentRunner == null) return;

            float t = _match.CurrentRunner.World.ElapsedSeconds;
            for (int i = 0; i < _config.RevealsPerRecording; i++)
            {
                float start = i * _config.RevealIntervalSeconds;
                if (t < start)
                {
                    _label.text = $"Next reveal in {start - t:F1}s";
                    return;
                }
                if (t < start + _config.RevealDurationSeconds)
                {
                    _label.text = "REVEAL";
                    return;
                }
            }
            _label.text = "No more reveals";
        }
    }
}