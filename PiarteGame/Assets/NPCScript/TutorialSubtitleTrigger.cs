using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialSubtitleTrigger : MonoBehaviour
{
    [Header("Dialogue Sequence")]
    public Dialogue[] dialogues;
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;
    private DialogueManager dialogueManager;

    [Header("Mission UI (Optional)")]
    public GameObject missionBox;
    public TextMeshProUGUI missionText;
    public GameObject missionImageObject;

    [Header("Mission Content")]
    [TextArea(2, 4)]
    public string missionDescription = "New Objective: Explore the area.";

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip missionSound;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (missionBox != null) HideMissionUI();
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
        Debug.Log($"[TRIGGER] PlayDialogueSequence started. Total dialogues: {dialogues.Length}");

        if (missionBox != null) missionBox.SetActive(false);

        for (int i = 0; i < dialogues.Length; i++)
        {
            if (dialogues[i] == null) continue;

            bool isLast = (i == dialogues.Length - 1);

            Debug.Log($"[TRIGGER] Starting dialogue {i + 1}/{dialogues.Length}. isLast: {isLast}");

            dialogueManager.StartDialogue(
                dialogues[i],
                DialogueManager.DialogueMode.Subtitle,
                animate: true,
                autoClose: true
            );

            Debug.Log($"[TRIGGER] Waiting for dialogue {i + 1} to finish...");
            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }
            Debug.Log($"[TRIGGER] Dialogue {i + 1} finished!");

            if (!isLast)
            {
                Debug.Log($"[TRIGGER] Waiting 0.5s before next dialogue...");
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log($"[TRIGGER] All dialogues complete. Waiting 0.3s before mission UI...");
        yield return new WaitForSeconds(0.3f);

        if (missionBox != null)
        {
            Debug.Log($"[TRIGGER] Showing mission UI.");
            ShowMissionFinal();
        }
    }

    void ShowMissionFinal()
    {
        if (missionText != null) missionText.text = missionDescription;
        if (missionText != null) missionText.gameObject.SetActive(true);
        if (missionImageObject != null) missionImageObject.SetActive(true);
        if (missionBox != null) missionBox.SetActive(true);

        if (audioSource != null && missionSound != null)
        {
            audioSource.PlayOneShot(missionSound);
        }
    }

    void HideMissionUI()
    {
        if (missionBox != null) missionBox.SetActive(false);
        if (missionText != null) missionText.gameObject.SetActive(false);
        if (missionImageObject != null) missionImageObject.SetActive(false);
    }
}