using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("Settings")]
    public GameObject loadingScreenPrefab;
    [Range(0.1f, 2f)]
    public float fillSpeed = 0.5f; // Lower = Slower smooth filling

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadProcess(sceneName));
    }

    IEnumerator LoadProcess(string sceneName)
    {
        // 1. Spawn Loading Screen
        GameObject loadingScreen = Instantiate(loadingScreenPrefab);
        DontDestroyOnLoad(loadingScreen);

        Slider progressBar = loadingScreen.GetComponentInChildren<Slider>();
        TextMeshProUGUI progressText = loadingScreen.GetComponentInChildren<TextMeshProUGUI>();

        // Set initial values
        if (progressBar != null) progressBar.value = 0f;
        if (progressText != null) progressText.text = "0%";

        // Wait to ensure the UI is rendered (Fixes the freeze)
        yield return new WaitForSeconds(0.5f);

        // 2. Start Async Loading
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // IMPORTANT: Prevent the scene from switching immediately
        operation.allowSceneActivation = false;

        float visualProgress = 0f;

        // 3. Loop until the scene is fully loaded AND the bar is fully filled
        while (!operation.isDone)
        {
            // The "Real" progress stops at 0.9. We scale it to 1.0.
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // --- THE SMOOTHING LOGIC ---
            // Instead of jumping straight to targetProgress, we move there gradually.
            // Time.deltaTime * fillSpeed ensures it takes time to fill up.
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, fillSpeed * Time.deltaTime);

            // Update UI
            if (progressBar != null) progressBar.value = visualProgress;
            if (progressText != null) progressText.text = (visualProgress * 100f).ToString("F0") + "%";

            // 4. Check if we can finish
            // We only switch scenes if:
            // A) Unity has finished loading (progress >= 0.9)
            // B) Our visual bar has actually reached 100% (visualProgress >= 0.99)
            if (operation.progress >= 0.9f && visualProgress >= 0.99f)
            {
                // Optional: Short pause at 100% for polish
                if (progressBar != null) progressBar.value = 1f;
                if (progressText != null) progressText.text = "100%";
                yield return new WaitForSeconds(0.5f);

                // Allow the scene to switch
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 5. Cleanup
        Destroy(loadingScreen);
    }
}