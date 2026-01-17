using UnityEngine;
using UnityEngine.Video;

public class InteractToPlayVideo : MonoBehaviour
{
    [Header("Video Settings")]
    [Tooltip("Drag the Object that has the Video Player component here")]
    public VideoPlayer videoPlayer;

    [Tooltip("Drag the UI RawImage (or the 3D Plane) that shows the video here.")]
    public GameObject videoDisplayScreen; // <--- NEW: This controls the black screen visibility

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";
    public GameObject interactTextUI; // The "Press E" text

    private bool playerInRange = false;
    private bool hasActivated = false;

    void Start()
    {
        // 1. Hide the "Press E" text at start
        if (interactTextUI != null) interactTextUI.SetActive(false);

        // 2. Hide the Video Screen (Black box) at start
        if (videoDisplayScreen != null)
        {
            videoDisplayScreen.SetActive(false);
        }

        // 3. Setup the video to close automatically when done
        if (videoPlayer != null)
        {
            videoPlayer.Stop(); // Ensure it's not playing
            videoPlayer.loopPointReached += OnVideoFinished; // Listen for the end
        }
    }

    void Update()
    {
        if (playerInRange && !hasActivated && Input.GetKeyDown(interactKey))
        {
            PlayTheVideo();
        }
    }

    void PlayTheVideo()
    {
        hasActivated = true;

        // 1. Turn ON the screen
        if (videoDisplayScreen != null)
        {
            videoDisplayScreen.SetActive(true);
        }

        // 2. Play the video
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }

        // 3. Hide the "Press E" text
        if (interactTextUI != null) interactTextUI.SetActive(false);
    }

    // Automatically called when video finishes
    void OnVideoFinished(VideoPlayer vp)
    {
        // Turn the screen OFF again so we can see the game
        if (videoDisplayScreen != null)
        {
            videoDisplayScreen.SetActive(false);
        }
    }

    // --- DETECTION LOGIC ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (interactTextUI != null && !hasActivated) interactTextUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactTextUI != null) interactTextUI.SetActive(false);
        }
    }
}