using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Events;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Draws the opponent's neon trail during a reveal window. Each simulation tick inside the window publishes a
    /// RevealTrailPointEvent; this just appends it to a LineRenderer, then fades the line out once the window ends.
    /// The window length itself (GDD: 2s) belongs to the simulation, not to this class.
    /// </summary>
    public class RevealVisualizer : MonoBehaviour
    {
        private enum VisualState { Hidden, Revealed, Fading }

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 1f;
        [SerializeField] private Color _lineColor = new Color(0f, 0.8f, 1f);
        [SerializeField] private float _lineWidth = 2f;

        private static Material _lineMaterial;

        private LineRenderer _line;
        private VisualState _state = VisualState.Hidden;
        private float _fadeElapsed;
        private readonly Gradient _gradient = new();
        private readonly GradientColorKey[] _colorKeys = new GradientColorKey[2];
        private readonly GradientAlphaKey[] _alphaKeys = new GradientAlphaKey[2];
        private readonly List<IDisposable> _subs = new();

        public void Initialize(IEventBus bus)
        {
            _line = CreateLine("Enemy Trail");

            _subs.Add(bus.Subscribe<RevealWindowStartedEvent>(e => { if (e.RevealedFighter == FighterSlot.Opponent) Begin(); }));
            _subs.Add(bus.Subscribe<RevealTrailPointEvent>(e => { if (e.RevealedFighter == FighterSlot.Opponent && _state == VisualState.Revealed) Append(e.Position); }));
            _subs.Add(bus.Subscribe<RevealWindowEndedEvent>(e => { if (e.RevealedFighter == FighterSlot.Opponent && _state == VisualState.Revealed) { _state = VisualState.Fading; _fadeElapsed = 0f; } }));
            _subs.Add(bus.Subscribe<SimWorldRetiredEvent>(_ => ResetLine()));
        }

        private void OnDestroy()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void Update()
        {
            if (_state != VisualState.Fading) return;

            _fadeElapsed += Time.deltaTime;
            if (_fadeElapsed >= _fadeDuration)
            {
                ResetLine();
                return;
            }

            SetAlpha(1f - _fadeElapsed / _fadeDuration);
        }

        private void Begin()
        {
            _line.positionCount = 0;
            _state = VisualState.Revealed;
            SetAlpha(1f);
        }

        private void ResetLine()
        {
            _state = VisualState.Hidden;
            if (_line != null) _line.positionCount = 0;
        }

        private void SetAlpha(float masterAlpha)
        {
            _colorKeys[0] = new GradientColorKey(_lineColor, 0f);
            _colorKeys[1] = new GradientColorKey(_lineColor, 1f);
            _alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            _alphaKeys[1] = new GradientAlphaKey(masterAlpha, 1f);
            _gradient.SetKeys(_colorKeys, _alphaKeys);
            _line.colorGradient = _gradient;
        }

        private void Append(Vector3 simPosition)
        {
            Vector3 target = simPosition + Vector3.up * 0.1f;

            if (_line.positionCount > 0 && Vector3.Distance(_line.GetPosition(_line.positionCount - 1), target) < 0.05f) return;

            _line.positionCount++;
            _line.SetPosition(_line.positionCount - 1, target);
        }

        private LineRenderer CreateLine(string objectName)
        {
            if (_lineMaterial == null) _lineMaterial = new Material(Shader.Find("Sprites/Default"));

            var go = new GameObject(objectName);
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.material = _lineMaterial;
            lr.numCapVertices = 5;
            lr.numCornerVertices = 5;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, _lineWidth));
            lr.positionCount = 0;
            return lr;
        }
    }
}
