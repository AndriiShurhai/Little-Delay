using UnityEngine;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Used for the 3.. 2.. 1.. countdowns and center screen announcements.")]
    [SerializeField] private TextMeshProUGUI _centerDisplay;
    
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
        }
    }

    private void UpdateCountdown(int seconds)
    {
        _centerDisplay.gameObject.SetActive(true);
        _centerDisplay.text = seconds.ToString();
        
        // Keep the timer clear while counting down
        _timerDisplay.text = "";
    }

    private void UpdateTimer(float timeRemaining)
    {
        // Hide the giant center text while playing/recording
        _centerDisplay.gameObject.SetActive(false); 
        
        // Format to show 2 decimal places (e.g., "Recording: 9.99s")
        _timerDisplay.text = $"Recording: {timeRemaining:F2}s";
    }

    private void ShowRecordingFinished()
    {
        _centerDisplay.gameObject.SetActive(true);
        _centerDisplay.text = "RECORDING SAVED!";
        _timerDisplay.text = "";
    }

    private void ShowBattlePhase()
    {
        _centerDisplay.gameObject.SetActive(true);
        _centerDisplay.text = "FIGHT!";
        _timerDisplay.text = "";
        
        // Hide the "FIGHT!" text after 1.5 seconds so it doesn't block the screen
        Invoke(nameof(ClearCenterDisplay), 1.5f);
    }

    private void ClearCenterDisplay()
    {
        _centerDisplay.text = "";
        _centerDisplay.gameObject.SetActive(false);
    }

}