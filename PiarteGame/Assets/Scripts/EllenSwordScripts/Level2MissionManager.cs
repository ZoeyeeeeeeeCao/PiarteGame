using UnityEngine;
using TMPro;
using System.Collections;

public class Level2MissionManager : MonoBehaviour
{
    [Header("Mission UI")]
    public GameObject missionBox;
    public TextMeshProUGUI missionText;
    public GameObject missionImageObject;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip missionUpdateSound;

    [Header("Compass Integration")]
    public Compass compass;
    public string leverMarkerID = "Lever_Marker";
    public string stairMarkerID = "Stair_Marker";
    public string peakBuildingMarkerID = "Peak_Building";

    [Header("Mission Triggers")]
    [Tooltip("Array of fog wall colliders - player can find any of them")]
    public GameObject[] fogWallTriggers;

    [Tooltip("Reference to the old man NPC GameObject")]
    public GameObject oldManNPC;

    [Tooltip("Array of stair colliders that lead to peak")]
    public GameObject[] stairTriggers;

    [Tooltip("Reference to the lever controller")]
    public LeverController leverController;

    [Header("Mission Texts")]
    [TextArea(2, 4)]
    public string leverMissionText = "Pull the lever to remove the fog walls.";
    [TextArea(2, 4)]
    public string stairMissionText = "Head to the stairs leading to the peak.";
    [TextArea(2, 4)]
    public string peakMissionText = "Enter the building at the peak.";

    // Internal state tracking
    private bool hasFoundFogWall = false;
    private bool hasTalkedToOldMan = false;
    private bool hasActivatedLever = false;
    private bool hasReachedStairs = false;
    private bool missionComplete = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (compass == null) compass = FindObjectOfType<Compass>();

        // Hide mission UI initially
        HideMissionUI();

        // Setup triggers
        SetupFogWallTriggers();
        SetupOldManListener();
        SetupStairTriggers();

