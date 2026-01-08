using UnityEngine;
using TMPro;
using System.Collections;

public class ReusableOpeningMission : MonoBehaviour
{
    [System.Serializable]
    public class MissionData
    {
        [Tooltip("The TextMeshPro object inside the mission box")]
        public TextMeshProUGUI textComponent;
        [Tooltip("What this specific mission should say")]
        [TextArea(2, 5)]
        public string missionDescription;
    }

    [Header("Main UI Container")]
    [Tooltip("Drag the shared Background Box / Panel here")]
    public GameObject sharedMissionBox;

    [Header("Missions List")]
    [Tooltip("Add your individual mission text objects here")]
    public MissionData[] missionList;

    [Header("Sequence Settings")]
    public bool autoStartOnLoad = true;
    public float startDelay = 0.5f;

    [Header("1. Subtitle Sequence")]
    public Dialogue subtitleDialogue;
    public bool playSubtitlesFirst = true;

    [Header("2. Audio Settings")]
    public AudioSource audioSource;
    public AudioClip missionAppearSound;

    private DialogueManager dialogueManager;
    private bool hasTriggered = false;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Initialization: Hide the main box and all mission texts
        if (sharedMissionBox != null) sharedMissionBox.SetActive(false);

        foreach (var mission in missionList)
        {
            if (mission.textComponent != null)
                mission.textComponent.gameObject.SetActive(false);
        }

        if (autoStartOnLoad)
        {
            StartCoroutine(WaitAndStart());
        }
    }

    IEnumerator WaitAndStart()
    {
        yield return new WaitForSeconds(startDelay);
        ExecuteMissionTrigger();
    }

    public void ExecuteMissionTrigger()
    {
        if (hasTriggered) return;
        hasTriggered = true;
        StartCoroutine(MissionSequence());
    }

    IEnumerator MissionSequence()
    {
        // Step 1: Subtitles (No freeze)
        if (playSubtitlesFirst && subtitleDialogue != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(subtitleDialogue, DialogueManager.DialogueMode.Subtitle);

            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }
        }

        // Step 2: Show everything and play sound
        ShowMissions();
    }

    void ShowMissions()
    {
        if (audioSource != null && missionAppearSound != null)
        {
            audioSource.PlayOneShot(missionAppearSound);
        }

        // 1. Turn on the Shared Box
        if (sharedMissionBox != null)
        {
            sharedMissionBox.SetActive(true);
        }

        // 2. Turn on and set each individual mission text
        foreach (var mission in missionList)
        {
            if (mission.textComponent != null)
            {
                mission.textComponent.text = mission.missionDescription;
                mission.textComponent.gameObject.SetActive(true);
            }
        }
    }
}