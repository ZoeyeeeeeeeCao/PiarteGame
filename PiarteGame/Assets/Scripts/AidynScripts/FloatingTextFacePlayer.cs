using UnityEngine;

/// <summary>
/// Makes a world-space UI (Canvas / TMP / any transform) always face the player camera.
/// Best for 3rd-person: yaw-only billboard (no pitch/roll).
/// </summary>
public class FloatingTextFacePlayer : MonoBehaviour
{
    [Header("Target to face (usually the main camera)")]
    public Transform target;

    [Header("Rotation")]
    public bool yawOnly = true;          // recommended for 3rd-person
    public bool flipForward = false;     // enable if your UI appears backwards

    [Header("Optional smoothing")]
    public bool smooth = true;
    [Range(0f, 30f)] public float smoothSpeed = 12f;

    void Awake()
    {
        // Auto-find camera if not assigned
        if (target == null && Camera.main != null)
            target = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 dir = transform.position - target.position; // face the camera
        if (yawOnly) dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);

        if (flipForward)
            lookRot *= Quaternion.Euler(0f, 180f, 0f);

        if (smooth)
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * smoothSpeed);
        else
            transform.rotation = lookRot;
    }
}
