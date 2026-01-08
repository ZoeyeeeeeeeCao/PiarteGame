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

        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !hasFinishedAll)
        {
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
            npcIndicator.SetActive(false); // Moved from the bottom to here

        // Loop through all dialogue parts
        for (int i = 0; i < dialogueParts.Length; i++)
        {
            bool isFirstPart = (i == 0);
            bool isLastPart = (i == dialogueParts.Length - 1);

            dialogueManager.StartDialogue(
                dialogueParts[i],
                DialogueManager.DialogueMode.Dialogue,
                animate: isFirstPart
            );

            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }

            if (!isLastPart)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Wait for player to press Enter to close
        Debug.Log("💬 Press Enter to close dialogue...");
        while (!Input.GetKeyDown(KeyCode.Return))
        {
            yield return null;
        }

        dialogueManager.CloseDialogueBox();
        yield return new WaitForSeconds(0.5f);

        hasFinishedAll = true;

        // (Optional) Remove the old reference at the bottom to keep code clean
        Debug.Log("✅ Full conversation sequence finished.");

        if (tutorialManager != null)
        {
            tutorialManager.OnDialogueComplete();
        }
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

            if (interactPrompt != null)
            {
                interactPrompt.SetActive(playerInRange && !hasFinishedAll && !dialogueManager.IsDialogueActive());
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}