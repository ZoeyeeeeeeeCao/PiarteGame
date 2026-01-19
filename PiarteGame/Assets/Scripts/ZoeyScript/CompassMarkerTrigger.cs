using UnityEngine;

/// <summary>
/// Triggers compass marker changes when player enters collider
/// Used for progressive waypoint system after door opens
/// </summary>
[RequireComponent(typeof(Collider))]
public class CompassMarkerTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";

    [Header("Marker to Hide (when entering)")]
    [Tooltip("Marker ID to hide when player enters this trigger")]
    public string hideMarkerID = "";

    [Header("Marker to Show (when entering)")]
    [Tooltip("Marker ID to show when player enters this trigger")]
    public string showMarkerID = "";

    [Header("Compass Reference")]
    public Compass compass;

    [Header("Trigger Once")]
    [Tooltip("If true, this trigger only works once")]
    public bool triggerOnce = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool hasTriggered = false;

    private void Awake()
    {
        // Auto-find compass if not assigned
        if (compass == null)
            compass = FindObjectOfType<Compass>();

        // Ensure trigger is set up
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (triggerOnce && hasTriggered) return;

        if (compass == null)
        {
            Debug.LogWarning($"[CompassMarkerTrigger] {name}: Compass reference not found!");
            return;
        }

        hasTriggered = true;

        // Hide previous marker
        if (!string.IsNullOrEmpty(hideMarkerID))
        {
            compass.HideMarker(hideMarkerID);
            if (showDebugLogs)
                Debug.Log($"[CompassMarkerTrigger] {name}: Hidden marker '{hideMarkerID}'");
        }

        // Show next marker
        if (!string.IsNullOrEmpty(showMarkerID))
        {
            compass.ShowMarker(showMarkerID);
            if (showDebugLogs)
                Debug.Log($"[CompassMarkerTrigger] {name}: Shown marker '{showMarkerID}'");
        }
    }

    // Optional: Allow manual reset
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    // Gizmo for easy visualization in Scene view
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = hasTriggered ? new Color(0, 1, 0, 0.3f) : new Color(1, 1, 0, 0.3f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
        }
    }
}