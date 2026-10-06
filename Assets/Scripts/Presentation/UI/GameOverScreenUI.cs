using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Result screen. Shows on MatchEndedEvent and publishes commands (rematch / quit) instead of calling the
    /// match flow or a scene loader directly - the composition root owns what those commands do.
    /// Public Quit() is kept so an inspector-wired Button.onClick still works; Rematch() is new.
    /// </summary>
    public class GameOverScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject _gameOverScreen;
        [SerializeField] private TMPro.TextMeshProUGUI _winnerText;
        [Tooltip("Optional. If set, the cursor is released while the screen is up and re-locked afterwards.")]
        [SerializeField] private CursorManager _cursorManager;
        [Tooltip("Optional. Hooked up automatically; leave empty if you wire Button.onClick in the inspector.")]
        [SerializeField] private Button _rematchButton;
        [SerializeField] private Button _quitButton;

        private IEventBus _bus;
        private readonly System.Collections.Generic.List<System.IDisposable> _subs = new();

        public void Initialize(IEventBus bus)
        {
            _bus = bus;
            _subs.Add(bus.Subscribe<MatchEndedEvent>(e => ShowGameOverScreen(e.Outcome)));
            _subs.Add(bus.Subscribe<ShowdownPreviewStartedEvent>(_ => HideGameOverScreen()));

            if (_rematchButton != null) _rematchButton.onClick.AddListener(Rematch);
            if (_quitButton != null) _quitButton.onClick.AddListener(Quit);

            HideGameOverScreen();
        }

        private void OnDestroy()
        {
            foreach (System.IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
            if (_rematchButton != null) _rematchButton.onClick.RemoveListener(Rematch);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(Quit);
        }

        public void HideGameOverScreen()
        {
            _gameOverScreen.SetActive(false);
            if (_cursorManager != null) _cursorManager.LockCursor();
        }

        public void ShowGameOverScreen(RoundOutcome outcome)
        {
            if (_winnerText != null)
            {
                _winnerText.text = outcome switch
                {
                    RoundOutcome.PlayerWin => "FIGHTER 1 WINS!",
                    RoundOutcome.OpponentWin => "FIGHTER 2 WINS!",
                    _ => "DRAW!"
                };
            }

            _gameOverScreen.SetActive(true);
            if (_cursorManager != null) _cursorManager.UnlockCursor();
        }

        public void Rematch() => _bus?.Publish(new RematchRequestedEvent());

        public void Quit() => _bus?.Publish(new QuitToMenuRequestedEvent());
    }
}
