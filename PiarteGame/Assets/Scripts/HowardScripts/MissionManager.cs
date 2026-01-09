using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [System.Serializable]
    public class Mission
    {
        public string missionName = "Mission Name";
        [TextArea] public string description = "What needs to be done";
        public int targetAmount = 1;
        public int currentAmount = 0;
        public bool isCompleted = false;
    }

    [Header("UI References")]
    public GameObject missionBoxUI;
    public TextMeshProUGUI missionText;

    [Header("Mission List")]
    public List<Mission> missions;
    public int currentMissionIndex = 0;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip missionUpdatedSound;
    public AudioClip missionCompleteSound;
    public AudioClip missionAppearSound;

    private bool isSystemActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Ensure everything is hidden until a trigger is hit
        if (missionBoxUI != null) missionBoxUI.SetActive(false);
        if (missionText != null) missionText.text = "";
    }

    // This is called by your Reusable Triggers
    public void StartNextMission()
    {
        // 1. If the system is totally off, turn it on for the first time (Index 0)
        if (!isSystemActive)
        {
            isSystemActive = true;
            if (missionBoxUI != null) missionBoxUI.SetActive(true);
            PlayMissionSound(missionAppearSound);
            UpdateMissionUI();
            return;
        }

        // 2. If already active, move to the next index in the list
        if (currentMissionIndex < missions.Count - 1)
        {
            currentMissionIndex++;
            PlayMissionSound(missionAppearSound); // Sound for new objective
            UpdateMissionUI();
        }
    }

    public void AddProgressToCurrent()
    {
        if (!isSystemActive || currentMissionIndex >= missions.Count) return;

        Mission currentMission = missions[currentMissionIndex];
        currentMission.currentAmount++;

        if (currentMission.currentAmount >= currentMission.targetAmount)
        {
            currentMission.currentAmount = currentMission.targetAmount;
            CompleteMission(currentMission);
        }
        else
        {
            PlayMissionSound(missionUpdatedSound);
        }

        UpdateMissionUI();
    }

    void CompleteMission(Mission mission)
    {
        mission.isCompleted = true;
        PlayMissionSound(missionCompleteSound);

        // Optional: Auto-advance index after completion
        if (currentMissionIndex < missions.Count - 1)
        {
            currentMissionIndex++;
            UpdateMissionUI();
        }
    }

    void UpdateMissionUI()
    {
        if (missionText == null || !isSystemActive) return;

        if (currentMissionIndex >= missions.Count || (currentMissionIndex == missions.Count - 1 && missions[currentMissionIndex].isCompleted))
        {
            missionText.text = "<color=green><b>ALL MISSIONS COMPLETED!</b></color>";
            return;
        }

        Mission current = missions[currentMissionIndex];
        string displayText = "<b>Current Objective:</b>\n" + current.description;

        if (current.targetAmount > 1)
        {
            displayText += "\n<color=yellow>(" + current.currentAmount + " / " + current.targetAmount + ")</color>";
        }

        missionText.text = displayText;
    }

    private void PlayMissionSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}