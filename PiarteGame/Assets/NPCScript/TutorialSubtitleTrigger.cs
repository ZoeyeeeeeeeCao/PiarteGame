using UnityEngine;
using System.Collections;

public class TutorialSubtitleTrigger : MonoBehaviour
{
    public Dialogue[] dialogues;
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
            if (dialogueManager != null && dialogues.Length > 0)
            {
                StartCoroutine(PlayDialogueSequence());
                if (triggerOnlyOnce) hasTriggered = true;
            }
        }
    }

    IEnumerator PlayDialogueSequence()
    {
        for (int i = 0; i < dialogues.Length; i++)
        {
            if (dialogues[i] == null) continue;

            // 1. Only animate (Slide In) on the VERY FIRST dialogue asset
            bool first = (i == 0);

            // 2. IMPORTANT: Tell the Manager NOT to auto-close 
            // We set autoClose to FALSE for every dialogue in the list
            dialogueManager.StartDialogue(dialogues[i], DialogueManager.DialogueMode.Subtitle, first, false);

            // Wait for the current lines to finish
            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }

            // Small gap where the box stays OPEN and STILL
            yield return new WaitForSeconds(0.1f);
        }

        // 3. NOW that the whole array is done, we manually close it
        dialogueManager.CloseDialogueBox();
    }
}