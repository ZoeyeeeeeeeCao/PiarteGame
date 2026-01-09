using UnityEngine;
using TMPro;
using UnityEngine.UI; // Required for the Image component
using System.Collections;

public class TutorialSubtitleTrigger : MonoBehaviour
{
    [Header("Dialogue Sequence")]
    public Dialogue[] dialogues;
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;
    private DialogueManager dialogueManager;

    [Header("Mission UI Slots")]
    [Tooltip("The Background Panel or Main Parent")]
    public GameObject missionBox;

    [Tooltip("The Text component")]
    public TextMeshProUGUI missionText;

    [Tooltip("The Icon/Image that was staying visible")]
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

        // Hide everything at start
        HideMissionUI();
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
        // 1. HIDE ALL PIECES
        HideMissionUI();

        for (int i = 0; i < dialogues.Length; i++)
        {
            if (dialogues[i] == null) continue;

            bool first = (i == 0);
            dialogueManager.StartDialogue(dialogues[i], DialogueManager.DialogueMode.Subtitle, first);

            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
        }

        dialogueManager.CloseDialogueBox();
        yield return new WaitForSeconds(0.5f);

        // 2. SHOW ALL PIECES
        ShowMission();
    }

    void HideMissionUI()
    {
        // We hide the GameObject specifically to ensure child images vanish
        if (missionBox != null) missionBox.SetActive(false);
        if (missionText != null) missionText.gameObject.SetActive(false);
        if (missionImageObject != null) missionImageObject.SetActive(false);
    }

    void ShowMission()
    {
        // Update the text string first
        if (missionText != null)
        {
            missionText.text = missionDescription;
            missionText.gameObject.SetActive(true); // Show text
        }

        // Show the icon
        if (missionImageObject != null)
        {
            missionImageObject.SetActive(true);
        }

        // Show the main background box
        if (missionBox != null)
        {
            missionBox.SetActive(true);
        }

        // Play the sound
        if (audioSource != null && missionSound != null)
        {
            audioSource.PlayOneShot(missionSound);
        }
    }
}