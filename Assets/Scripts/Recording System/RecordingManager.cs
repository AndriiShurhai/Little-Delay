using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class RecordingManager : MonoBehaviour
{
    public static RecordingManager Instance { get; private set; }

    [SerializeField] private InputPlayback inputPlayback;
    [SerializeField] private PlayerInputSource playerInputSource;
    [SerializeField] private InputRecorder inputRecorder;

    private bool playbackPauseToggle = false;
    private Vector3 randomForceDirection;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        PlayerController.Instance.SetInputSource(playerInputSource);

        GameInput.Instance.OnRecordAction += GameInput_OnRecordAction;
        GameInput.Instance.OnRecordStopAction += GameInput_OnRecordStopAction;
        GameInput.Instance.OnPlaybackStopAction += GameInput_OnPlaybackStopAction;
        GameInput.Instance.OnPlaybackPauseAction += Instance_OnPlayBackPauseAction;
    }

    private void Instance_OnPlayBackPauseAction(object sender, EventArgs e)
    {
        randomForceDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        PlayerController.Instance.ApplyDeflectionForce(randomForceDirection * 30f);
    }

    private void Update()
    {

    }

    private void GameInput_OnPlaybackStopAction(object sender, EventArgs e)
    {
        inputPlayback.StopPlayback();
        PlayerController.Instance.SetInputSource(playerInputSource);
    }
    private void GameInput_OnRecordAction(object sender, EventArgs e)
    {
        PlayerController.Instance.SetInputSource(playerInputSource);
        inputRecorder.StartRecording();
    }

    private void GameInput_OnRecordStopAction(object sender, EventArgs e)
    {
        Recording recording = inputRecorder.StopRecording();
        inputPlayback.StartPlayback(recording);
    }
}