using UnityEngine;

public class InspectManager : MonoBehaviour
{
    public Transform spawnPoint;
    public Camera inspectCamera;

    [Header("Optional: Let InspectManager manage pause/cursor")]
    public bool manageTimeScale = false;
    public bool manageCursor = false;

    [Header("Auto Fit (Uncharted-style)")]
    [Tooltip("Extra space around the object. 0.9 = more padding, 1.0 = tight fit.")]
    [Range(0.5f, 1.0f)]
    public float fitPadding = 0.9f;

    [Tooltip("Reset the inspect camera FOV each time you show an item.")]
    public bool resetFovOnShow = true;

    [Tooltip("Default FOV to reset to (only used if resetFovOnShow is true).")]
    public float defaultFov = 40f;

    GameObject currentPivot;      // wrapper pivot at spawnpoint
    GameObject currentModel;      // instantiated prefab (child of pivot)
    InspectRotator currentRotator;

    float prevTimeScale = 1f;
    CursorLockMode prevLockMode;
    bool prevCursorVisible;

    public InspectRotator CurrentRotator => currentRotator;

    public void Show(GameObject collectiblePrefab)
    {
        if (collectiblePrefab == null)
        {
            Hide();
            Debug.LogWarning("[InspectManager] Show called with null prefab.");
            return;
        }

        Hide();

        // Save state (so Hide restores correctly if manage* toggles are ON)
        prevTimeScale = Time.timeScale;
        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        if (resetFovOnShow && inspectCamera != null)
            inspectCamera.fieldOfView = defaultFov;

        // Create pivot at spawn point (this is what we rotate/scale)
        currentPivot = new GameObject($"InspectPivot_{collectiblePrefab.name}");
        currentPivot.transform.position = spawnPoint.position;
        currentPivot.transform.rotation = spawnPoint.rotation;

        // Instantiate model as child of pivot
        currentModel = Instantiate(collectiblePrefab, currentPivot.transform);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;

        // Assign Inspect layer to pivot + all children
        int inspectLayer = LayerMask.NameToLayer("Inspect");
        currentPivot.layer = inspectLayer;
        foreach (Transform t in currentPivot.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = inspectLayer;

        // Add rotator to pivot (rotate pivot, not the mesh itself)
        currentRotator = currentPivot.GetComponent<InspectRotator>();
        if (currentRotator == null) currentRotator = currentPivot.AddComponent<InspectRotator>();
        currentRotator.inspectCamera = inspectCamera;

        // Auto center & auto scale
        AutoCenterAndScale();

        if (manageTimeScale) Time.timeScale = 0f;

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void AutoCenterAndScale()
    {
        if (inspectCamera == null || currentPivot == null) return;

        // Calculate bounds from renderers
        if (!TryGetRendererBounds(currentPivot, out Bounds b))
        {
            Debug.LogWarning("[InspectManager] No renderers found to compute bounds for auto-fit.");
            return;
        }

        // 1) Auto-center: move model so its bounds center becomes pivot origin
        // We move the model child, not the pivot (pivot stays at spawnpoint)
        Vector3 centerWorld = b.center;
        Vector3 centerLocalToPivot = currentPivot.transform.InverseTransformPoint(centerWorld);
        currentModel.transform.localPosition -= centerLocalToPivot;

        // Recalculate bounds after centering
        if (!TryGetRendererBounds(currentPivot, out b))
            return;

        // 2) Auto-scale: fit bounding sphere radius into camera frustum
        float radius = b.extents.magnitude;
        if (radius <= 0.0001f) return;

        float distance = Vector3.Distance(inspectCamera.transform.position, currentPivot.transform.position);
        if (distance <= 0.0001f) distance = 1f;

        float vFovRad = inspectCamera.fieldOfView * Mathf.Deg2Rad;
        float halfHeight = Mathf.Tan(vFovRad * 0.5f) * distance;
        float halfWidth = halfHeight * inspectCamera.aspect;

        float limitingHalfSize = Mathf.Min(halfHeight, halfWidth);

        float targetRadius = limitingHalfSize * fitPadding;
        float scaleFactor = targetRadius / radius;

        currentPivot.transform.localScale = Vector3.one * scaleFactor;
    }

    bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds(root.transform.position, Vector3.zero);

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            // Skip ParticleSystemRenderer etc if you want (optional); for now keep all renderers.
            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    public void Hide()
    {
        if (currentPivot != null) Destroy(currentPivot);
        currentPivot = null;
        currentModel = null;
        currentRotator = null;

        if (manageTimeScale) Time.timeScale = prevTimeScale;

        if (manageCursor)
        {
            Cursor.lockState = prevLockMode;
            Cursor.visible = prevCursorVisible;
        }
    }

    public bool IsInspecting()
    {
        return currentPivot != null;
    }
}
