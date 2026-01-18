using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Creates ping-pong effect by swapping between normal and reversed video clips
/// Fixed: Waits for video to be prepared before playing
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
    [SerializeField] private bool showDebugLogs = true;

    private bool playingForward = true;
    private bool isInitialized = false;
    private bool waitingForPrepare = false;

    void Start()
    {
        InitializeVideo();
    }

    void OnEnable()
    {
        Debug.Log($"🟢 [{gameObject.name}] OnEnable called");

        // Re-initialize when enabled
        if (isInitialized && !waitingForPrepare)
        {
            Debug.Log($"🔄 [{gameObject.name}] Re-starting video on Enable");
            PlayForward();
        }
    }

    void InitializeVideo()
    {
        Debug.Log($"🎬 [{gameObject.name}] Initializing VideoBackgroundPingPong");

        // Auto-find VideoPlayer
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError($"❌ [{gameObject.name}] No VideoPlayer found!");
                enabled = false;
                return;
            }
            Debug.Log($"✅ [{gameObject.name}] Auto-found VideoPlayer");
        }

        // Validate clips
        if (enablePingPong && (forwardClip == null || reversedClip == null))
        {
            Debug.LogError($"❌ [{gameObject.name}] Both Forward and Reversed clips must be assigned for ping-pong!");
            enabled = false;
            return;
        }
        if (!enablePingPong && forwardClip == null)
        {
            Debug.LogError($"❌ [{gameObject.name}] Forward clip must be assigned!");
            enabled = false;
            return;
        }

        Debug.Log($"✅ [{gameObject.name}] Clips validated");

        // Configure VideoPlayer
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.playbackSpeed = playbackSpeed;
        videoPlayer.waitForFirstFrame = true; // IMPORTANT: Wait for video to load

        Debug.Log($"⚙️ [{gameObject.name}] VideoPlayer configured");

        // Subscribe to events
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        Debug.Log($"✅ [{gameObject.name}] Event listeners attached");

        isInitialized = true;

        // Start with forward video
        PlayForward();
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log($"✅ [{gameObject.name}] Video prepared and ready!");
        waitingForPrepare = false;

        // Video is now ready, play it
        if (vp.isPrepared && !vp.isPlaying)
        {
            vp.Play();
            Debug.Log($"▶️ [{gameObject.name}] Started playback after prepare");
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log($"🏁 [{gameObject.name}] Video finished! Playing forward: {playingForward}");

        if (!enablePingPong)
        {
            Debug.Log($"🔁 [{gameObject.name}] Ping-pong disabled, looping forward");
            PlayForward();
            return;
        }

        // Ping-pong: swap videos
        if (playingForward)
        {
            Debug.Log($"⬅️ [{gameObject.name}] Switching to REVERSED");
            PlayReversed();
        }
        else
        {
            Debug.Log($"➡️ [{gameObject.name}] Switching to FORWARD");
            PlayForward();
        }
    }

    void PlayForward()
    {
        if (forwardClip == null)
        {
            Debug.LogError($"❌ [{gameObject.name}] Forward clip is null!");
            return;
        }

        Debug.Log($"▶️ [{gameObject.name}] Playing FORWARD video: {forwardClip.name}");

        playingForward = true;
        videoPlayer.clip = forwardClip;
        waitingForPrepare = true;

        // Prepare will trigger OnVideoPrepared, which will call Play()
        videoPlayer.Prepare();
    }

    void PlayReversed()
    {
        if (reversedClip == null)
        {
            Debug.LogError($"❌ [{gameObject.name}] Reversed clip is null!");
            return;
        }

        Debug.Log($"◀️ [{gameObject.name}] Playing REVERSED video: {reversedClip.name}");

        playingForward = false;
        videoPlayer.clip = reversedClip;
        waitingForPrepare = true;

        // Prepare will trigger OnVideoPrepared, which will call Play()
        videoPlayer.Prepare();
    }

    void OnDestroy()
    {
        Debug.Log($"🔴 [{gameObject.name}] OnDestroy called");
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }

    void OnDisable()
    {
        Debug.Log($"⏸️ [{gameObject.name}] OnDisable called");
    }

    // ===== PUBLIC CONTROL METHODS =====

    public void SetPingPongEnabled(bool enabled)
    {
        enablePingPong = enabled;
        if (!enabled && !playingForward)
        {
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
        Debug.Log($"🔄 [{gameObject.name}] RestartFromBeginning called");
        waitingForPrepare = false;
        PlayForward();
    }

    public void ForceRestart()
    {
        Debug.Log($"⚡ [{gameObject.name}] ForceRestart called");
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            waitingForPrepare = false;
            PlayForward();
        }
    }
}