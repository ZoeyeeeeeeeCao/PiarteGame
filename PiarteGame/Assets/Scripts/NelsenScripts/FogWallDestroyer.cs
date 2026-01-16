using UnityEngine;

public class FogWallDestroyer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag all the fog wall GameObjects you want to remove into this list.")]
    public GameObject[] fogWalls;

    [Tooltip("Should there be an optional delay before they disappear?")]
    public float delay = 0f;

    /// <summary>
    /// Call this function from your CutsceneController's UnityEvent.
    /// </summary>
    public void DestroyWalls()
    {
        if (fogWalls == null || fogWalls.Length == 0)
        {
            Debug.LogWarning("FogWallDestroyer: No walls assigned to destroy!");
            return;
        }

        foreach (GameObject wall in fogWalls)
        {
            if (wall != null)
            {
                Destroy(wall, delay);
            }
        }

        Debug.Log($"FogWallDestroyer: Destroyed {fogWalls.Length} objects.");
    }
}