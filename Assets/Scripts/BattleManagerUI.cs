using UnityEngine;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _roundsCountPanel;
    [SerializeField] private GameObject _roundUIPrefab;
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
            BattleManager.Instance.OnRecordingStarts.AddListener(ShowNewRoundStarts);
            BattleManager.Instance.OnMatchStarted += () => ClearRoundsCountDisplay();
        }

        ClearCenterDisplay();
        ClearRoundsCountDisplay();
        _timerDisplay.text = "";
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnCountdownTick.RemoveListener(UpdateCountdown);
            BattleManager.Instance.OnRecordingTick.RemoveListener(UpdateTimer);
            BattleManager.Instance.OnRecordingFinished.RemoveListener(ShowRecordingFinished);
            BattleManager.Instance.OnBattlePhaseStarted.RemoveListener(ShowBattlePhase);
            BattleManager.Instance.OnRoundContinues.RemoveListener(ShowRoundContinues);
            BattleManager.Instance.OnFighterWon.RemoveListener(ShowFighterWon);
            BattleManager.Instance.OnMatchDraw.RemoveListener(ShowMatchDraw);
            BattleManager.Instance.OnRecordingStarts.RemoveListener(ShowNewRoundStarts);
            BattleManager.Instance.OnMatchStarted -= () => ClearRoundsCountDisplay();
        }
    }

    private void UpdateCountdown(int seconds)
    {
        _announcementDisplay.gameObject.SetActive(false);
        _countdownDisplay.gameObject.SetActive(true);
        _countdownDisplay.text = seconds.ToString();

        _timerDisplay.text = "";
    }

    private void UpdateTimer(float timeRemaining)
    {
        _announcementDisplay.gameObject.SetActive(false);
        _countdownDisplay.gameObject.SetActive(false);

        _timerDisplay.text = $"{timeRemaining:F2}s";
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

    private void ShowNewRoundStarts()
    {
        Instantiate(_roundUIPrefab, _roundsCountPanel.transform);
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

    private void ClearRoundsCountDisplay()
    {
        foreach (Transform child in _roundsCountPanel.transform)
        {
            Destroy(child.gameObject);
        }
    }
}