using UnityEngine;
using TMPro;
using System.Collections.Generic; // Needed for Lists

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [System.Serializable] // This makes it show up in Inspector
    public class Mission
    {
        public string missionName = "Mission Name";
        [TextArea] public string description = "What needs to be done";
        public int targetAmount = 1; // How many times to trigger?
        public int currentAmount = 0; // Private tracker
        public bool isCompleted = false;
    }

    [Header("UI Reference")]
    public TextMeshProUGUI missionText;

    [Header("Mission List")]
    // This allows you to add Mission 1, Mission 2, Mission 3 in Inspector
    public List<Mission> missions;

    // Tracks which mission in the list we are currently doing
    public int currentMissionIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateMissionUI();
    }

    // Call this to progress the CURRENT mission
    public void AddProgressToCurrent()
    {
        // Safety Check: Are we out of missions?
        if (currentMissionIndex >= missions.Count) return;

        Mission currentMission = missions[currentMissionIndex];

        // 1. Add Progress
        currentMission.currentAmount++;

        // 2. Check if Complete
        if (currentMission.currentAmount >= currentMission.targetAmount)
        {
            currentMission.currentAmount = currentMission.targetAmount;
            CompleteMission(currentMission);
        }

        UpdateMissionUI();
    }

    // Call this if you want to progress a SPECIFIC mission ID (optional)
    public void AddProgressToID(int missionID)
    {
        if (missionID < missions.Count)
        {
            Mission m = missions[missionID];
            if (!m.isCompleted)
            {
                m.currentAmount++;
                if (m.currentAmount >= m.targetAmount) CompleteMission(m);
                UpdateMissionUI();
            }
        }
    }

    void CompleteMission(Mission mission)
    {
        mission.isCompleted = true;
        Debug.Log("Mission Completed: " + mission.missionName);

        // Auto-advance to the next mission
        if (currentMissionIndex < missions.Count - 1)
        {
            currentMissionIndex++;
            Debug.Log("New Mission Started: " + missions[currentMissionIndex].missionName);
        }
        else
        {
            Debug.Log("ALL MISSIONS COMPLETED!");
            // Optional: You could trigger a "Level Complete" screen here
        }
    }

    void UpdateMissionUI()
    {
        // Case 1: All missions done
        if (currentMissionIndex >= missions.Count || (currentMissionIndex == missions.Count - 1 && missions[currentMissionIndex].isCompleted))
        {
            missionText.text = "<color=green><b>ALL MISSIONS COMPLETED!</b></color>";
            return;
        }

        // Case 2: Show current active mission
        Mission current = missions[currentMissionIndex];

        string displayText = "<b>Current Objective:</b>\n" + current.description;

        // ONLY show the count (0/5) if the target is greater than 0
        if (current.targetAmount > 0)
        {
            displayText += "\n<color=yellow>(" + current.currentAmount + " / " + current.targetAmount + ")</color>";
        }

        missionText.text = displayText;
    }
}