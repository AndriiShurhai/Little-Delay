using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


public class GameOverScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject _gameOverScreen;
    [SerializeField] private TMPro.TextMeshProUGUI _winnerText;

    private void Start()
    {
        HideGameOverScreen();
    }

    public void HideGameOverScreen()
    {
        _gameOverScreen.SetActive(false);
    }
    public void ShowGameOverScreen()
    {
        _gameOverScreen.SetActive(true);
    }

    public void Quit()
    {
        SceneLoader.Instance?.LoadScene("MainMenu");
    }
}