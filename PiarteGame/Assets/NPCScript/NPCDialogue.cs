using UnityEngine;
using System.Collections;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Data")]
    public Dialogue dialogue;

    [Header("Detection Settings")]
    public float detectionRadius = 3f;
    public string playerTag = "Player";

    [Header("Visual Indicators")]
    public GameObject particleEffect; // The "Talk to me" indicator
    public GameObject interactImage;  // NEW: The "Press E" image/prompt

    [Header("Tutorial Integration")]
    [Tooltip("Drag ANY tutorial manager here (Herb, Parkour, or Combat)")]
    public TutorialManagerBase tutorialManager;

    private bool playerInRange = false;
    private bool hasInteracted = false;
    private DialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager == null)
            Debug.LogError("DialogueManager not found in the scene!");

        // Show indicator at start
        if (particleEffect != null)
            particleEffect.SetActive(true);

        // Hide interaction image at start
        if (interactImage != null)
            interactImage.SetActive(false);
    }

    void Update()
    {
        CheckForPlayer();

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (dialogueManager != null && !dialogueManager.IsDialogueActive())
            {
                TriggerDialogue();
            }
        }
    }

    void TriggerDialogue()
    {
        if (dialogueManager == null || dialogue == null) return;

        // 1. Start the conversation
        dialogueManager.StartDialogue(dialogue);

        // 2. Hide indicators immediately when speaking starts
        if (interactImage != null)
            interactImage.SetActive(false);

        // 3. Handle one-time events
        if (!hasInteracted)
        {
            hasInteracted = true;

            // Turn off the particle effect forever
            if (particleEffect != null)
                particleEffect.SetActive(false);

            if (tutorialManager != null)
            {
                StartCoroutine(WaitForDialogueToEnd());
            }
        }
    }

    IEnumerator WaitForDialogueToEnd()
    {
        while (dialogueManager.IsDialogueActive())
        {
            yield return null;
        }

        Debug.Log("Dialogue finished. Triggering Tutorial.");
        tutorialManager.OnDialogueComplete();
    }

    void CheckForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
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

            // Only toggle the image if the NPC hasn't been "completed" yet
            // and if a dialogue isn't currently playing
            UpdateInteractVisuals();
        }
    }

    void UpdateInteractVisuals()
    {
        if (interactImage == null) return;

        // Show image only if: Player is in range AND haven't finished the interaction
        if (playerInRange && !hasInteracted)
        {
            interactImage.SetActive(true);
        }
        else
        {
            interactImage.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}