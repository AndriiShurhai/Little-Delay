using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using TMPro;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Match HUD: countdown numbers, the recording timer, round pips and centre-screen announcements.
    /// Everything discrete arrives as an event; the one continuous value (recording time left) is pulled from the
    /// active SimRoundRunner each frame, so there is no per-tick "timer tick" event spam.
    /// Same class/file name as before so existing scene references survive; the old per-event UnityEvent
    /// wiring to BattleManager is gone.
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _roundsCountPanel;
        [SerializeField] private GameObject _roundUIPrefab;
        [SerializeField] private TextMeshProUGUI _countdownDisplay;
        [SerializeField] private TextMeshProUGUI _announcementDisplay;

        [Tooltip("Used for the active recording timer.")]
        [SerializeField] private TextMeshProUGUI _timerDisplay;

        [SerializeField] private float _announcementSeconds = 1.5f;

        private SimMatch _match;
        private bool _showRecordingTimer;
        private float _clearAnnouncementAt = -1f;
        private bool _resultShown;
        private readonly List<IDisposable> _subs = new();

        public void Initialize(IEventBus bus, SimMatch match)
        {
            _match = match;

            _subs.Add(bus.Subscribe<MatchStartedEvent>(_ => { ClearRounds(); _resultShown = false; ClearCenter(); }));
            _subs.Add(bus.Subscribe<RoundStartedEvent>(OnRoundStarted));
            _subs.Add(bus.Subscribe<CountdownTickEvent>(e => ShowCountdown(e.SecondsRemaining)));
            _subs.Add(bus.Subscribe<RecordingStartedEvent>(_ => { ClearCenter(); _showRecordingTimer = true; }));
            _subs.Add(bus.Subscribe<RecordingCompletedEvent>(_ => { _showRecordingTimer = false; _timerDisplay.text = ""; Announce("RECORDING SAVED!", true); }));
            _subs.Add(bus.Subscribe<BattleStartedEvent>(_ => Announce("FIGHT!", true)));
            _subs.Add(bus.Subscribe<RoundEndedEvent>(e => { if (e.Outcome != RoundOutcome.NoKill) ShowResult(e.Outcome); }));
            _subs.Add(bus.Subscribe<MatchEndedEvent>(e => ShowResult(e.Outcome)));

            ClearCenter();
            ClearRounds();
            _timerDisplay.text = "";
        }

        private void OnDestroy()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void Update()
        {
            if (_showRecordingTimer && _match?.CurrentRunner != null)
            {
                _timerDisplay.text = $"{_match.CurrentRunner.SecondsRemaining:F2}s";
            }

            if (_clearAnnouncementAt > 0f && Time.unscaledTime >= _clearAnnouncementAt)
            {
                _clearAnnouncementAt = -1f;
                ClearCenter();
            }
        }

        private void OnRoundStarted(RoundStartedEvent e)
        {
            Instantiate(_roundUIPrefab, _roundsCountPanel.transform);
            if (e.RoundNumber > 1) Announce($"ROUND {e.RoundNumber}", true);
        }

        private void ShowCountdown(int seconds)
        {
            _announcementDisplay.gameObject.SetActive(false);
            _countdownDisplay.gameObject.SetActive(true);
            _countdownDisplay.text = seconds.ToString();
            _timerDisplay.text = "";
        }

        private void ShowResult(RoundOutcome outcome)
        {
            if (_resultShown) return;
            _resultShown = true;

            string text = outcome switch
            {
                RoundOutcome.PlayerWin => "FIGHTER 1 WINS!",
                RoundOutcome.OpponentWin => "FIGHTER 2 WINS!",
                _ => "DRAW!"
            };

            Announce(text, false);
        }

        private void Announce(string text, bool autoClear)
        {
            _countdownDisplay.gameObject.SetActive(false);
            _announcementDisplay.gameObject.SetActive(true);
            _announcementDisplay.text = text;
            _clearAnnouncementAt = autoClear ? Time.unscaledTime + _announcementSeconds : -1f;
        }

        private void ClearCenter()
        {
            _countdownDisplay.text = "";
            _countdownDisplay.gameObject.SetActive(false);
            _announcementDisplay.gameObject.SetActive(false);
        }

        private void ClearRounds()
        {
            foreach (Transform child in _roundsCountPanel.transform) Destroy(child.gameObject);
        }
    }
}
