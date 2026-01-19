using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using TMPro;

public class SceneLoaderHoward : MonoBehaviour
{
    public static SceneLoaderHoward Instance;

    [Header("Settings")]
    public GameObject loadingScreenPrefab;
    [Range(0.1f, 2f)]
    public float fillSpeed = 0.5f;

    [Header("Scene Video References")]
    public VideoPlayer sceneVideoPlayer; // Drag your scene's VideoPlayer here
    public GameObject sceneVideoCanvas;   // Drag the Canvas holding your RawImage here
    public bool playVideoBeforeLoading = true;

    private void Awake()
    {
        if (Instance == null)
        {
            // If I am the first one, I am the Boss.
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Hide canvas initially
            if (sceneVideoCanvas) sceneVideoCanvas.SetActive(false);
        }
        else
        {
            // A Boss already exists! 
            // But I (the new script) have the correct video for THIS level.
            // So, I will give my video references to the Boss before I die.

            Instance.sceneVideoPlayer = this.sceneVideoPlayer;
            Instance.sceneVideoCanvas = this.sceneVideoCanvas;
            Instance.playVideoBeforeLoading = this.playVideoBeforeLoading;

            // Make sure the Boss hides the canvas I just gave him
            if (Instance.sceneVideoCanvas) Instance.sceneVideoCanvas.SetActive(false);

            // Now I can die peacefully
            Destroy(gameObject);
        }
    }

    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadProcess(sceneName));
    }

    IEnumerator LoadProcess(string sceneName)
    {
        if (sceneVideoCanvas) sceneVideoCanvas.SetActive(false);
        // --- STEP 1: PLAY THE SCENE VIDEO FIRST ---
        if (playVideoBeforeLoading && sceneVideoPlayer != null)
        {
            // Ensure the UI is ready but black initially
            if (sceneVideoCanvas != null) sceneVideoCanvas.SetActive(true);

            sceneVideoPlayer.Prepare();
            while (!sceneVideoPlayer.isPrepared) yield return null;

            sceneVideoPlayer.Play();

            // Wait for the video to finish completely
            while (sceneVideoPlayer.isPlaying)
            {
                yield return null;
            }

            // Hide the video canvas once done
            if (sceneVideoCanvas != null) sceneVideoCanvas.SetActive(false);
        }

        // --- STEP 2: RUN THE NORMAL LOADING PREFAB ---
        GameObject loadingScreen = Instantiate(loadingScreenPrefab);
        DontDestroyOnLoad(loadingScreen);

        LoadingScreenUI ui = loadingScreen.GetComponent<LoadingScreenUI>();
        if (ui == null) { yield break; }

        // Start standard loading animations
        Coroutine dotAnim = StartCoroutine(AnimateLoadingText(ui.loadingText));
        Coroutine wheelAnim = StartCoroutine(AnimateSteeringWheel(ui.steeringWheel));

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float visualProgress = 0f;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, fillSpeed * Time.deltaTime);

            if (ui.progressBar != null) ui.progressBar.fillAmount = visualProgress;

            if (operation.progress >= 0.9f && visualProgress >= 0.99f)
            {
                if (dotAnim != null) StopCoroutine(dotAnim);
                if (wheelAnim != null) StopCoroutine(wheelAnim);

                if (ui.loadingText != null) ui.loadingText.text = "Complete!";
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