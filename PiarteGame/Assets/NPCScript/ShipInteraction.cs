using UnityEngine;
using UnityEngine.SceneManagement; // Required for changing scenes

public class ShipInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    public float interactionRadius = 5f;
    public string playerTag = "Player";
    public string nextSceneName = "CreditsScene"; // Name of your end scene

    [Header("Visuals")]
    public GameObject interactImage; // The "Press E to board ship" image

    private bool playerInRange = false;

    void Start()
    {
        // Ensure image is hidden at start
        if (interactImage != null) interactImage.SetActive(false);
    }

    void Update()
    {
        CheckForPlayer();

        // If in range and player presses E, move to the next scene
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            EndGame();
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

        // Toggle the prompt image based on range
        if (currentlyInRange != playerInRange)
        {
            playerInRange = currentlyInRange;
            if (interactImage != null) interactImage.SetActive(playerInRange);
        }
    }

    void EndGame()
    {
        Debug.Log("Boarding Ship... Loading next scene.");
        // Make sure the scene name is added to your Build Settings!
        SceneManager.LoadScene(nextSceneName);
    }

    // Visual helper for the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}