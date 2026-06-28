#if UNITY_EDITOR
using UnityEngine;

public class TimeController : MonoBehaviour
{
    // The default fixed timestep. Store it on start so it's always in sync
    // with the Project Settings value rather than a hardcoded 0.02f.
    private float defaultFixedTimestep;

    private void Start()
    {
        defaultFixedTimestep = Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.T)) return;

        bool goingSlowMo = Time.timeScale >= 1f;

        if (goingSlowMo)
        {
            Time.timeScale = 0.1f;
            // Scale fixedDeltaTime to maintain physics determinism at reduced speed.
            Time.fixedDeltaTime = defaultFixedTimestep * Time.timeScale;
            Debug.Log("<color=yellow>[DEBUG] Slow Motion: ON</color>");
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedTimestep;
            Debug.Log("<color=yellow>[DEBUG] Slow Motion: OFF</color>");
        }
    }
}
#endif