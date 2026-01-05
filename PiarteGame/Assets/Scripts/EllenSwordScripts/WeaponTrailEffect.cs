using UnityEngine;
using System.Collections.Generic;

public class WeaponTrailEffect : MonoBehaviour
{
    [Header("Setup - General")]
    [SerializeField] private bool enableGizmos = true;
    [SerializeField] private bool rootMotion = false;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool useEvents = false;
    [SerializeField] private string trailName = "Trail 1";

    [Header("Trail Transform Settings")]
    [SerializeField] private Transform lineTipTransform;
    [SerializeField] private Transform lineBottomTransform;

    [Header("Trail Settings")]
    [SerializeField] private bool enableTrail = false;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private Color trailColor = Color.white;
    [SerializeField][Range(0f, 1f)] private float fadeInDuration = 0.2f;
    [SerializeField][Range(0f, 2f)] private float fadeOutDuration = 0.05f;
    [SerializeField][Range(0.05f, 2f)] private float trailLifetime = 0.35f;
    [SerializeField][Range(0.01f, 0.5f)] private float minVertexDistance = 0.1f;
    [SerializeField] private int maxTrailPoints = 50;

    [Header("Playback Settings")]
    [SerializeField][Range(0.1f, 5f)] private float playbackSpeed = 1f;
    [SerializeField][Range(0f, 1f)] private float rangeOffset = 0.2f;

    // Trail mesh data
    private Mesh trailMesh;
    private GameObject trailObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private List<TrailPoint> trailPoints = new List<TrailPoint>();
    private bool isTrailActive = false;
    private float timeSinceLastPoint = 0f;

    private class TrailPoint
    {
        public Vector3 tipPosition;
        public Vector3 bottomPosition;
        public float timestamp;
        public float normalizedTime;

        public TrailPoint(Vector3 tip, Vector3 bottom, float time)
        {
            tipPosition = tip;
            bottomPosition = bottom;
            timestamp = time;
            normalizedTime = 0f;
        }
    }

    private void Start()
    {
        InitializeTrail();
    }

    private void InitializeTrail()
    {
        // Create trail object
        trailObject = new GameObject(trailName + " Mesh");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = Vector3.zero;
        trailObject.transform.localRotation = Quaternion.identity;

        // Add mesh components
        meshFilter = trailObject.AddComponent<MeshFilter>();
        meshRenderer = trailObject.AddComponent<MeshRenderer>();

        // Create mesh
        trailMesh = new Mesh();
        trailMesh.name = trailName + " Mesh";
        meshFilter.mesh = trailMesh;

        // Set material
        if (trailMaterial != null)
        {
            meshRenderer.material = trailMaterial;
            meshRenderer.material.color = trailColor;
        }

        trailObject.SetActive(false);
    }

    private void Update()
    {
        if (enableTrail && !isTrailActive)
        {
            StartTrail();
        }
        else if (!enableTrail && isTrailActive)
        {
            StopTrail();
        }

        if (isTrailActive)
        {
            UpdateTrail();
        }
    }

    public void StartTrail()
    {
        isTrailActive = true;
        trailPoints.Clear();
        trailObject.SetActive(true);
        timeSinceLastPoint = 0f;
    }

    public void StopTrail()
    {
        isTrailActive = false;
    }

    private void UpdateTrail()
    {
        if (lineTipTransform == null || lineBottomTransform == null)
            return;

        timeSinceLastPoint += Time.deltaTime * playbackSpeed;

        // Add new point if distance threshold is met
        if (timeSinceLastPoint >= minVertexDistance / 10f)
        {
            Vector3 tipPos = lineTipTransform.position;
            Vector3 bottomPos = lineBottomTransform.position;

            if (trailPoints.Count == 0 ||
                Vector3.Distance(tipPos, trailPoints[trailPoints.Count - 1].tipPosition) > minVertexDistance)
            {
                TrailPoint newPoint = new TrailPoint(tipPos, bottomPos, Time.time);
                trailPoints.Add(newPoint);

                if (trailPoints.Count > maxTrailPoints)
                {
                    trailPoints.RemoveAt(0);
                }

                timeSinceLastPoint = 0f;
            }
        }

        // Remove old points based on lifetime
        for (int i = trailPoints.Count - 1; i >= 0; i--)
        {
            float age = Time.time - trailPoints[i].timestamp;
            if (age > trailLifetime)
            {
                trailPoints.RemoveAt(i);
            }
        }

        // Update mesh
        UpdateTrailMesh();

        // Clear trail if no points remain
        if (trailPoints.Count == 0)
        {
            trailObject.SetActive(false);
        }
    }

