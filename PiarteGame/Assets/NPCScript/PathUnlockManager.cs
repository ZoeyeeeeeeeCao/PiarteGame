using UnityEngine;
using TMPro;
using System.Collections;

public class PathUnlockManager : MonoBehaviour
{
    [Header("Requirements")]
    public GameObject[] npcsToWatch;

    [Header("Objects to Remove")]
    public GameObject[] blockingWalls;

    [Header("Subtitle Settings")]
    [Tooltip("Drag the TextMeshPro text used for subtitles (bottom of screen)")]
    public TextMeshProUGUI subtitleText;
    [Tooltip("The text that will appear when the path opens")]
    public string unlockSubtitle = "The barrier has faded... I can proceed now.";
    public float subtitleDuration = 3.0f;

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
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (missionUI != null) missionUI.SetActive(false);
        if (subtitleText != null) subtitleText.text = ""; // Clear subtitle at start
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

        // 2. Run the visual sequence
        StartCoroutine(ShowSubtitleThenMission());
    }

    IEnumerator ShowSubtitleThenMission()
    {
        // Wait a moment for the wall destruction to be noticed
        yield return new WaitForSeconds(0.5f);

        // Show Subtitle
        if (subtitleText != null)
        {
            subtitleText.text = unlockSubtitle;
            Debug.Log("Subtitle shown: " + unlockSubtitle);

            yield return new WaitForSeconds(subtitleDuration);

            subtitleText.text = ""; // Clear subtitle
        }

        // Show Mission UI
        ShowMissionUI();
    }

    void ShowMissionUI()
    {
        if (missionUI != null) missionUI.SetActive(true);

        if (missionText != null)
        {
            missionText.text = newMissionObjective;
            Canvas.ForceUpdateCanvases();
        }

        if (missionPopUpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(missionPopUpSound);
        }
    }
}