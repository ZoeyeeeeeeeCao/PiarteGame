using UnityEngine;

public class HerbCollectableTutorial : MonoBehaviour
{
    [Header("Settings")]
    public float collectionRadius = 2f;
    public GameObject interactPrompt; // This is your image/UI that shows in range

    [Header("Audio")]
    public AudioClip pickupSound;
    [Range(0, 1)] public float volume = 1f;

    private HerbTutorialSystem manager;
    private bool playerInRange = false;

    void Start()
    {
        manager = FindObjectOfType<HerbTutorialSystem>();

        // Ensure the prompt image is hidden at the start
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update()
    {
        CheckPlayer();

        // Only allow collection if the player is in range and presses E
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Collect();
        }
    }

    void CheckPlayer()
    {
        // Detect player using a sphere overlap
        Collider[] hits = Physics.OverlapSphere(transform.position, collectionRadius);
        bool inRange = false;
        foreach (var h in hits)
        {
            if (h.CompareTag("Player")) inRange = true;
        }

        // Only toggle the image if the range state actually changes
        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(playerInRange);
            }
        }
    }

    void Collect()
    {
        // 1. Notify the Tutorial Manager
        if (manager != null) manager.OnHerbCollected();

        // 2. Play the Pickup Audio
        // We play at position so the sound continues even after this object is destroyed
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, volume);
        }

        // 3. Cleanup UI
        if (interactPrompt != null) interactPrompt.SetActive(false);

        // 4. Destroy the herb
        Destroy(gameObject);
    }

    // Visual aid in the editor to see the collection range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }
}