    private void UpdateTrailMesh()
    {
        if (trailPoints.Count < 2)
        {
            trailMesh.Clear();
            return;
        }

        int pointCount = trailPoints.Count;
        Vector3[] vertices = new Vector3[pointCount * 2];
        Vector2[] uvs = new Vector2[pointCount * 2];
        Color[] colors = new Color[pointCount * 2];
        int[] triangles = new int[(pointCount - 1) * 6];

        // Build mesh data
        for (int i = 0; i < pointCount; i++)
        {
            TrailPoint point = trailPoints[i];
            float age = Time.time - point.timestamp;
            float normalizedAge = age / trailLifetime;

            // Calculate alpha based on fade in/out
            float alpha = 1f;
            if (age < fadeInDuration)
            {
                alpha = age / fadeInDuration;
            }
            else if (normalizedAge > (1f - fadeOutDuration / trailLifetime))
            {
                alpha = (1f - normalizedAge) / (fadeOutDuration / trailLifetime);
            }

            alpha = Mathf.Clamp01(alpha);

            // Set vertices
            vertices[i * 2] = transform.InverseTransformPoint(point.bottomPosition);
            vertices[i * 2 + 1] = transform.InverseTransformPoint(point.tipPosition);

            // Set UVs
            float uvX = (float)i / (pointCount - 1);
            uvs[i * 2] = new Vector2(uvX, 0);
            uvs[i * 2 + 1] = new Vector2(uvX, 1);

            // Set colors with alpha
            Color vertexColor = trailColor;
            vertexColor.a *= alpha;
            colors[i * 2] = vertexColor;
            colors[i * 2 + 1] = vertexColor;
        }

        // Build triangles
        for (int i = 0; i < pointCount - 1; i++)
        {
            int baseIndex = i * 6;
            int vertIndex = i * 2;

            triangles[baseIndex] = vertIndex;
            triangles[baseIndex + 1] = vertIndex + 1;
            triangles[baseIndex + 2] = vertIndex + 2;

            triangles[baseIndex + 3] = vertIndex + 1;
            triangles[baseIndex + 4] = vertIndex + 3;
            triangles[baseIndex + 5] = vertIndex + 2;
        }

        // Apply to mesh
        trailMesh.Clear();
        trailMesh.vertices = vertices;
        trailMesh.uv = uvs;
        trailMesh.colors = colors;
        trailMesh.triangles = triangles;
        trailMesh.RecalculateNormals();
        trailMesh.RecalculateBounds();
    }

    private void OnDrawGizmos()
    {
        if (!enableGizmos)
            return;

        // Draw tip transform
        if (lineTipTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lineTipTransform.position, 0.02f);
            Gizmos.DrawLine(lineTipTransform.position, lineTipTransform.position + lineTipTransform.right * 0.1f);
        }

        // Draw bottom transform
        if (lineBottomTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lineBottomTransform.position, 0.02f);
            Gizmos.DrawLine(lineBottomTransform.position, lineBottomTransform.position + lineBottomTransform.right * 0.1f);
        }

        // Draw connection line
        if (lineTipTransform != null && lineBottomTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lineTipTransform.position, lineBottomTransform.position);
        }

        // Draw trail points
        if (Application.isPlaying && debugMode)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < trailPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(trailPoints[i].tipPosition, trailPoints[i + 1].tipPosition);
                Gizmos.DrawLine(trailPoints[i].bottomPosition, trailPoints[i + 1].bottomPosition);
            }
        }
    }

    private void OnDestroy()
    {
        if (trailMesh != null)
        {
            Destroy(trailMesh);
        }
    }
}