        // Monitor lever state if assigned
        if (leverController != null)
        {
            StartCoroutine(MonitorLever());
        }
    }

    void SetupFogWallTriggers()
    {
        // Setup listener for ALL fog wall triggers
        foreach (GameObject fogWallTrigger in fogWallTriggers)
        {
            if (fogWallTrigger != null)
            {
                TriggerListener listener = fogWallTrigger.GetComponent<TriggerListener>();
                if (listener == null)
                    listener = fogWallTrigger.AddComponent<TriggerListener>();

                listener.triggerTag = "Player";
                listener.onTriggerEnter = OnFogWallFound;
            }
        }
    }

    void SetupOldManListener()
    {
        if (oldManNPC != null)
        {
            // Check for TwoWayInteraction component
            TwoWayInteraction interaction = oldManNPC.GetComponent<TwoWayInteraction>();
            if (interaction != null)
            {
                StartCoroutine(MonitorOldManConversation(interaction));
            }
        }
    }

    void SetupStairTriggers()
    {
        foreach (GameObject stairTrigger in stairTriggers)
        {
            if (stairTrigger != null)
            {
                TriggerListener listener = stairTrigger.GetComponent<TriggerListener>();
                if (listener == null)
                    listener = stairTrigger.AddComponent<TriggerListener>();

                listener.triggerTag = "Player";
                listener.onTriggerEnter = OnStairsReached;
            }
        }
    }

    // Called when player finds ANY fog wall
    void OnFogWallFound(Collider other)
    {
        if (hasFoundFogWall || missionComplete) return;

        hasFoundFogWall = true;
        Debug.Log("[LEVEL2] Player found a fog wall");

        // SCENARIO 1: Found fog wall first (no old man yet)
        if (!hasTalkedToOldMan)
        {
            ShowLeverMission();
        }
        // SCENARIO 2: Already talked to old man, now found fog wall
        else
        {
            // Stair marker already showing, just add lever marker
            ShowLeverMarkerOnly();
        }
    }

    // Monitor old man conversation
    IEnumerator MonitorOldManConversation(TwoWayInteraction interaction)
    {
        while (!hasTalkedToOldMan && !missionComplete)
        {
            if (interaction.IsInteractionFinished())
            {
                hasTalkedToOldMan = true;
                Debug.Log("[LEVEL2] Player talked to old man");
                OnOldManTalked();
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    void OnOldManTalked()
    {
        // SCENARIO 2: Talked to old man first
        if (!hasFoundFogWall)
        {
            // Show stair marker immediately
            ShowStairMission();
        }
        // SCENARIO 1: Already found fog wall, lever mission showing
        else
        {
            // Keep lever mission active, stair marker will show after lever
        }
    }

    // Monitor lever state
    IEnumerator MonitorLever()
    {
        while (!hasActivatedLever && !missionComplete)
        {
            if (leverController.leverStatus)
            {
                hasActivatedLever = true;
                Debug.Log("[LEVEL2] Lever activated");
                OnLeverActivated();
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    void OnLeverActivated()
    {
        // Hide lever marker
        if (compass != null)
        {
            compass.HideMarker(leverMarkerID);
        }

        // SCENARIO 1: Lever done, no old man yet - show stair mission
        if (!hasTalkedToOldMan)
        {
            ShowStairMission();
        }
        // SCENARIO 2: Lever done, already talked to old man - stair marker already visible
        else
        {
            // Stair marker already showing, no action needed
            Debug.Log("[LEVEL2] Lever complete, stair marker already active");
        }
    }

    void OnStairsReached(Collider other)
    {
        if (hasReachedStairs || missionComplete) return;

        hasReachedStairs = true;
        Debug.Log("[LEVEL2] Player reached stairs");

        // Hide stair marker
        if (compass != null)
        {
            compass.HideMarker(stairMarkerID);
        }

        // Show peak building mission
        ShowPeakMission();
    }

    // Mission display methods
    void ShowLeverMission()
    {
        UpdateMissionUI(leverMissionText);

        if (compass != null)
        {
            compass.ShowMarker(leverMarkerID);
        }
    }

    void ShowLeverMarkerOnly()
    {
        // Don't update UI text, just show the lever marker
        if (compass != null)
        {
            compass.ShowMarker(leverMarkerID);
        }
        Debug.Log("[LEVEL2] Lever marker added (stair marker already visible)");
    }

    void ShowStairMission()
    {
        UpdateMissionUI(stairMissionText);

        if (compass != null)
        {
            compass.ShowMarker(stairMarkerID);
        }
    }

    void ShowPeakMission()
    {
        UpdateMissionUI(peakMissionText);

        if (compass != null)
        {
            compass.ShowMarker(peakBuildingMarkerID);
        }
    }

    void UpdateMissionUI(string newText)
    {
        if (missionText != null)
        {
            missionText.text = newText;
        }

        if (missionBox != null && !missionBox.activeSelf)
        {
            ShowMissionUI();
        }

        PlayMissionSound();
    }

    void ShowMissionUI()
    {
        if (missionText != null) missionText.gameObject.SetActive(true);
        if (missionImageObject != null) missionImageObject.SetActive(true);
        if (missionBox != null) missionBox.SetActive(true);
    }

    void HideMissionUI()
    {
        if (missionBox != null) missionBox.SetActive(false);
        if (missionText != null) missionText.gameObject.SetActive(false);
        if (missionImageObject != null) missionImageObject.SetActive(false);
    }

    void PlayMissionSound()
    {
        if (audioSource != null && missionUpdateSound != null)
        {
            audioSource.PlayOneShot(missionUpdateSound);
        }
    }

    // Public method to complete the entire mission (call from cutscene trigger)
    public void CompleteMission()
    {
        missionComplete = true;

        if (compass != null)
        {
            compass.HideMarker(leverMarkerID);
            compass.HideMarker(stairMarkerID);
            compass.HideMarker(peakBuildingMarkerID);
        }

        HideMissionUI();
        Debug.Log("[LEVEL2] Mission complete!");
    }

    // Helper class for trigger detection
    public class TriggerListener : MonoBehaviour
    {
        public string triggerTag = "Player";
        public System.Action<Collider> onTriggerEnter;
        private bool hasTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(triggerTag) && !hasTriggered)
            {
                hasTriggered = true;
                onTriggerEnter?.Invoke(other);
            }
        }
    }
}