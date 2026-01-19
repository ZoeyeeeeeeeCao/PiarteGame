using UnityEngine;

/// <summary>
/// Triggers BGM change when this collider is triggered by the player
/// Add this to any trigger that should also change music
/// </summary>
public class BGMTriggerHelper : MonoBehaviour
{
    [Header("BGM Settings")]
    [Tooltip("Name of the BGM zone to play")]
    public string bgmZoneName = "";

    [Tooltip("Only trigger once")]
    public bool triggerOnlyOnce = true;

    [Tooltip("Player tag to detect")]
    public string playerTag = "Player";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !hasTriggered)
        {
            TriggerBGM();

            if (triggerOnlyOnce)
            {
                hasTriggered = true;
            }
        }
    }

    void TriggerBGM()
    {
        if (string.IsNullOrEmpty(bgmZoneName))
        {
            Debug.LogWarning("[BGM Trigger] No BGM zone name specified!");
            return;
        }

        if (BGMZoneManager.Instance != null)
        {
            BGMZoneManager.Instance.PlayZoneByName(bgmZoneName);
            Debug.Log($"[BGM Trigger] Changed music to: {bgmZoneName}");
        }
        else
        {
            Debug.LogWarning("[BGM Trigger] BGMZoneManager not found!");
        }
    }
}