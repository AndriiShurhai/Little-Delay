#if UNITY_EDITOR
using UnityEngine;

public class ReplayVisualizer : MonoBehaviour
{
    private LineRenderer _recordedLine;
    private LineRenderer _playbackLine;

    private PlayerController _player;
    private InputRecorder _recorder;

    private bool _wasRecording;
    private bool _wasPlayingBack;

    // Shared material across all visualizer line instances to avoid material leaks.
    private static Material lineMaterial;

    private void Start()
    {
        _player = PlayerController.Instance;
        _recorder = Object.FindFirstObjectByType<InputRecorder>();

        _recordedLine = CreateLine("Recorded Path (Blue)", Color.blue, width: 0.1f);
        _playbackLine = CreateLine("Playback Path (Red)", Color.red, width: 0.2f);
    }

    private LineRenderer CreateLine(string objectName, Color color, float width)
    {
        if (lineMaterial == null)
            lineMaterial = new Material(Shader.Find("Sprites/Default"));

        var go = new GameObject(objectName);
        go.transform.SetParent(transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = lineMaterial;
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = 0;
        return lr;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            _recordedLine.positionCount = 0;
            _playbackLine.positionCount = 0;
        }
    }

    private void FixedUpdate()
    {
        if (_player == null || _recorder == null) return;

        bool isRecordingNow = _recorder.IsRecording;
        bool isPlayingBackNow = _player.IsPlayingBack;

        if (isRecordingNow)
        {
            if (!_wasRecording) _recordedLine.positionCount = 0;
            AppendPoint(_recordedLine, _player.transform.position);
        }

        if (isPlayingBackNow)
        {
            if (!_wasPlayingBack) _playbackLine.positionCount = 0;
            AppendPoint(_playbackLine, _player.transform.position);
        }

        _wasRecording = isRecordingNow;
        _wasPlayingBack = isPlayingBackNow;
    }

    private static void AppendPoint(LineRenderer lr, Vector3 position)
    {
        lr.positionCount++;
        // Offset upward slightly so the line doesn't clip into floor geometry.
        lr.SetPosition(lr.positionCount - 1, position + Vector3.up * 0.1f);
    }
}
#endif