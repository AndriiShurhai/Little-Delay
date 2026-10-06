using System;
using System.Collections;
using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.Scenes;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Project-owned copy of the toolkit's FadeScreenController sample (the sample lives in Samples~ and is
    /// documented as "copy into your own project"). Fades a CanvasGroup in when SceneLoadingService starts a load
    /// and out when it completes. Replaces the fade coroutines and GC.Collect() calls in the old SceneLoader.
    /// </summary>
    public sealed class ScreenFadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadeCanvas;
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private IDisposable _startedSub, _completedSub;
        private Coroutine _activeFade;

        public void Initialize(IEventBus eventBus)
        {
            _startedSub = eventBus.Subscribe<SceneLoadStartedEvent>(_ => FadeTo(1f));
            _completedSub = eventBus.Subscribe<SceneLoadCompletedEvent>(_ => FadeTo(0f));
        }

        private void OnDestroy()
        {
            _startedSub?.Dispose();
            _completedSub?.Dispose();
        }

        private void FadeTo(float value)
        {
            if (_fadeCanvas == null) return;
            if (_activeFade != null) StopCoroutine(_activeFade);
            _activeFade = StartCoroutine(FadeRoutine(value));
        }

        private IEnumerator FadeRoutine(float value)
        {
            _fadeCanvas.blocksRaycasts = true;
            float startAlpha = _fadeCanvas.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeCanvas.alpha = Mathf.Lerp(startAlpha, value, _fadeCurve.Evaluate(Mathf.Clamp01(elapsed / _fadeDuration)));
                yield return null;
            }

            _fadeCanvas.alpha = value;
            _fadeCanvas.blocksRaycasts = value > 0f;
            _activeFade = null;
        }
    }
}
