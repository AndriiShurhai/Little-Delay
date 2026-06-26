#if UNITY_EDITOR
using Unity.VisualScripting;
using UnityEngine;

public class ReplayVisualizer : MonoBehaviour
{
    private LineRenderer recordedLine;
    private LineRenderer playbackLine;

    private PlayerController player;
    private InputRecorder recorder;

    private bool wasRecording = false;
    private bool wasPlayingBack = false;

    private void Start()
    {
        player = PlayerController.Instance;
        // Find the recorder dynamically
        recorder = Object.FindFirstObjectByType<InputRecorder>();

        // Create the two permanent lines
        recordedLine = SetupLine("Recorded Path (Blue)", Color.blue);
        playbackLine = SetupLine("Playback Path (Red)", Color.red);
    }

    private LineRenderer SetupLine(string objName, Color color)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(this.transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();

        if (color == Color.blue)
        {
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
        }

        else
        {
            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
        }

        // Use a default material so it renders brightly without lighting issues
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = 0;

        return lr;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            // clearing all lines
            recordedLine.positionCount = 0;
            playbackLine.positionCount = 0;
        }
    }

    private void FixedUpdate()
    {
        if (player == null || recorder == null) return;

        bool isRecordingNow = recorder.IsRecording;
        bool isPlayingBackNow = player.IsPlayingBack;

        // --- HANDLE RECORDING (BLUE LINE) ---
        if (isRecordingNow)
        {
            // If we just started a brand new recording, wipe the old blue line
            if (!wasRecording) recordedLine.positionCount = 0;

            AddPoint(recordedLine, player.transform.position);
        }

        // --- HANDLE PLAYBACK (RED LINE) ---
        if (isPlayingBackNow)
        {
            // If we just started a brand new playback, wipe the old red line
            if (!wasPlayingBack) playbackLine.positionCount = 0;

            AddPoint(playbackLine, player.transform.position);
        }

        wasRecording = isRecordingNow;
        wasPlayingBack = isPlayingBackNow;
    }

    private void AddPoint(LineRenderer lr, Vector3 position)
    {
        lr.positionCount++;
        // We lift the line up by 0.1f on the Y-axis so it doesn't clip into the floor geometry
        lr.SetPosition(lr.positionCount - 1, position + (Vector3.up * 0.1f));
    }
}
#endif