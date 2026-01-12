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
        // Prevent mission UI from overlapping
        if (missionBox != null) missionBox.SetActive(false);

        for (int i = 0; i < dialogues.Length; i++)
        {
            if (dialogues[i] == null) continue;

            bool isFirst = (i == 0);
            bool isLast = (i == dialogues.Length - 1);

            // Pass isLast to autoClose so the box stays open until the sequence is done
            dialogueManager.StartDialogue(dialogues[i], DialogueManager.DialogueMode.Subtitle, isFirst, isLast);

            // Wait for manager to finish current asset
            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }

            // Tiny delay between assets while box stays static
            if (!isLast) yield return new WaitForSeconds(0.1f);
        }

        // Wait for the final slide-down animation to finish
        yield return new WaitForSeconds(0.6f);

        if (missionBox != null)
        {
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