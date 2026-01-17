using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;

    [Header("Settings")]
    public string nextSceneName;

    [Tooltip("If true, player can press Enter to skip the cutscene")]
    public bool allowSkip = true;

    [Tooltip("Which key to press to skip (default: Return = Enter)")]
    public KeyCode skipKey = KeyCode.Return;

    private bool hasSkipped = false;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Subscribe to the event that triggers when the video reaches the end
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        // Skip logic - only skip with specified key (Enter by default)
        if (allowSkip && !hasSkipped && Input.GetKeyDown(skipKey))
        {
            hasSkipped = true;
            Debug.Log("Cutscene skipped by player");
            LoadNextScene();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (!hasSkipped)
        {
            Debug.Log("Cutscene finished naturally");
            LoadNextScene();
        }
    }

    public void LoadNextScene()
    {
        // Unsubscribe from event to prevent double-loading
        videoPlayer.loopPointReached -= OnVideoFinished;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("CutsceneManager: No scene name provided!");
        }
    }
}