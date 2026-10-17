using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDelay.Presentation
{
    public class ShowdownScreenUI : MonoBehaviour
    {
        [Header("UI Root")]
        [SerializeField] private GameObject _showdownRoot;

        [Header("Split Screen Panels")]
        [SerializeField] private RectTransform _player1Panel;
        [SerializeField] private RectTransform _player2Panel;
        [SerializeField] private float _offscreenOffset = 1200f;

        [Header("Fighter 1 Visuals & Stats")]
        [SerializeField] private Image _player1Portrait;
        [SerializeField] private TextMeshProUGUI _player1NameText;
        [SerializeField] private TextMeshProUGUI _player1StatsText;
        [SerializeField] private string _player1Name = "FIGHTER 1";

        [Header("Fighter 2 Visuals & Stats")]
        [SerializeField] private Image _player2Portrait;
        [SerializeField] private TextMeshProUGUI _player2NameText;
        [SerializeField] private TextMeshProUGUI _player2StatsText;
        [SerializeField] private string _player2Name = "FIGHTER 2";

        [Header("Center Elements")]
        [SerializeField] private RectTransform _vsText;
        [SerializeField] private CanvasGroup _statsCanvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float _slideDuration = 0.35f;
        [SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _popDuration = 0.25f;

        private IEventBus _eventBus;
        private SimMatchConfig _config;
        private readonly List<IDisposable> _subs = new();
        private Coroutine _animationRoutine;

        public void Initialize(IEventBus eventBus, SimMatchConfig config)
        {
            _eventBus = eventBus;
            _config = config;

            _subs.Add(_eventBus.Subscribe<ShowdownPreviewStartedEvent>(e => StartShowdown(e.Duration)));
            _subs.Add(_eventBus.Subscribe<MatchStartedEvent>(_ => HideShowdown()));

            if (_showdownRoot != null) _showdownRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void StartShowdown(float duration)
        {
            if (_showdownRoot == null) return;

            PopulateStats();

            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _showdownRoot.SetActive(true);
            _animationRoutine = StartCoroutine(ShowdownRoutine(duration));
        }

        private void PopulateStats()
        {
            // Set static names
            if (_player1NameText != null) _player1NameText.text = _player1Name;
            if (_player2NameText != null) _player2NameText.text = _player2Name;

            // Generate stat block from the simulation config
            string statBlock = $"HP: {_config.MaxHealth}\n" +
                               $"SPD: {_config.FighterMoveSpeed}\n" +
                               $"DMG: {_config.ProjectileDamage}";

            if (_player1StatsText != null) _player1StatsText.text = statBlock;
            if (_player2StatsText != null) _player2StatsText.text = statBlock;
        }

        private void HideShowdown()
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            if (_showdownRoot != null) _showdownRoot.SetActive(false);

            Debug.Log("Hide showdown");
        }

        private IEnumerator ShowdownRoutine(float totalDuration)
        {
            _player1Panel.anchoredPosition = new Vector2(-_offscreenOffset, 0);
            _player2Panel.anchoredPosition = new Vector2(_offscreenOffset, 0);
            _vsText.localScale = Vector3.zero;
            _statsCanvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _slideCurve.Evaluate(Mathf.Clamp01(elapsed / _slideDuration));

                _player1Panel.anchoredPosition = Vector2.LerpUnclamped(new Vector2(-_offscreenOffset, 0), Vector2.zero, t);
                _player2Panel.anchoredPosition = Vector2.LerpUnclamped(new Vector2(_offscreenOffset, 0), Vector2.zero, t);
                yield return null;
            }

            _player1Panel.anchoredPosition = Vector2.zero;
            _player2Panel.anchoredPosition = Vector2.zero;

            elapsed = 0f;
            while (elapsed < _popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _popDuration;

                float scale = t < 0.7f
                    ? Mathf.Lerp(0f, 1.3f, t / 0.7f)
                    : Mathf.Lerp(1.3f, 1f, (t - 0.7f) / 0.3f);

                _vsText.localScale = new Vector3(scale, scale, 1f);
                _statsCanvasGroup.alpha = Mathf.Clamp01(t * 1.5f);

                yield return null;
            }

            _vsText.localScale = Vector3.one;
            _statsCanvasGroup.alpha = 1f;

            float hideDelay = totalDuration - _slideDuration - _popDuration - 0.2f;
            if (hideDelay > 0)
            {
                yield return new WaitForSecondsRealtime(hideDelay);
            }

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                _statsCanvasGroup.alpha = 1f - (elapsed / 0.2f);

                float slideOut = Mathf.Lerp(0, 100f, elapsed / 0.2f);
                _player1Panel.anchoredPosition = new Vector2(-slideOut, 0);
                _player2Panel.anchoredPosition = new Vector2(slideOut, 0);

                yield return null;
            }

            HideShowdown();
        }
    }
}