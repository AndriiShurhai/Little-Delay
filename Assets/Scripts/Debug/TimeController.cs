#if UNITY_EDITOR
using UnityEngine;

public class TimeController : MonoBehaviour
{
    private bool isSlowMo = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            isSlowMo = !isSlowMo;

            if (isSlowMo)
            {
                Time.timeScale = 0.1f; // Run at 10% speed
                // Crucial: We must also scale FixedDeltaTime to maintain perfect physics determinism!
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                Debug.Log("<color=yellow>[DEBUG] Slow Motion: ON</color>");
            }
            else
            {
                Time.timeScale = 1f; // Back to normal speed
                Time.fixedDeltaTime = 0.02f;
                Debug.Log("<color=yellow>[DEBUG] Slow Motion: OFF</color>");
            }
        }
    }
}
#endif