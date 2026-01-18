using UnityEngine;
using UnityEngine.Video;

public class StoneInteraction : MonoBehaviour
{
    [Header("Mission Requirements")]
    public GameObject captainPirate; // Drag the Captain Pirate NPC here
    private bool isCaptainDead = false;

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public GameObject videoDisplayCanvas;

    [Header("Level Progression")]
    public Collider nextLevelCollider;

    [Header("Visuals")]
    public GameObject stoneVisualMesh;

    [Header("Interaction")]
    public string playerTag = "Player";
    public GameObject interactTextUI;
    public string needToKillCaptainMessage = "Kill the Captain first!";

    private bool playerInRange = false;
    private bool hasActivated = false;

    private void OnEnable()
    {
        // Listen for ANY enemy death
        EnemyHealthController.EnemyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealthController.EnemyDied -= OnEnemyDied;
    }

    void OnEnemyDied(EnemyHealthController enemy)
    {
        // Check if the enemy that just died is the Captain we assigned
        if (enemy.gameObject == captainPirate)
        {
            isCaptainDead = true;
            Debug.Log("Captain is dead! Stone is now collectable.");
        }
    }

    void Start()
    {
        if (interactTextUI) interactTextUI.SetActive(false);
        if (videoDisplayCanvas) videoDisplayCanvas.SetActive(false);
        if (nextLevelCollider) nextLevelCollider.enabled = false;

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    void Update()
    {
        if (playerInRange && !hasActivated && Input.GetKeyDown(KeyCode.E))
        {
            // ONLY start the sequence if the Captain is dead
            if (isCaptainDead)
            {
                StartStoneSequence();
            }
            else
            {
                Debug.Log(needToKillCaptainMessage);
                // You could also update a UI text here to tell the player
            }
        }
    }

    void StartStoneSequence()
    {
        hasActivated = true;
        if (stoneVisualMesh) stoneVisualMesh.SetActive(false);
        if (nextLevelCollider) nextLevelCollider.enabled = true;
        if (interactTextUI) interactTextUI.SetActive(false);

        if (videoDisplayCanvas) videoDisplayCanvas.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += (vp) => { vp.Play(); };
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (videoDisplayCanvas) videoDisplayCanvas.SetActive(false);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !hasActivated)
        {
            playerInRange = true;
            // Only show "Press E" if they can actually collect it
            if (interactTextUI && isCaptainDead) interactTextUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactTextUI) interactTextUI.SetActive(false);
        }
    }
}