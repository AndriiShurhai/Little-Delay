using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 4f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public static event Action<string> OnSceneLoadStarted;
    public static event Action<string, float> OnSceneLoadProgress; // sceneName, progress (0-1)
    public static event Action<string> OnSceneLoadCompleted;
    public static event Action<string, string> OnSceneLoadFailed; // sceneName, error message

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvas == null)
        {
            Debug.LogWarning("SceneLoader: No fade canvas assigned. Scene transitions will be instant.");
        }
    }

    public void LoadSceneByName(string name)
    {
        LoadScene(name);
    }

    public void LoadAdditiveSceneByName(string name)
    {
        LoadScene(name, true);
    }

    public void LoadScene(string sceneName, bool additive = false)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"SceneLoader: Already loading a scene. Ignoring request to load '{sceneName}'");
            return;
        }

        if (!IsSceneValid(sceneName))
        {
            string error = $"Scene '{sceneName}' not found in build settings";
            Debug.LogError($"SceneLoader: {error}");
            OnSceneLoadFailed?.Invoke(sceneName, error);
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, additive));
    }

    private bool IsSceneValid(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator LoadSceneRoutine(string sceneName, bool additive)
    {
        IsLoading = true;
        OnSceneLoadStarted?.Invoke(sceneName);

        yield return StartCoroutine(Fade(1));

        AsyncOperation loadOp;
        if (!additive)
        {
            loadOp = SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        if (loadOp == null)
        {
            string error = "Failed to start scene loading operation";
            Debug.LogError($"SceneLoader: {error}");
            OnSceneLoadFailed?.Invoke(sceneName, error);

            // Fade back in on error
            yield return StartCoroutine(Fade(0));
            IsLoading = false;
            yield break;
        }

        while (!loadOp.isDone)
        {
            float progress = Mathf.Clamp01(loadOp.progress / 0.9f); // LoadSceneAsync progress goes to 0.9f, then jumps to 1
            OnSceneLoadProgress?.Invoke(sceneName, progress);
            yield return null;
        }

        // Ensure final progress update
        OnSceneLoadProgress?.Invoke(sceneName, 1f);

        // Small delay to ensure scene is fully initialized
        yield return new WaitForEndOfFrame();

        // Fade back in
        yield return StartCoroutine(Fade(0));

        IsLoading = false;
        OnSceneLoadCompleted?.Invoke(sceneName);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvas == null) yield break;

        fadeCanvas.blocksRaycasts = true;

        float startAlpha = fadeCanvas.alpha;
        float startTime = Time.realtimeSinceStartup;
        float endTime = startTime + fadeDuration;

        while (Time.realtimeSinceStartup < endTime)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float normalizedTime = elapsed / fadeDuration;
            float curveValue = fadeCurve.Evaluate(normalizedTime);
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
        fadeCanvas.blocksRaycasts = targetAlpha > 0;
    }

    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName);
    }

    public void UnloadScene(string sceneName)
    {
        if (IsLoading)
        {
            Debug.LogWarning("SceneLoader: Cannot unload scene while loading is in progress");
            return;
        }

        StartCoroutine(UnloadSceneRoutine(sceneName));
    }

    private IEnumerator UnloadSceneRoutine(string sceneName)
    {
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);

        if (unloadOp != null)
        {
            while (!unloadOp.isDone)
            {
                yield return null;
            }

            // Clean up memory
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }

    public string[] GetAllSceneNames()
    {
        string[] sceneNames = new string[SceneManager.sceneCountInBuildSettings];
        for (int i = 0; i < sceneNames.Length; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }
        return sceneNames;
    }
}