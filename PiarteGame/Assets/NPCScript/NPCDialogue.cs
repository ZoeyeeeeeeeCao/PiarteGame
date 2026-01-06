using UnityEngine;
using System.Collections;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Data")]
    public Dialogue dialogue;

    [Header("Detection Settings")]
    public float detectionRadius = 3f;
    public string playerTag = "Player";

    [Header("Visuals")]
    public GameObject particleEffect; // The "Talk to me" indicator

    [Header("Tutorial Integration")]
    [Tooltip("Drag ANY tutorial manager here (Herb, Parkour, or Combat)")]
    // CHANGED: Now accepts the parent class, so it works for ALL mission types
    public TutorialManagerBase tutorialManager;

    private bool playerInRange = false;
    private bool hasInteracted = false;
    private DialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager == null)
            Debug.LogError("DialogueManager not found in the scene!");

        // Show particle at start
        if (particleEffect != null)
            particleEffect.SetActive(true);
    }

    void Update()
    {
        CheckForPlayer();

        // Check for "E" press, but ONLY if player is in range AND dialogue isn't already running
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

        Debug.Log("Interacting with " + gameObject.name);

        // 1. Start the conversation
        dialogueManager.StartDialogue(dialogue);

        // 2. Handle one-time events (like Tutorials)
        if (!hasInteracted)
        {
            hasInteracted = true;

            // Turn off the particle effect forever
            if (particleEffect != null)
                particleEffect.SetActive(false);

            // If there is a tutorial attached, wait for the talk to finish
            if (tutorialManager != null)
            {
                StartCoroutine(WaitForDialogueToEnd());
            }
        }
    }

    IEnumerator WaitForDialogueToEnd()
    {
        // Wait while the dialogue box is still open
        while (dialogueManager.IsDialogueActive())
        {
            yield return null;
        }

        // Now that the box is closed, start the tutorial
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

        // Logic to detect enter/exit only once
        if (currentlyInRange != playerInRange)
        {
            playerInRange = currentlyInRange;
            if (playerInRange) Debug.Log("Player entered range.");
            else Debug.Log("Player left range.");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}