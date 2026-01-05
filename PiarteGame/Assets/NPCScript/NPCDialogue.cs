using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public Dialogue dialogue;

    [Header("Detection Settings")]
    public float detectionRadius = 3f;
    public string playerTag = "Player"; // Using tag instead of layer

    [Header("Glow Effect")]
    public GameObject glowEffect; // Assign a child object with glow sprite/particle

    private bool playerInRange = false;
    private DialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (glowEffect != null)
            glowEffect.SetActive(false);

        Debug.Log("NPC Dialogue Started on: " + gameObject.name);
    }

    void Update()
    {
        CheckForPlayer();

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E pressed! Player in range!");

            if (dialogueManager != null && dialogue != null)
            {
                Debug.Log("Starting dialogue...");
                dialogueManager.StartDialogue(dialogue);
            }
            else
            {
                if (dialogueManager == null) Debug.LogError("DialogueManager not found!");
                if (dialogue == null) Debug.LogError("No dialogue assigned to NPC!");
            }
        }
    }

    void CheckForPlayer()
    {
        // Find all colliders in range (3D VERSION)
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        bool foundPlayer = false;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(playerTag))
            {
                foundPlayer = true;

                if (!playerInRange)
                {
                    playerInRange = true;
                    Debug.Log("Player entered range!");

                    if (glowEffect != null)
                        glowEffect.SetActive(true);
                }
                break;
            }
        }

        if (!foundPlayer && playerInRange)
        {
            playerInRange = false;
            Debug.Log("Player left range!");

            if (glowEffect != null)
                glowEffect.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}