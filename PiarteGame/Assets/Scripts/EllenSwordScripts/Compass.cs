using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Compass : MonoBehaviour
{
    public RawImage compassTape;

    [Header("Camera Reference")]
    public Transform cameraTransform;
    public Transform playerTransform;

    [Header("Visual Settings")]
    public float viewScale = 0.25f;

    [Header("Smoothing")]
    public float smoothSpeed = 10f;

    [Header("Quest Marker Settings")]
    public GameObject questMarkerPrefab;
    public RectTransform markerContainer;
    public RectTransform markerMask;

    public float markerMaxSize = 30f;
    public float markerMinSize = 15f;
    public float nearDistance = 10f;
    public float farDistance = 100f;
    public Color markerColor = Color.yellow;

    [Header("Fade Settings")]
    public float fadeSpeed = 8f;
    public float fadeEdgeThreshold = 0.15f;

    [Header("Center Lock Settings")]
    [Tooltip("Distance at which marker locks to center (stops sliding)")]
    public float centerLockDistance = 15f;
    [Tooltip("Angle threshold for center snap (degrees)")]
    public float centerSnapAngle = 30f;
    [Tooltip("Once locked, angle needed to unlock (prevents flickering)")]
    public float unlockAngle = 45f;
    [Tooltip("Where to position marker: true = screen center, false = compass tape center")]
    public bool lockToScreenCenter = false;
    [Tooltip("If locking to screen center, this is the RectTransform of your Canvas")]
    public RectTransform canvasRect;

    [Header("Stability Settings")]
    [Tooltip("Smooth marker movement to reduce jittering")]
    public float markerSmoothSpeed = 15f;
    [Tooltip("Minimum angle change to update position (reduces micro-movements)")]
    public float angleDeadzone = 0.5f;

    [Header("Manual Quest Points")]
    [Tooltip("Assign your quest point GameObjects here manually")]
    public List<QuestPoint> questPoints = new List<QuestPoint>();

    [Header("Debug")]
    public bool showDebug = false;

    private float currentRotation;
    private float targetRotation;

    private Dictionary<Transform, MarkerData> activeMarkers = new Dictionary<Transform, MarkerData>();

    [System.Serializable]
    public class QuestPoint
    {
        [Tooltip("Unique identifier for this quest")]
        public string questID;

        [Tooltip("Where the marker should point to (Empty GameObject)")]
        public Transform targetLocation;

        [Tooltip("Check this to show the marker")]
        public bool isActive = false;

        [Tooltip("Optional: Custom color for this marker")]
        public bool useCustomColor = false;
        public Color customColor = Color.yellow;
    }

    private class MarkerData
    {
        public GameObject gameObject;
        public Image image;
        public RectTransform rectTransform;
        public float currentAlpha;
        public float targetAlpha;
        public Color baseColor;
        public float currentPosX;
        public float targetPosX;
        public bool isLocked;
        public float lastAngle;
    }

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (cameraTransform != null)
        {
            currentRotation = cameraTransform.eulerAngles.y;
            targetRotation = currentRotation;
        }

        SetupMarkerContainerWithMask();
    }

    void SetupMarkerContainerWithMask()
    {
        RectTransform compassRect = compassTape.GetComponent<RectTransform>();

        if (markerMask == null)
        {
            GameObject maskObj = new GameObject("MarkerMask");
            maskObj.transform.SetParent(compassTape.transform.parent, false);
            markerMask = maskObj.AddComponent<RectTransform>();
            markerMask.anchorMin = compassRect.anchorMin;
            markerMask.anchorMax = compassRect.anchorMax;
            markerMask.pivot = compassRect.pivot;
            markerMask.anchoredPosition = compassRect.anchoredPosition;
            markerMask.sizeDelta = compassRect.sizeDelta;
            Image maskImage = maskObj.AddComponent<Image>();
            maskImage.color = new Color(1, 1, 1, 0);
            maskObj.AddComponent<RectMask2D>();
        }

        if (markerContainer == null)
        {
            GameObject container = new GameObject("MarkerContainer");
            container.transform.SetParent(markerMask, false);
            markerContainer = container.AddComponent<RectTransform>();
            markerContainer.anchorMin = new Vector2(0.5f, 0.5f);
            markerContainer.anchorMax = new Vector2(0.5f, 0.5f);
            markerContainer.pivot = new Vector2(0.5f, 0.5f);
            markerContainer.anchoredPosition = Vector2.zero;
            markerContainer.sizeDelta = markerMask.sizeDelta;
        }

        // Force update to ensure proper positioning
        Canvas.ForceUpdateCanvases();
    }

    void Update()
    {
        if (cameraTransform == null) return;

        // Smooth rotation for compass tape
        targetRotation = cameraTransform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(currentRotation, targetRotation);
        currentRotation += delta * Time.deltaTime * smoothSpeed;
        currentRotation = Mathf.Repeat(currentRotation, 360f);

        // Update UV Tape
        float uvOffset = currentRotation / 360f;
        compassTape.uvRect = new Rect(uvOffset - (viewScale / 2f), 0, viewScale, 1);

        UpdateQuestMarkers();
    }

    void UpdateQuestMarkers()
    {
        if (playerTransform == null || markerContainer == null) return;

        // Remove markers for inactive quests
        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in activeMarkers)
        {
            bool shouldKeep = false;
            foreach (var questPoint in questPoints)
            {
                if (questPoint.targetLocation == kvp.Key && questPoint.isActive)
                {
                    shouldKeep = true;
                    break;
                }
            }

            if (!shouldKeep)
            {
                if (kvp.Value.gameObject != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove) activeMarkers.Remove(key);

        // Create/Update markers for active quests
        foreach (var questPoint in questPoints)
        {
            if (questPoint.targetLocation == null) continue;

            if (questPoint.isActive)
            {
                // Create marker if it doesn't exist
                if (!activeMarkers.ContainsKey(questPoint.targetLocation))
                {
                    CreateMarker(questPoint);
                }

                // Update marker position
                UpdateMarkerPosition(questPoint.targetLocation, activeMarkers[questPoint.targetLocation]);
            }
        }
    }

    void CreateMarker(QuestPoint questPoint)
    {
        GameObject marker;
        Color markerColorToUse = questPoint.useCustomColor ? questPoint.customColor : markerColor;

        if (questMarkerPrefab != null)
        {
            marker = Instantiate(questMarkerPrefab, markerContainer);
            Image img = marker.GetComponent<Image>();
            if (img != null) img.color = markerColorToUse;
        }
        else
        {
            marker = new GameObject("QuestMarker_" + questPoint.questID);
            marker.transform.SetParent(markerContainer, false);
            Image img = marker.AddComponent<Image>();
            img.color = markerColorToUse;
            RectTransform rt = marker.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(20, 20);
        }

        marker.SetActive(true);
        activeMarkers[questPoint.targetLocation] = new MarkerData
        {
            gameObject = marker,
            image = marker.GetComponent<Image>(),
            rectTransform = marker.GetComponent<RectTransform>(),
            currentAlpha = 0f,
            targetAlpha = 0f,
            baseColor = markerColorToUse,
            currentPosX = 0f,
            targetPosX = 0f,
            isLocked = false,
            lastAngle = 0f
        };
    }

    void UpdateMarkerPosition(Transform questLocation, MarkerData markerData)
    {
        // Use actual 3D positions without zeroing Y-axis
        Vector3 directionToTarget = questLocation.position - playerTransform.position;

        // Project direction onto horizontal plane for compass bearing
        Vector3 horizontalDir = new Vector3(directionToTarget.x, 0, directionToTarget.z);
        float distance = horizontalDir.magnitude;

        // Calculate angle between camera forward and target
        Vector3 cameraForwardFlat = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        float angle = Vector3.SignedAngle(cameraForwardFlat, horizontalDir.normalized, Vector3.up);

        // Apply angle deadzone to reduce jitter from micro-movements
        if (Mathf.Abs(angle - markerData.lastAngle) < angleDeadzone && !markerData.isLocked)
        {
            angle = markerData.lastAngle;
        }
        else
        {
            markerData.lastAngle = angle;
        }

        // Calculate visible degrees
        float visibleDegrees = viewScale * 360f;

        // IMPROVED: Center lock with hysteresis (prevents flickering)
        bool shouldLock = distance <= centerLockDistance && Mathf.Abs(angle) <= centerSnapAngle;
        bool shouldUnlock = Mathf.Abs(angle) > unlockAngle || distance > centerLockDistance;

        if (shouldLock && !markerData.isLocked)
        {
            markerData.isLocked = true;
        }
        else if (shouldUnlock && markerData.isLocked)
        {
            markerData.isLocked = false;
        }

        // Hide marker if outside visible range (but not if locked)
        if (Mathf.Abs(angle) > (visibleDegrees / 2f) && !markerData.isLocked)
        {
            markerData.targetAlpha = 0;
        }
        else
        {
            markerData.targetAlpha = 1;

            // Edge fading (only if not locked)
            if (!markerData.isLocked)
            {
                float edgeFade = (visibleDegrees / 2f) * fadeEdgeThreshold;
                float distFromEdge = (visibleDegrees / 2f) - Mathf.Abs(angle);
                if (distFromEdge < edgeFade)
                {
                    markerData.targetAlpha = distFromEdge / edgeFade;
                }
            }
        }

        // Calculate target position X
        float targetPosX;

        if (markerData.isLocked)
        {
            if (lockToScreenCenter && canvasRect != null)
            {
                // Lock to absolute screen center
                Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    markerContainer,
                    screenCenter,
                    null,
                    out Vector2 localPoint
                );
                targetPosX = localPoint.x;
            }
            else
            {
                // Lock to compass tape center
                targetPosX = 0f;
            }
        }
        else
        {
            // Normal positioning - use the full mask width for calculations
            float parentWidth = markerMask.rect.width;
            float normalizedX = angle / (visibleDegrees / 2f);
            targetPosX = normalizedX * (parentWidth / 2f);
        }

        // Smooth position change
        markerData.currentPosX = Mathf.Lerp(markerData.currentPosX, targetPosX, Time.deltaTime * markerSmoothSpeed);
        markerData.rectTransform.anchoredPosition = new Vector2(markerData.currentPosX, 0);

        // Update visual (size & alpha)
        markerData.currentAlpha = Mathf.Lerp(markerData.currentAlpha, markerData.targetAlpha, Time.deltaTime * fadeSpeed);

        float size = Mathf.Lerp(markerMaxSize, markerMinSize, Mathf.InverseLerp(nearDistance, farDistance, distance));
        markerData.rectTransform.sizeDelta = new Vector2(size, size);

        if (markerData.image != null)
        {
            Color c = markerData.baseColor;
            c.a = markerData.currentAlpha;
            markerData.image.color = c;
        }

        // Debug
        if (showDebug)
        {
            Debug.DrawLine(playerTransform.position, questLocation.position, markerData.isLocked ? Color.green : Color.yellow);
            Debug.DrawRay(playerTransform.position, cameraForwardFlat * 10f, Color.blue);
        }
    }

    // ============================================================
    // PUBLIC API - Call these from ANY quest/mission script
    // ============================================================

    /// <summary>
    /// Activate a quest marker by its ID
    /// </summary>
    public void ShowMarker(string questID)
    {
        Debug.Log($"[COMPASS DEBUG] Attempting to show marker: '{questID}'");

        foreach (var questPoint in questPoints)
        {
            Debug.Log($"[COMPASS DEBUG] Checking quest point: '{questPoint.questID}' (Target: {questPoint.targetLocation?.name ?? "NULL"})");

            if (questPoint.questID == questID)
            {
                questPoint.isActive = true;
                Debug.Log($"[COMPASS DEBUG] ✓ Marker activated for '{questID}'");
                return;
            }
        }
        Debug.LogWarning($"[COMPASS DEBUG] ✗ Quest ID '{questID}' NOT FOUND in questPoints list!");
        Debug.LogWarning($"[COMPASS DEBUG] Available Quest IDs: {string.Join(", ", System.Array.ConvertAll(questPoints.ToArray(), qp => qp.questID))}");
    }

    /// <summary>
    /// Deactivate a quest marker by its ID
    /// </summary>
    public void HideMarker(string questID)
    {
        foreach (var questPoint in questPoints)
        {
            if (questPoint.questID == questID)
            {
                questPoint.isActive = false;
                if (showDebug) Debug.Log($"Compass: Hiding marker for {questID}");
                return;
            }
        }
    }

    /// <summary>
    /// Show multiple markers at once
    /// </summary>
    public void ShowMarkers(string[] questIDs)
    {
        foreach (string id in questIDs)
        {
            ShowMarker(id);
        }
    }

    /// <summary>
    /// Hide multiple markers at once
    /// </summary>
    public void HideMarkers(string[] questIDs)
    {
        foreach (string id in questIDs)
        {
            HideMarker(id);
        }
    }

    /// <summary>
    /// Hide all active markers
    /// </summary>
    public void HideAllMarkers()
    {
        foreach (var questPoint in questPoints)
        {
            questPoint.isActive = false;
        }
        if (showDebug) Debug.Log("Compass: All markers hidden");
    }

    /// <summary>
    /// Check if a specific marker is currently active
    /// </summary>
    public bool IsMarkerActive(string questID)
    {
        foreach (var questPoint in questPoints)
        {
            if (questPoint.questID == questID)
            {
                return questPoint.isActive;
            }
        }
        return false;
    }
}