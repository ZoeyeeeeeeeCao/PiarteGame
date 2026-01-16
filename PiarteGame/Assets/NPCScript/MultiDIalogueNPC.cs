using UnityEngine;
using System.Collections;

public class GeneralMultiDialogueNPC : MonoBehaviour
{
    [Header("Conversation Sequence")]
    [Tooltip("Add multiple Dialogue assets here. They will play one after another.")]
    public Dialogue[] dialogueParts;

    [Header("Detection Settings")]
    public float detectionRadius = 4f;
    public string playerTag = "Player";

    [Header("UI & Visuals")]
    public GameObject interactPrompt; // The "Press E" UI
    public GameObject npcIndicator;   // Optional: Floating icon over NPC

    [Header("Tutorial Integration (Optional)")]
    [Tooltip("If this NPC starts a tutorial, drag the tutorial manager here")]
    public TutorialManagerBase tutorialManager;

    [Header("Dialogue Progression")]
    [Tooltip("The key used to advance the dialogue to the next part.")]
    public KeyCode nextPartKey = KeyCode.Space; // Key to advance between dialogue parts

    private DialogueManager dialogueManager;
    private bool playerInRange = false;
    private bool hasFinishedAll = false;
    private int currentPartIndex = 0;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager == null)
            Debug.LogError("Missing DialogueManager in scene!");

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Update()
    {
        CheckDistance();

        // Start new conversation: Check for 'E' press and ensures no dialogue is currently active
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !hasFinishedAll)
        {
            // Only start the sequence if the DialogueManager isn't already running something
            if (!dialogueManager.IsDialogueActive())
            {
                StartCoroutine(RunConversationSequence());
            }
        }
    }

    IEnumerator RunConversationSequence()
    {
        // 1. IMMEDIATELY HIDE VISUALS WHEN INTERACTION STARTS
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        if (npcIndicator != null)
            npcIndicator.SetActive(false);

        // Loop through all dialogue parts
        for (int i = 0; i < dialogueParts.Length; i++)
        {
            bool isFirstPart = (i == 0);

            // --- START DIALOGUE PART ---
            dialogueManager.StartDialogue(
                dialogueParts[i],
                DialogueManager.DialogueMode.Dialogue,
                animate: isFirstPart // Only animate the first part's text fully
            );

            // Wait until the DialogueManager says the current dialogue part is complete.
            // This is the VITAL part that needs to align with your DialogueManager's logic.
            // If IsDialogueActive() only turns false when the dialogue box closes, this is fine.
            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }

            // --- INTER-PART PAUSE AND INPUT WAIT ---
            // If it's NOT the last part, wait for the player to press a key to continue to the next part.
            if (i < dialogueParts.Length - 1)
            {
                // This assumes your DialogueManager hides the box temporarily after a part, 
                // but you want to ensure a player press before starting the next one.

                // You might need a way to visually prompt the player here (e.g., a "Continue" text).
                Debug.Log($"💬 Press {nextPartKey} to continue to part {i + 2} of the conversation...");

                bool advance = false;
                while (!advance)
                {
                    if (Input.GetKeyDown(nextPartKey))
                    {
                        advance = true;
                    }
                    yield return null;
                }
            }
        }

        // --- END OF ENTIRE SEQUENCE ---

        // This old logic used KeyCode.Return to close the box, but if your DialogueManager 
        // already handled the closing on the last line, we just need a delay before setting the flag.

        // If your DialogueManager DOESN'T close the box automatically after the last line, 
        // you should put the KeyCode.Return close wait here.

        // For now, assuming DialogueManager closed the box after the final part's final line:

        yield return new WaitForSeconds(0.5f);

        hasFinishedAll = true;
        Debug.Log("✅ Full conversation sequence finished.");

        // Call tutorial manager hook if it exists
        if (tutorialManager != null)
        {
            tutorialManager.OnDialogueComplete();
        }

        // Ensure prompt is hidden now that conversation is over
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void CheckDistance()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null) return;

        float distance = Vector3.Distance(transform.position, playerObj.transform.position);
        bool inRange = distance <= detectionRadius;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;

            // Only show prompt if in range, hasn't finished conversation, AND dialogue is not currently running
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(playerInRange && !hasFinishedAll && !dialogueManager.IsDialogueActive());
            }

            // Also manage the indicator
            if (npcIndicator != null)
            {
                // Only show indicator if in range and hasn't finished
                npcIndicator.SetActive(playerInRange && !hasFinishedAll && !dialogueManager.IsDialogueActive());
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}