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

    [Header("Compass Integration")]
    [Tooltip("Reference to the Compass script")]
    public Compass compass;

    [Tooltip("Quest ID that matches the compass questPoint - e.g., 'Opening_Mission'")]
    public string compassQuestID = "";

    [Tooltip("Show compass marker when mission appears?")]
    public bool showCompassMarker = true;

    private DialogueManager dialogueManager;
    private bool hasTriggered = false;
    private bool missionActive = false;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (compass == null)
            compass = FindObjectOfType<Compass>();

        // Hide the main box and all mission texts
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
            // FIX: Enable animation and auto-close for subtitles
            dialogueManager.StartDialogue(
                subtitleDialogue,
                DialogueManager.DialogueMode.Subtitle,
                animate: true,      // Slide in
                autoClose: true     // Slide out when done
            );

            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }

            // Wait for slide-out animation to complete
            yield return new WaitForSeconds(0.5f);
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

        // Turn on the Shared Box
        if (sharedMissionBox != null)
        {
            sharedMissionBox.SetActive(true);
        }

        // Turn on and set each individual mission text
        foreach (var mission in missionList)
        {
            if (mission.textComponent != null)
            {
                mission.textComponent.text = mission.missionDescription;
                mission.textComponent.gameObject.SetActive(true);
            }
        }

        // Show compass marker when mission appears
        missionActive = true;
        if (compass != null && !string.IsNullOrEmpty(compassQuestID) && showCompassMarker)
        {
            compass.ShowMarker(compassQuestID);
            Debug.Log($"[MISSION] Compass marker shown for: {compassQuestID}");
        }
    }

    public void CompleteMission()
    {
        if (!missionActive)
        {
            Debug.LogWarning("[MISSION] CompleteMission called but mission is not active!");
            return;
        }

        missionActive = false;

        // Hide the mission UI
        if (sharedMissionBox != null)
        {
            sharedMissionBox.SetActive(false);
        }

        foreach (var mission in missionList)
        {
            if (mission.textComponent != null)
                mission.textComponent.gameObject.SetActive(false);
        }

        // Hide compass marker when mission completes
        if (compass != null && !string.IsNullOrEmpty(compassQuestID))
        {
            compass.HideMarker(compassQuestID);
            Debug.Log($"[MISSION] Compass marker hidden for: {compassQuestID}");
        }
    }

    public bool IsMissionActive()
    {
        return missionActive;
    }

    public void ShowCompassMarker()
    {
        if (compass != null && !string.IsNullOrEmpty(compassQuestID))
        {
            compass.ShowMarker(compassQuestID);
        }
    }

    public void HideCompassMarker()
    {
        if (compass != null && !string.IsNullOrEmpty(compassQuestID))
        {
            compass.HideMarker(compassQuestID);
        }
    }
}