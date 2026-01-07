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
        // Check if the player entered and we haven't triggered yet
        if (other.CompareTag("Player") && !hasTriggered)
        {
            if (dialogueManager != null)
            {
                // Play as Subtitle (Auto-advancing, no Enter key)
                dialogueManager.StartDialogue(dialogue, DialogueManager.DialogueMode.Subtitle);

                if (triggerOnlyOnce) hasTriggered = true;
            }
        }
    }
}