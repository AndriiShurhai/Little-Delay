using Drewio.UnityToolkit.Scenes;
using LittleDelay.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDelay.Presentation
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private string _arenaSceneName = "SampleScene";

        private SceneLoadingService _sceneLoading;

        private void Awake()
        {
            // Leaf UI with no composition root of its own: resolve the one service it needs, once.
            _sceneLoading = GameCompositionRoot.Instance.Services.Resolve<SceneLoadingService>();

            _startButton.onClick.AddListener(OnStartButtonClicked);
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        private void OnStartButtonClicked() => _sceneLoading.LoadScene(_arenaSceneName);

        private void OnQuitButtonClicked() => Application.Quit();
    }
}
