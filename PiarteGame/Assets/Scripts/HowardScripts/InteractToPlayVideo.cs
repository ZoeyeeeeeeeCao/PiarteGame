using UnityEngine;
using UnityEngine.Video;

public class InteractToPlayVideo : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public GameObject videoDisplayScreen; // Drag your Canvas/Panel here

    [Header("Enable After Video")]
    public GameObject objectToEnable;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";
    public GameObject interactTextUI;

    private bool playerInRange = false;
    private bool hasActivated = false;

    void Start()
    {
        // Initial setup
        if (interactTextUI != null) interactTextUI.SetActive(false);
        if (videoDisplayScreen != null) videoDisplayScreen.SetActive(false);
        if (objectToEnable != null) objectToEnable.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false; // Prevent it from starting early
            videoPlayer.loopPointReached += OnVideoFinished; // Setup end event
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
        if (interactTextUI != null) interactTextUI.SetActive(false);

        if (videoPlayer != null)
        {
            // CRITICAL: Prepare the video so it's ready before showing the UI
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += (vp) =>
            {
                if (videoDisplayScreen != null) videoDisplayScreen.SetActive(true);
                vp.Play();
            };
        }
        else
        {
            // Fallback: If no video is found, just finish immediately
            OnVideoFinished(null);
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (videoDisplayScreen != null) videoDisplayScreen.SetActive(false);

        // Turn on the portal/object
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
        }

        Debug.Log("Video finished. Enabling object and destroying this trigger.");
        Destroy(gameObject); // Now it's safe to destroy
    }

    // --- DETECTION ---
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