using UnityEngine;

/// <summary>
/// Triggers BGM change when this collider is triggered by the player
/// Scene-local version (no singleton dependency)
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

    [Header("References (Optional)")]
    [Tooltip("If not assigned, will auto-find in scene")]
    public BGMZoneManager bgmZoneManager;

    private bool hasTriggered = false;

    private void Awake()
    {
        // Auto-find if not manually assigned
        if (bgmZoneManager == null)
        {
            bgmZoneManager = FindObjectOfType<BGMZoneManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (hasTriggered && triggerOnlyOnce) return;

        TriggerBGM();

        if (triggerOnlyOnce)
        {
            hasTriggered = true;
        }
    }

    private void TriggerBGM()
    {
        if (string.IsNullOrEmpty(bgmZoneName))
        {
            Debug.LogWarning("[BGMTriggerHelper] No BGM zone name specified!");
            return;
        }

        if (bgmZoneManager == null)
        {
            Debug.LogWarning("[BGMTriggerHelper] BGMZoneManager not found in scene!");
            return;
        }

        bgmZoneManager.PlayZoneByName(bgmZoneName);
        Debug.Log($"[BGMTriggerHelper] Changed music to: {bgmZoneName}");
        if (FindObjectOfType<PlayerHealthController>()?.IsDead == true)
            return;

    }
}
