using UnityEngine;

/// <summary>
/// Place this trigger after passing through Answer Door
/// Shows the "after_answer_door" marker for stone puzzle area
/// </summary>
[RequireComponent(typeof(Collider))]
public class AfterDoorTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";

    [Header("Compass Integration")]
    public Compass compass;
    public string hideMarkerID = "before_answer_door"; // Extra safety cleanup
    public string showMarkerID = "after_answer_door";

    [Header("Trigger Once")]
    public bool triggerOnce = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

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
            Debug.LogWarning($"[AfterDoorTrigger] {name}: Compass not found!");
            return;
        }

        hasTriggered = true;

        // Clean up any previous markers
        if (!string.IsNullOrEmpty(hideMarkerID))
        {
            compass.HideMarker(hideMarkerID);
            if (showDebugLogs)
                Debug.Log($"[AfterDoorTrigger] Hidden marker: {hideMarkerID}");
        }

        // Show stone puzzle area marker
        if (!string.IsNullOrEmpty(showMarkerID))
        {
            compass.ShowMarker(showMarkerID);
            if (showDebugLogs)
                Debug.Log($"[AfterDoorTrigger] Shown marker: {showMarkerID}");
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = hasTriggered ? new Color(0, 1, 0, 0.3f) : new Color(0, 0.5f, 1, 0.3f);

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
