using UnityEngine;

public class FourBowlPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Zones (All Must Be Solved)")]
    public InventoryPlacementTriggerZone[] zones;

    [Header("Door / Animation")]
    public Animator doorAnimator;
    public string doorOpenBool = "DoorOpen";

    [Header("Debug")]
    public bool debugLog;

    bool puzzleCompleted;

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

        if (debugLog) Debug.Log("[Puzzle] All zones solved! DoorOpen = true");

        if (doorAnimator)
            doorAnimator.SetBool(doorOpenBool, true);
        else
            Debug.LogWarning("[Puzzle] doorAnimator is not assigned.");
    }
}
