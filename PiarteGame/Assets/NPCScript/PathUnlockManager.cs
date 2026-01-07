using UnityEngine;
using TMPro;
using System.Collections;

public class PathUnlockManager : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Drag the 3 NPCs here. The path opens when ALL of them are destroyed.")]
    public GameObject[] npcsToWatch;

    [Header("Objects to Remove")]
    [Tooltip("Drag the invisible wall objects here. They will be destroyed when unlocked.")]
    public GameObject[] blockingWalls;

    [Header("Mission UI Update")]
    [Tooltip("Drag your Mission UI Panel here")]
    public GameObject missionUI;
    [Tooltip("Drag the TextMeshPro text that shows the current objective")]
    public TextMeshProUGUI missionText;
    [Tooltip("What should the mission text say after walls are gone?")]
    public string newMissionObjective = "Path Unlocked! Proceed to the next area.";

    [Header("Audio")]
    [Tooltip("Audio source for playing sounds")]
    public AudioSource audioSource;
    [Tooltip("Sound to play when mission text appears")]
    public AudioClip missionPopUpSound;

    [Header("Unlocking Event")]
    public Dialogue pathOpenedDialogue;
    private DialogueManager dialogueManager;

    private bool isUnlocked = false;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();

        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Hide mission UI at start
        if (missionUI != null)
            missionUI.SetActive(false);
    }

    void Update()
    {
        if (isUnlocked) return;

        // Check if all NPCs are destroyed
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
            // If any NPC in the list still exists, return false
            if (npc != null)
            {
                return false;
            }
        }
        return true;
    }

    void UnlockPath()
    {
        isUnlocked = true;
        Debug.Log("🔓 All NPCs defeated! Opening path...");

        // 1. Destroy the Invisible Walls
        foreach (GameObject wall in blockingWalls)
        {
            if (wall != null) Destroy(wall);
        }

        // 2. Start Dialogue first
        if (pathOpenedDialogue != null && pathOpenedDialogue.dialogueLines != null && pathOpenedDialogue.dialogueLines.Length > 0)
        {
            StartCoroutine(StartDialogueAndMission());
        }
        else
        {
            // No dialogue, show mission UI immediately
            ShowMissionUI();
        }
    }

    IEnumerator StartDialogueAndMission()
    {
        // Wait 1 second so player sees the walls vanish first
        yield return new WaitForSeconds(1.0f);

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(pathOpenedDialogue);

            // Wait for dialogue to finish
            while (dialogueManager.IsDialogueActive())
            {
                yield return null;
            }

            Debug.Log("✅ Dialogue finished! Showing mission UI...");

            // Wait a tiny bit before showing mission UI
            yield return new WaitForSeconds(0.1f);

            // Show mission UI after dialogue ends
            ShowMissionUI();
        }
    }

    void ShowMissionUI()
    {
        // Show mission UI
        if (missionUI != null)
            missionUI.SetActive(true);

        // Update mission text
        if (missionText != null)
        {
            missionText.text = newMissionObjective;
            // Force update to prevent visual lag
            Canvas.ForceUpdateCanvases();
        }

        // Play sound when mission text pops up
        if (missionPopUpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(missionPopUpSound);
        }

        Debug.Log("🎯 Mission UI updated: " + newMissionObjective);
    }
}