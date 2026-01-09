using UnityEngine;
using TMPro;
using System.Collections;

public class PathUnlockManager : MonoBehaviour
{
    [Header("Requirements")]
    public GameObject[] npcsToWatch;

    [Header("Objects to Remove")]
    public GameObject[] blockingWalls;

    [Header("Dialogue Integration")]
    [Tooltip("The dialogue asset to play when the path opens")]
    public Dialogue unlockDialogue;
    private DialogueManager dialogueManager;

    [Header("Mission UI Update")]
    public GameObject missionUI;
    public TextMeshProUGUI missionText;
    public string newMissionObjective = "Path Unlocked! Proceed to the next area.";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip missionPopUpSound;

    private bool isUnlocked = false;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (missionUI != null) missionUI.SetActive(false);
    }

    void Update()
    {
        if (isUnlocked) return;

        if (AreAllNPCsDestroyed())
        {
            UnlockPath();
        }
    }

    bool AreAllNPCsDestroyed()
    {
        if (npcsToWatch.Length == 0) return false;
        foreach (GameObject npc in npcsToWatch)
        {
            if (npc != null) return false;
        }
        return true;
    }

    void UnlockPath()
    {
        isUnlocked = true;

        // 1. Destroy Walls
        foreach (GameObject wall in blockingWalls)
        {
            if (wall != null) Destroy(wall);
        }

        // 2. Start the sequence that waits for the dialogue
        StartCoroutine(RunUnlockSequence());
    }

    IEnumerator RunUnlockSequence()
    {
        // Wait a tiny bit after wall destruction
        yield return new WaitForSeconds(0.5f);

        if (dialogueManager != null && unlockDialogue != null)
        {
            // Start the dialogue in Subtitle mode (so it auto-slides away)
            dialogueManager.StartDialogue(unlockDialogue, DialogueManager.DialogueMode.Subtitle);

            // WAIT until the DialogueManager says it's finished
            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }

            // Wait for the slide-out animation to finish completely
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Show Mission UI ONLY after dialogue is done
        ShowMissionUI();
    }

    void ShowMissionUI()
    {
        if (missionUI != null) missionUI.SetActive(true);

        if (missionText != null)
        {
            missionText.text = newMissionObjective;
        }

        if (missionPopUpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(missionPopUpSound);
        }
    }
}