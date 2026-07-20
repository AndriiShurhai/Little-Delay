using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;
    private void Awake()
    {
        _startButton.onClick.AddListener(OnStartButtonClicked);
        _quitButton.onClick.AddListener(OnQuitButtonClicked);
    }
    private void OnStartButtonClicked()
    {
        SceneLoader.Instance.LoadScene("SampleScene");
    }
    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}