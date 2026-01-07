using UnityEngine;
using TMPro; // Needed for TextMeshPro
using System.Collections;

public class PathUnlockManager : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Drag the 3 NPCs here. The path opens when ALL of them are destroyed.")]
    public GameObject[] npcsToWatch;

    [Header("Objects to Remove")]
    [Tooltip("Drag the invisible wall objects here. They will be destroyed when unlocked.")]
    public GameObject[] blockingWalls;

    [Header("Mission UI Update (NEW)")]
    [Tooltip("Drag your Mission UI Panel here")]
    public GameObject missionUI;
    [Tooltip("Drag the TextMeshPro text that shows the current objective")]
    public TextMeshProUGUI missionText;
    [Tooltip("What should the mission text say after walls are gone?")]
    public string newMissionObjective = "Path Unlocked! Proceed to the next area.";

    [Header("Unlocking Event")]
    public Dialogue pathOpenedDialogue;

    private DialogueManager dialogueManager;
    private bool isUnlocked = false;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
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

        // 2. Update Mission UI Text (NEW)
        if (missionUI != null) missionUI.SetActive(true);
        if (missionText != null)
        {
            missionText.text = newMissionObjective;
            // Force update to prevent visual lag
            Canvas.ForceUpdateCanvases();
        }

        // 3. Start Dialogue
        if (pathOpenedDialogue.dialogueLines != null && pathOpenedDialogue.dialogueLines.Length > 0)
        {
            StartCoroutine(StartDialogueDelayed());
        }
    }

    IEnumerator StartDialogueDelayed()
    {
        // Wait 1 second so player sees the walls vanish/UI update first
        yield return new WaitForSeconds(1.0f);

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(pathOpenedDialogue);
        }
    }
}