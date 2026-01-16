using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SceneLoaderHoward : MonoBehaviour
{
    public static SceneLoaderHoward Instance;

    [Header("Settings")]
    public GameObject loadingScreenPrefab;
    [Range(0.1f, 2f)]
    public float fillSpeed = 0.5f; // Lower = Slower smooth filling

    [Header("Loading Screen Components (Assigned in Prefab)")]
    public Image progressBar; // Changed from Slider to Image
    public TextMeshProUGUI loadingText;
    public Image steeringWheel;

    private Coroutine dotAnimationCoroutine;
    private Coroutine wheelAnimationCoroutine;

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

        // Get components from the instantiated prefab
        SceneLoaderHoward loader = loadingScreen.GetComponent<SceneLoaderHoward>();
        if (loader == null)
        {
            Debug.LogError("Loading screen prefab must have SceneLoaderHoward component!");
            yield break;
        }

        Image bar = loader.progressBar;
        TextMeshProUGUI text = loader.loadingText;
        Image wheel = loader.steeringWheel;

        // Set initial values
        if (bar != null) bar.fillAmount = 0f;
        if (text != null) text.text = "Loading";

        // Start animations
        dotAnimationCoroutine = StartCoroutine(AnimateLoadingText(text));
        wheelAnimationCoroutine = StartCoroutine(AnimateSteeringWheel(wheel));

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
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, fillSpeed * Time.deltaTime);

            // Update UI
            if (bar != null) bar.fillAmount = visualProgress;

            // 4. Check if we can finish
            if (operation.progress >= 0.9f && visualProgress >= 0.99f)
            {
                // Stop animations
                if (dotAnimationCoroutine != null)
                {
                    StopCoroutine(dotAnimationCoroutine);
                }

                if (wheelAnimationCoroutine != null)
                {
                    StopCoroutine(wheelAnimationCoroutine);
                }

                // Final state
                if (bar != null) bar.fillAmount = 1f;
                if (text != null) text.text = "Complete!";

                yield return new WaitForSeconds(0.5f);

                // Allow the scene to switch
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 5. Cleanup
        Destroy(loadingScreen);
    }

    IEnumerator AnimateLoadingText(TextMeshProUGUI loadingText)
    {
        if (loadingText == null) yield break;

        int dotCount = 0;

        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            loadingText.text = "Loading" + new string('.', dotCount);
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator AnimateSteeringWheel(Image steeringWheel)
    {
        if (steeringWheel == null) yield break;

        float currentRotation = 0f;
        float maxRotation = 750f;
        bool turningRight = true;

        while (true)
        {
            float startRotation = currentRotation;
            float targetRotation = turningRight ? maxRotation : 0f;

            float duration = 3.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Improved Ease In-Out (Cubic)
                float easedT = t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

                currentRotation = Mathf.Lerp(startRotation, targetRotation, easedT);
                steeringWheel.transform.rotation = Quaternion.Euler(0f, 0f, -currentRotation);

                yield return null;
            }

            currentRotation = targetRotation;
            steeringWheel.transform.rotation = Quaternion.Euler(0f, 0f, -currentRotation);

            turningRight = !turningRight;

            yield return new WaitForSeconds(0.3f);
        }
    }
}