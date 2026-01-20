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
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                r.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        // ✅ Save checkpoint (and compass step)
        if (LevelCheckpointManager.Instance != null)
            LevelCheckpointManager.Instance.SetCheckpoint(transform, other.transform);

        if (disableAfterTriggered)
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}
