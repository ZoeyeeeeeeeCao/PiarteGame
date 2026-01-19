using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class ShipInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    public float interactionRadius = 5f;
    public string playerTag = "Player";

    [Header("Visuals")]
    public GameObject interactImage;

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public GameObject videoUICanvas;

    [Header("After Video")]
    public string nextSceneName = "CreditsScene";
    public bool allowSkip = true;

    [Header("Player Control")]
    public MonoBehaviour playerController;
    public MonoBehaviour locomotionController;

    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool isPlayingVideo = false;

    void Start()
    {
        if (interactImage != null) interactImage.SetActive(false);
        if (videoUICanvas != null) videoUICanvas.SetActive(false);

        if (videoPlayer != null)
        {
            // We removed the error-prone line. 
            // VideoPlayer follows Time.timeScale by default.
            videoPlayer.Stop();
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    void Update()
    {
        // Only allow skipping if the game is NOT paused (Time.timeScale > 0)
        if (isPlayingVideo && allowSkip && Time.timeScale > 0)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                EndCutsceneAndLoad();
                return;
            }
        }

        if (hasTriggered) return;

        CheckForPlayer();

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            StartVideoCutscene();
        }
    }

    void CheckForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius);
        bool currentlyInRange = false;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(playerTag))
            {
                currentlyInRange = true;
                break;
            }
        }

        if (currentlyInRange != playerInRange)
        {
            playerInRange = currentlyInRange;
            if (interactImage != null)
                interactImage.SetActive(playerInRange);
        }
    }

    void StartVideoCutscene()
    {
        hasTriggered = true;
        isPlayingVideo = true;

        if (interactImage != null) interactImage.SetActive(false);

        // Disable movement scripts so player can't walk away during video
        if (playerController != null) playerController.enabled = false;
        if (locomotionController != null) locomotionController.enabled = false;

        if (videoUICanvas != null) videoUICanvas.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
        else
        {
            EndCutsceneAndLoad();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // If the game is paused, wait to load the next scene until it's unpaused
        if (Time.timeScale > 0)
        {
            EndCutsceneAndLoad();
        }
    }

    void EndCutsceneAndLoad()
    {
        // Reset timeScale just in case, then load
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}