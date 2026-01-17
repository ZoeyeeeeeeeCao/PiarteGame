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
    public float fillSpeed = 0.5f;

    // We don't need the public variables here anymore because they are on the prefab script!

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

        // --- UPDATED LOGIC ---
        // Look for the NEW UI script instead of SceneLoaderHoward
        LoadingScreenUI ui = loadingScreen.GetComponent<LoadingScreenUI>();

        if (ui == null)
        {
            Debug.LogError("Your Loading Screen Prefab is missing the 'LoadingScreenUI' script!");
            yield break; // Stop if script is missing
        }

        // Get references from the UI script
        Image bar = ui.progressBar;
        TextMeshProUGUI text = ui.loadingText;
        Image wheel = ui.steeringWheel;

        // Set initial values
        if (bar != null) bar.fillAmount = 0f;
        if (text != null) text.text = "Loading";

        // Start animations
        Coroutine dotAnim = StartCoroutine(AnimateLoadingText(text));
        Coroutine wheelAnim = StartCoroutine(AnimateSteeringWheel(wheel));

        // Wait to ensure UI renders
        yield return new WaitForSeconds(0.5f);

        // 2. Start Async Loading
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float visualProgress = 0f;

        // 3. Loop
        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, fillSpeed * Time.deltaTime);

            if (bar != null) bar.fillAmount = visualProgress;

            // 4. Check finish
            if (operation.progress >= 0.9f && visualProgress >= 0.99f)
            {
                if (dotAnim != null) StopCoroutine(dotAnim);
                if (wheelAnim != null) StopCoroutine(wheelAnim);

                if (bar != null) bar.fillAmount = 1f;
                if (text != null) text.text = "Complete!";

                yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

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
        bool turningRight = true;

        while (true)
        {
            float start = currentRotation;
            float target = turningRight ? 750f : 0f;
            float duration = 3.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smooth Step
                float easedT = t * t * (3f - 2f * t);

                currentRotation = Mathf.Lerp(start, target, easedT);
                steeringWheel.transform.rotation = Quaternion.Euler(0f, 0f, -currentRotation);
                yield return null;
            }
            turningRight = !turningRight;
            yield return new WaitForSeconds(0.3f);
        }
    }
}