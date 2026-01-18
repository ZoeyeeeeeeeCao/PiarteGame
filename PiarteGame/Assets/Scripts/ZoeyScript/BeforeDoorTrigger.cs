using UnityEngine;

/// <summary>
/// Place this trigger between NPC area and Answer Door
/// Transitions compass marker from waypoint to actual door
/// </summary>
[RequireComponent(typeof(Collider))]
public class BeforeDoorTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";

    [Header("Compass Integration")]
    public Compass compass;
    public string hideMarkerID = "before_answer_door";
    public string showMarkerID = "answer_door";

    [Header("Trigger Once")]
    public bool triggerOnce = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool hasTriggered = false;

    private void Awake()
    {
        if (compass == null)
            compass = FindObjectOfType<Compass>();

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
            Debug.LogWarning($"[BeforeDoorTrigger] {name}: Compass not found!");
            return;
        }

        hasTriggered = true;

        // Hide waypoint marker
        if (!string.IsNullOrEmpty(hideMarkerID))
        {
            compass.HideMarker(hideMarkerID);
            if (showDebugLogs)
                Debug.Log($"[BeforeDoorTrigger] Hidden marker: {hideMarkerID}");
        }

        // Show door marker
        if (!string.IsNullOrEmpty(showMarkerID))
        {
            compass.ShowMarker(showMarkerID);
            if (showDebugLogs)
                Debug.Log($"[BeforeDoorTrigger] Shown marker: {showMarkerID}");
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = hasTriggered ? new Color(0, 1, 0, 0.3f) : new Color(1, 0.5f, 0, 0.3f);

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