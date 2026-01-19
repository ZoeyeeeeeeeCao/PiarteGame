using UnityEngine;

public class FourBowlPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Zones (All Must Be Solved)")]
    public InventoryPlacementTriggerZone[] zones;

    [Header("On Complete: Load Scene")]
    [Tooltip("Scene name to load when the puzzle is completed")]
    public string sceneToLoad;

    [Header("Compass Integration")]
    [Tooltip("If true, hides all active markers when puzzle complete")]
    public bool hideAllMarkersOnComplete = true;
    public Compass compass;

    [Header("Debug")]
    public bool debugLog;

    private bool puzzleCompleted;

    private void Awake()
    {
        // Auto-find compass if not assigned
        if (compass == null)
            compass = FindObjectOfType<Compass>();
    }

    void Update()
    {
        if (puzzleCompleted) return;
        if (zones == null || zones.Length == 0) return;

        // if ANY zone not solved => not completed
        for (int i = 0; i < zones.Length; i++)
        {
            if (!zones[i])
            {
                if (debugLog) Debug.LogWarning("[Puzzle] zones has a null reference.");
                return;
            }

            if (!zones[i].IsSolved)
                return;
        }

        // ✅ all solved
        puzzleCompleted = true;

        if (debugLog) Debug.Log("[Puzzle] All zones solved! Completing puzzle...");

        // ✅ Hide all active compass markers when puzzle complete
        if (compass != null && hideAllMarkersOnComplete)
        {
            compass.HideAllMarkers();
            if (debugLog) Debug.Log("[Puzzle] All compass markers hidden");
        }

        // ✅ Load next scene using SceneLoaderHoward
        if (SceneLoaderHoward.Instance != null)
        {
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                if (debugLog) Debug.Log($"[Puzzle] Loading scene: {sceneToLoad}");

                // 🔇 Turn off BGM before loading
                AudioManager audioManager = FindObjectOfType<AudioManager>();
                if (audioManager != null)
                {
                    audioManager.MuteMusicImmediate();
                }
                else
                {
                    Debug.LogWarning("[Puzzle] AudioManager not found. Music not muted.");
                }

                SceneLoaderHoward.Instance.LoadLevel(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("[Puzzle] sceneToLoad is empty. Please set the scene name in the Inspector.");
            }
        }
        else
        {
            Debug.LogWarning("[Puzzle] SceneLoaderHoward.Instance is null. Make sure SceneLoaderHoward exists in the scene and is initialized.");
        }

    }
}
