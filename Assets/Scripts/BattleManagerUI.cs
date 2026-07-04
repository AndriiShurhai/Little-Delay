using UnityEngine;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _countdownDisplay;
    [SerializeField] private TextMeshProUGUI _announcementDisplay;

    [Tooltip("Used for the active recording timer.")]
    [SerializeField] private TextMeshProUGUI _timerDisplay;

    private void Start()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnCountdownTick.AddListener(UpdateCountdown);
            BattleManager.Instance.OnRecordingTick.AddListener(UpdateTimer);
            BattleManager.Instance.OnRecordingFinished.AddListener(ShowRecordingFinished);
            BattleManager.Instance.OnBattlePhaseStarted.AddListener(ShowBattlePhase);
            BattleManager.Instance.OnRoundContinues.AddListener(ShowRoundContinues);
            BattleManager.Instance.OnFighterWon.AddListener(ShowFighterWon);
            BattleManager.Instance.OnMatchDraw.AddListener(ShowMatchDraw);
        }

        ClearCenterDisplay();
        _timerDisplay.text = "";
    }

    private void OnDestroy()
    {
        // Always clean up listeners to prevent memory leaks or null reference errors
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnCountdownTick.RemoveListener(UpdateCountdown);
            BattleManager.Instance.OnRecordingTick.RemoveListener(UpdateTimer);
            BattleManager.Instance.OnRecordingFinished.RemoveListener(ShowRecordingFinished);
            BattleManager.Instance.OnBattlePhaseStarted.RemoveListener(ShowBattlePhase);
            BattleManager.Instance.OnRoundContinues.RemoveListener(ShowRoundContinues);
            BattleManager.Instance.OnFighterWon.RemoveListener(ShowFighterWon);
            BattleManager.Instance.OnMatchDraw.RemoveListener(ShowMatchDraw);
        }
    }

    private void UpdateCountdown(int seconds)
    {
        _announcementDisplay.gameObject.SetActive(false);
        _countdownDisplay.gameObject.SetActive(true);
        _countdownDisplay.text = seconds.ToString();

        // Keep the timer clear while counting down
        _timerDisplay.text = "";
    }

    private void UpdateTimer(float timeRemaining)
    {
        // Hide the giant center text while playing/recording
        _announcementDisplay.gameObject.SetActive(false);
        _countdownDisplay.gameObject.SetActive(false);

        // Format to show 2 decimal places (e.g., "Recording: 9.99s")
        _timerDisplay.text = $"Recording: {timeRemaining:F2}s";
    }

    private void ShowRecordingFinished()
    {
        _countdownDisplay.gameObject.SetActive(false);
        _announcementDisplay.gameObject.SetActive(true);
        _announcementDisplay.text = "RECORDING SAVED!";
        _timerDisplay.text = "";
    }

    private void ShowBattlePhase()
    {
        _countdownDisplay.gameObject.SetActive(false);
        _announcementDisplay.gameObject.SetActive(true);
        _announcementDisplay.text = "FIGHT!";
        _timerDisplay.text = "";

        // Hide the "FIGHT!" text after 1.5 seconds so it doesn't block the screen
        Invoke(nameof(ClearCenterDisplay), 1.5f);
    }

    private void ShowRoundContinues(int nextRound)
    {
        _countdownDisplay.gameObject.SetActive(false);
        _announcementDisplay.gameObject.SetActive(true);
        _announcementDisplay.text = $"ROUND {nextRound}";
        _timerDisplay.text = "";

        Invoke(nameof(ClearCenterDisplay), 1.5f);
    }

    private void ShowFighterWon(int fighterId)
    {
        CancelInvoke(nameof(ClearCenterDisplay));

        _countdownDisplay.gameObject.SetActive(false);
        _announcementDisplay.gameObject.SetActive(true);
        _announcementDisplay.text = $"FIGHTER {fighterId + 1} WINS!";
        _timerDisplay.text = "";
    }

    private void ShowMatchDraw()
    {
        CancelInvoke(nameof(ClearCenterDisplay));

        _countdownDisplay.gameObject.SetActive(false);
        _announcementDisplay.gameObject.SetActive(true);
        _announcementDisplay.text = "DRAW!";
        _timerDisplay.text = "";
    }

    private void ClearCenterDisplay()
    {
        _countdownDisplay.text = "";
        _announcementDisplay.gameObject.SetActive(false);
        _countdownDisplay.text = "";
        _countdownDisplay.gameObject.SetActive(false);
    }
}