using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;

    [Header("Settings")]
    public string nextSceneName;
    [Tooltip("If true, player can press any key to skip the cutscene")]
    public bool allowSkip = true;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Subscribe to the event that triggers when the video reaches the end
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        // Skip logic
        if (allowSkip && Input.anyKeyDown)
        {
            LoadNextScene();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        LoadNextScene();
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