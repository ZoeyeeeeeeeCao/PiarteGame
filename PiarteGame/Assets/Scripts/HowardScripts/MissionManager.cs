using UnityEngine;
using TMPro; // Needed for TextMeshPro

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance; // Singleton so other scripts can find it easily

    [Header("UI Reference")]
    public TextMeshProUGUI missionText;

    [Header("Mission Settings")]
    public string missionDescription = "Gather Info from Villagers";
    public int totalVillagersToTalk = 2;

    // Private tracker
    private int currentCount = 0;

    void Awake()
    {
        // Set up the Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateMissionUI();
    }

    // Call this function from your Dialogue Script!
    public void AddProgress()
    {
        currentCount++;

        // Clamp it so it doesn't go over the total (e.g. 3/2)
        if (currentCount > totalVillagersToTalk)
            currentCount = totalVillagersToTalk;

        UpdateMissionUI();

        if (currentCount == totalVillagersToTalk)
        {
            MissionComplete();
        }
    }

    void UpdateMissionUI()
    {
        if (currentCount >= totalVillagersToTalk)
        {
            missionText.text = "<b>Mission:</b> " + missionDescription + "\n" +
                               "<color=green>COMPLETED!</color>";
        }
        else
        {
            missionText.text = "<b>Mission:</b> " + missionDescription + "\n" +
                               "(" + currentCount + " / " + totalVillagersToTalk + ")";
        }
    }

    void MissionComplete()
    {
        Debug.Log("Mission Finished! You can now unlock the gate or spawn enemies.");
        // Add logic here later (e.g., Open a Gate)
    }
}