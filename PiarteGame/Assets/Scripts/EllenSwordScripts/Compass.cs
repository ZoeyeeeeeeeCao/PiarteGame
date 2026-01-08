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

    private float currentRotation;
    private float targetRotation;
    private float scanTimer = 0f;
    private float scanInterval = 0.5f;

    private Dictionary<Transform, MarkerData> activeMarkers = new Dictionary<Transform, MarkerData>();
    private List<Transform> questLocations = new List<Transform>();

    private class MarkerData
    {
        public GameObject gameObject;
        public Image image;
        public RectTransform rectTransform;
        public float currentAlpha;
        public float targetAlpha;
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
        UpdateQuestLocations();
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
    }

    void Update()
    {
        if (cameraTransform == null) return;

        targetRotation = cameraTransform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(currentRotation, targetRotation);
        currentRotation += delta * Time.deltaTime * smoothSpeed;
        currentRotation = Mathf.Repeat(currentRotation, 360f);

        float uvOffset = (currentRotation / 360f) * 0.8f;
        float westAlignment = -0.6f;
        float finalX = uvOffset + westAlignment - (viewScale / 2f);
        finalX = Mathf.Repeat(finalX + 0.8f, 0.8f);

        compassTape.uvRect = new Rect(finalX, 0, viewScale, 1);

        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            UpdateQuestLocations();
            scanTimer = 0f;
        }

        UpdateQuestMarkers();
    }

    void UpdateQuestLocations()
    {
        questLocations.Clear();
        OpeningTutorialManager[] tutorialManagers = FindObjectsOfType<OpeningTutorialManager>();
        foreach (var manager in tutorialManagers)
        {
            if (manager.missions != null)
            {
                foreach (var mission in manager.missions)
                {
                    if (!mission.isCompleted && mission.npc != null)
                    {
                        questLocations.Add(mission.npc.transform);
                    }
                }
            }
        }
    }

    void UpdateQuestMarkers()
    {
        if (playerTransform == null || markerContainer == null) return;

        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in activeMarkers)
        {
            if (!questLocations.Contains(kvp.Key))
            {
                if (kvp.Value.gameObject != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove) activeMarkers.Remove(key);

        foreach (Transform questLocation in questLocations)
        {
            if (!activeMarkers.ContainsKey(questLocation)) CreateMarker(questLocation);
            UpdateMarkerPosition(questLocation, activeMarkers[questLocation]);
        }
    }

    void CreateMarker(Transform questLocation)
    {
        GameObject marker;
        if (questMarkerPrefab != null)
        {
            marker = Instantiate(questMarkerPrefab, markerContainer);
        }
        else
        {
            marker = new GameObject("QuestMarker_" + questLocation.name);
            marker.transform.SetParent(markerContainer, false);
            Image img = marker.AddComponent<Image>();
            img.color = markerColor;
            RectTransform rt = marker.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(20, 20);
        }

        marker.SetActive(true);
        activeMarkers[questLocation] = new MarkerData
        {
            gameObject = marker,
            image = marker.GetComponent<Image>(),
            rectTransform = marker.GetComponent<RectTransform>(),
            currentAlpha = 0f,
            targetAlpha = 0f
        };
    }

    void UpdateMarkerPosition(Transform questLocation, MarkerData markerData)
    {
        Vector3 dir = questLocation.position - playerTransform.position;
        dir.y = 0;
        float angleToTarget = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        float relativeAngle = Mathf.DeltaAngle(cameraTransform.eulerAngles.y, angleToTarget);

        float visibleDegrees = (viewScale / 0.8f) * 360f;
        float normalizedPos = relativeAngle / visibleDegrees;

        float actualWidth = markerMask.rect.width;

        RectMask2D mask = markerMask.GetComponent<RectMask2D>();
        if (mask != null)
        {
            actualWidth -= (mask.padding.x + mask.padding.z);
        }

        float posX = normalizedPos * actualWidth;
        markerData.rectTransform.anchoredPosition = new Vector2(posX, 0);

        float distFromCenter = Mathf.Abs(normalizedPos);
        float fadeAmount = 1f;
        float fadeStart = 0.5f - fadeEdgeThreshold;

        if (distFromCenter > fadeStart)
        {
            fadeAmount = 1f - Mathf.Clamp01((distFromCenter - fadeStart) / fadeEdgeThreshold);
        }

        if (distFromCenter > 0.5f || Mathf.Abs(relativeAngle) > 90f)
        {
            fadeAmount = 0;
        }

        markerData.targetAlpha = fadeAmount;
        markerData.currentAlpha = Mathf.Lerp(markerData.currentAlpha, markerData.targetAlpha, Time.deltaTime * fadeSpeed);

        float distance = Vector3.Distance(playerTransform.position, questLocation.position);
        float size = Mathf.Lerp(markerMaxSize, markerMinSize, Mathf.InverseLerp(nearDistance, farDistance, distance));
        markerData.rectTransform.sizeDelta = new Vector2(size, size);

        if (markerData.image != null)
        {
            Color c = markerColor;
            c.a = markerData.currentAlpha;
            markerData.image.color = c;
        }
    }

    public void AddQuestLocation(Transform location)
    {
        if (!questLocations.Contains(location)) questLocations.Add(location);
    }

    public void RemoveQuestLocation(Transform location)
    {
        questLocations.Remove(location);
        if (activeMarkers.ContainsKey(location))
        {
            if (activeMarkers[location].gameObject != null) Destroy(activeMarkers[location].gameObject);
            activeMarkers.Remove(location);
        }
    }
}