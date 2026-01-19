using UnityEngine;

[DisallowMultipleComponent]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Hide this checkpoint's renderers on play")]
    public bool hideRenderersOnPlay = true;

    [Header("Checkpoint Behavior")]
    [Tooltip("Disable this checkpoint after it is activated")]
    public bool disableAfterTriggered = true;

    private bool hasTriggered = false;

    private void Start()
    {
        if (hideRenderersOnPlay)
        {
            // Hide this object and all child renderers
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (LevelCheckpointManager.Instance != null)
        {
            // Save checkpoint + advance compass marker if this is the next one
            LevelCheckpointManager.Instance.SetCheckpoint(transform, other.transform);
        }

        // Disable collider so it cannot retrigger
        if (disableAfterTriggered)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}
