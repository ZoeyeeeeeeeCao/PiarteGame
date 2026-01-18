using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Creates ping-pong effect by swapping between normal and reversed video clips
/// Much smoother than manual backward playback!
/// </summary>
public class VideoBackgroundPingPong : MonoBehaviour
{
    [Header("Video Clips")]
    [Tooltip("Your original video (plays forward)")]
    public VideoClip forwardClip;

    [Tooltip("Your reversed video (plays backward)")]
    public VideoClip reversedClip;

    [Header("Video Player")]
    [Tooltip("The VideoPlayer component (auto-finds if empty)")]
    public VideoPlayer videoPlayer;

    [Header("Settings")]
    [Tooltip("Enable ping-pong effect (swap between videos)")]
    public bool enablePingPong = true;

    [Tooltip("Playback speed (1 = normal)")]
    [Range(0.1f, 3f)]
    public float playbackSpeed = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool playingForward = true;

    void Start()
    {
        // Auto-find VideoPlayer
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("❌ No VideoPlayer found!");
                return;
            }
        }

        // Validate clips
        if (enablePingPong && (forwardClip == null || reversedClip == null))
        {
            Debug.LogError("❌ Both Forward and Reversed clips must be assigned for ping-pong!");
            return;
        }

        if (!enablePingPong && forwardClip == null)
        {
            Debug.LogError("❌ Forward clip must be assigned!");
            return;
        }

        // Configure VideoPlayer
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false; // We handle looping
        videoPlayer.playbackSpeed = playbackSpeed;

        // Subscribe to end event
        videoPlayer.loopPointReached += OnVideoFinished;

        // Start with forward video
        PlayForward();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (!enablePingPong)
        {
            // Just loop the forward video
            PlayForward();
            return;
        }

        // Ping-pong: swap videos
        if (playingForward)
        {
            PlayReversed();
        }
        else
        {
            PlayForward();
        }
    }

    void PlayForward()
    {
        if (forwardClip == null) return;

        playingForward = true;
        videoPlayer.clip = forwardClip;
        videoPlayer.Play();

        if (showDebugLogs)
            Debug.Log("▶️ Playing FORWARD video");
    }

    void PlayReversed()
    {
        if (reversedClip == null) return;

        playingForward = false;
        videoPlayer.clip = reversedClip;
        videoPlayer.Play();

        if (showDebugLogs)
            Debug.Log("◀️ Playing REVERSED video");
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    // Public control methods
    public void SetPingPongEnabled(bool enabled)
    {
        enablePingPong = enabled;

        if (!enabled && !playingForward)
        {
            // If disabling ping-pong while on reversed, switch back to forward
            PlayForward();
        }
    }

    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Clamp(speed, 0.1f, 3f);
        if (videoPlayer != null)
        {
            videoPlayer.playbackSpeed = playbackSpeed;
        }
    }

    public void RestartFromBeginning()
    {
        PlayForward();
    }
}