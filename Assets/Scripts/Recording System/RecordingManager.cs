using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class RecordingManager : MonoBehaviour
{
    public static RecordingManager Instance { get; private set; }

    [SerializeField] private InputPlayback _inputPlayback;
    [SerializeField] private PlayerInputSource _playerInputSource;
    [SerializeField] private InputRecorder _inputRecorder;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        //BattleManager.Instance.LivePlayer.SetInputSource(_playerInputSource);

        //GameInput.Instance.OnRecordAction += OnRecordStart;
        //GameInput.Instance.OnRecordStopAction += OnRecordStop;
        //GameInput.Instance.OnPlaybackStopAction += OnPlaybackStop;
        //GameInput.Instance.OnPlaybackPauseAction += OnPlaybackPause;

        //_inputPlayback.OnPlaybackCompleted += OnPlaybackFinished;
    }

    private void OnDestroy()
    {
        //if (GameInput.Instance != null)
        //{
        //    GameInput.Instance.OnRecordAction -= OnRecordStart;
        //    GameInput.Instance.OnRecordStopAction -= OnRecordStop;
        //    GameInput.Instance.OnPlaybackStopAction -= OnPlaybackStop;
        //    GameInput.Instance.OnPlaybackPauseAction -= OnPlaybackPause;
        //}

        //_inputPlayback.OnPlaybackCompleted -= OnPlaybackFinished;
    }

    private void OnRecordStart()
    {
        //BattleManager.Instance.LivePlayer.SetInputSource(_playerInputSource);
        //_inputRecorder.StartRecording();
    }

    private void OnRecordStop()
    {
        Recording recording = _inputRecorder.StopRecording();
        //_inputPlayback.StartPlayback(recording);

        //BattleManager.Instance.LivePlayer.SetInputSource(_inputPlayback);
    }

    private void OnPlaybackStop()
    {
        _inputPlayback.StopPlayback();
        BattleManager.Instance.LivePlayer.SetInputSource(_playerInputSource);
    }

    private void OnPlaybackFinished()
    {
        BattleManager.Instance.LivePlayer.SetInputSource(_playerInputSource);
    }

    private void OnPlaybackPause()
    {
#if UNITY_EDITOR
        // Debug: apply a random deflection force to test the physics recovery system.
        Vector3 randomDirection = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;

        BattleManager.Instance.LivePlayer.ApplyDeflectionForce(randomDirection * 10f);
#endif
    }
}