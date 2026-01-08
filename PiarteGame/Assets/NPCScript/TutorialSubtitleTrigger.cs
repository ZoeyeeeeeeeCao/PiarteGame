using UnityEngine;

public class TutorialSubtitleTrigger : MonoBehaviour
{
    [Header("Dialogue Data")]
    public Dialogue dialogue;

    [Header("Settings")]
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    private DialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            if (dialogueManager != null)
            {
                // FIX: Stop current dialogue to clear old text buffer and prevent glitches
                dialogueManager.EndDialogue();

                // Play as Subtitle (Auto-advancing)
                dialogueManager.StartDialogue(dialogue, DialogueManager.DialogueMode.Subtitle);

                if (triggerOnlyOnce) hasTriggered = true;
            }
        }
    }
}