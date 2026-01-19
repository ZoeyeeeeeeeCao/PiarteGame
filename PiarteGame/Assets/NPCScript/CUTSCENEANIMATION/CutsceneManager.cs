using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;

    [Header("Audio")]
    [Tooltip("Optional. If null, will FindObjectOfType<AudioManager>().")]
    public AudioManager audioManager;

    [Header("Settings")]
    public string nextSceneName;

    [Tooltip("If true, player can press Enter to skip the cutscene")]
    public bool allowSkip = true;

    [Tooltip("Which key to press to skip (default: Return = Enter)")]
    public KeyCode skipKey = KeyCode.Return;

    private bool hasSkipped = false;
    private bool hasEnded = false;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (audioManager == null)
            audioManager = FindObjectOfType<AudioManager>();

        // Mute BGM (music mixer group) while cutscene plays
        if (audioManager != null)
            audioManager.MuteMusicForVideo();
        else
            Debug.LogWarning("⚠️ CutsceneManager: AudioManager not found, can't mute music.");

        // Subscribe to the event that triggers when the video reaches the end
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
        else
            Debug.LogError("CutsceneManager: VideoPlayer missing!");
    }

    void Update()
    {
        if (!allowSkip || hasSkipped || hasEnded) return;

        if (Input.GetKeyDown(skipKey))
        {
            hasSkipped = true;
            Debug.Log("Cutscene skipped by player");
            EndAndLoad();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (hasEnded || hasSkipped) return;

        Debug.Log("Cutscene finished naturally");
        EndAndLoad();
    }

    private void EndAndLoad()
    {
        if (hasEnded) return;
        hasEnded = true;

        // Unsubscribe from event to prevent double-loading
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;

        // Restore BGM
        if (audioManager != null)
            audioManager.RestoreMusicAfterVideo();

        // Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("CutsceneManager: No scene name provided!");
        }
    }

    void OnDestroy()
    {
        // Safety cleanup
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;

        // If object is destroyed mid-video, try to restore music
        if (audioManager != null)
            audioManager.RestoreMusicAfterVideo();
    }
}
