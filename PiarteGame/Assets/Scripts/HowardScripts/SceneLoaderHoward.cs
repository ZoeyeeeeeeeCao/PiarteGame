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
    public GameObject sceneVideoCanvas;  // Drag the Canvas holding your RawImage here
    public bool playVideoBeforeLoading = true;

    [Header("Audio (Mute BGM During Video)")]
    [Tooltip("Optional. If null, will auto-find AudioManager in scene / DontDestroy.")]
    public AudioManager audioManager;

    [Tooltip("Mute music while the scene video plays.")]
    public bool muteMusicDuringSceneVideo = true;

    [Tooltip("Keep music muted during the loading screen too.")]
    public bool keepMusicMutedDuringLoading = false;

    private bool _musicMutedByThisLoader = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (sceneVideoCanvas) sceneVideoCanvas.SetActive(false);

            if (audioManager == null)
                audioManager = FindObjectOfType<AudioManager>();
        }
        else
        {
            // Copy level-specific references into the Boss instance
            Instance.sceneVideoPlayer = this.sceneVideoPlayer;
            Instance.sceneVideoCanvas = this.sceneVideoCanvas;
            Instance.playVideoBeforeLoading = this.playVideoBeforeLoading;

            Instance.muteMusicDuringSceneVideo = this.muteMusicDuringSceneVideo;
            Instance.keepMusicMutedDuringLoading = this.keepMusicMutedDuringLoading;

            // Only overwrite audioManager if the boss doesn't have one
            if (Instance.audioManager == null)
                Instance.audioManager = this.audioManager != null ? this.audioManager : FindObjectOfType<AudioManager>();

            if (Instance.sceneVideoCanvas) Instance.sceneVideoCanvas.SetActive(false);

            Destroy(gameObject);
        }
    }

    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadProcess(sceneName));
    }

    IEnumerator LoadProcess(string sceneName)
    {
        // Always refresh AudioManager reference (some scenes spawn it late)
        if (audioManager == null)
            audioManager = FindObjectOfType<AudioManager>();

        if (sceneVideoCanvas) sceneVideoCanvas.SetActive(false);

        // -------------------------
        // STEP 1: PLAY SCENE VIDEO
        // -------------------------
        if (playVideoBeforeLoading && sceneVideoPlayer != null)
        {
            if (muteMusicDuringSceneVideo)
                MuteMusic();

            if (sceneVideoCanvas != null) sceneVideoCanvas.SetActive(true);

            sceneVideoPlayer.Prepare();
            while (!sceneVideoPlayer.isPrepared) yield return null;

            sceneVideoPlayer.Play();

            // Wait until video ends
            while (sceneVideoPlayer.isPlaying)
                yield return null;

            if (sceneVideoCanvas != null) sceneVideoCanvas.SetActive(false);

            // If you DON'T want music muted during loading, restore now
            if (muteMusicDuringSceneVideo && !keepMusicMutedDuringLoading)
                RestoreMusic();
        }

        // -------------------------
        // STEP 2: LOADING SCREEN
        // -------------------------
        GameObject loadingScreen = null;

        if (loadingScreenPrefab != null)
        {
            loadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(loadingScreen);
        }

        LoadingScreenUI ui = loadingScreen != null ? loadingScreen.GetComponent<LoadingScreenUI>() : null;

        Coroutine dotAnim = null;
        Coroutine wheelAnim = null;

        if (ui != null)
        {
            dotAnim = StartCoroutine(AnimateLoadingText(ui.loadingText));
            wheelAnim = StartCoroutine(AnimateSteeringWheel(ui.steeringWheel));
        }

        // If we want to keep muted during loading, make sure it's muted here
        if (keepMusicMutedDuringLoading)
            MuteMusic();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float visualProgress = 0f;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, fillSpeed * Time.deltaTime);

            if (ui != null && ui.progressBar != null)
                ui.progressBar.fillAmount = visualProgress;

            if (operation.progress >= 0.9f && visualProgress >= 0.99f)
            {
                if (dotAnim != null) StopCoroutine(dotAnim);
                if (wheelAnim != null) StopCoroutine(wheelAnim);

                if (ui != null && ui.loadingText != null)
                    ui.loadingText.text = "Complete!";

                yield return new WaitForSeconds(0.5f);

                // ✅ restore right before scene becomes active
                RestoreMusic();

                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        if (loadingScreen != null)
            Destroy(loadingScreen);
    }

    // -------------------------
    // MUSIC MUTE/RESTORE
    // -------------------------
    private void MuteMusic()
    {
        if (_musicMutedByThisLoader) return;

        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("⚠️ SceneLoaderHoward: AudioManager not found (can't mute music).");
                return;
            }
        }

        audioManager.MuteMusicForVideo();
        _musicMutedByThisLoader = true;
        Debug.Log("🎵 SceneLoaderHoward muted music");
    }

    private void RestoreMusic()
    {
        if (!_musicMutedByThisLoader) return;

        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("⚠️ SceneLoaderHoward: AudioManager not found (can't restore music).");
                _musicMutedByThisLoader = false;
                return;
            }
        }

        audioManager.RestoreMusicAfterVideo();
        _musicMutedByThisLoader = false;
        Debug.Log("🎵 SceneLoaderHoward restored music");
    }

    // -------------------------
    // Loading UI animations
    // -------------------------
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
