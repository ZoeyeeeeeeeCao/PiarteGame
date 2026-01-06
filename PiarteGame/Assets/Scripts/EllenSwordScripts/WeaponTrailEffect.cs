using UnityEngine;
using System.Collections.Generic;

public class WeaponTrailEffect : MonoBehaviour
{
    [Header("Setup - General")]
    [SerializeField] private bool enableGizmos = true;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private string trailName = "Trail 1";

    [Header("Trail Transform Settings")]
    [SerializeField] private Transform lineTipTransform;
    [SerializeField] private Transform lineBottomTransform;

    [Header("Trail Renderer Prefab Settings")]
    [SerializeField] private GameObject trailRendererPrefab;
    [SerializeField] private int trailSpawnCount = 3; // Number of trail points along the blade
    [SerializeField] private float trailScale = 1f;

    // Trail state - REACTIVE ONLY
    private bool isTrailActive = false;
    private List<GameObject> activeTrailObjects = new List<GameObject>();
    private List<TrailRenderer> trailRenderers = new List<TrailRenderer>();

    private void Start()
    {
        if (trailRendererPrefab == null)
        {
            Debug.LogWarning("Trail Renderer Prefab is not assigned!");
        }

        if (lineTipTransform == null || lineBottomTransform == null)
        {
            Debug.LogWarning("Trail transforms not assigned!");
        }
    }

    // CRITICAL: Update only handles position tracking, NOT trail activation
    private void Update()
    {
        // Only update positions if trail is active
        if (isTrailActive && activeTrailObjects.Count > 0)
        {
            UpdateTrailPositions();
        }
    }

    // REACTIVE: Only starts when explicitly called by combat controller
    public void StartTrail()
    {
        Debug.Log(">>> StartTrail() called!");

        // Validation checks
        if (trailRendererPrefab == null)
        {
            Debug.LogError(">>> FAILED: Trail Renderer Prefab is not assigned!");
            return;
        }
        else
        {
            Debug.Log($">>> Trail prefab is assigned: {trailRendererPrefab.name}");
        }

        if (lineTipTransform == null || lineBottomTransform == null)
        {
            Debug.LogError($">>> FAILED: Transforms not assigned! Tip={lineTipTransform}, Bottom={lineBottomTransform}");
            return;
        }
        else
        {
            Debug.Log($">>> Transforms OK: Tip={lineTipTransform.name}, Bottom={lineBottomTransform.name}");
        }

        // Prevent double-start
        if (isTrailActive)
        {
            Debug.Log(">>> Trail already active, skipping StartTrail");
            return;
        }

        isTrailActive = true;
        Debug.Log($">>> Setting isTrailActive = true. Starting to spawn {trailSpawnCount} trail renderers...");

        // Spawn trail renderer objects along the blade
        for (int i = 0; i < trailSpawnCount; i++)
        {
            float t = trailSpawnCount > 1 ? (float)i / (trailSpawnCount - 1) : 0.5f;
            Vector3 spawnPos = Vector3.Lerp(lineBottomTransform.position, lineTipTransform.position, t);

            Debug.Log($">>> Spawning trail renderer {i + 1} at position: {spawnPos}");

            GameObject trailObject = Instantiate(trailRendererPrefab, spawnPos, Quaternion.identity, transform);
            trailObject.name = $"{trailName} Effect {i + 1}";
            trailObject.transform.localScale = Vector3.one * trailScale;

            Debug.Log($">>> Trail GameObject created: {trailObject.name}");

            // Find TrailRenderer component (could be on root or child)
            TrailRenderer tr = trailObject.GetComponent<TrailRenderer>();
            if (tr == null)
            {
                tr = trailObject.GetComponentInChildren<TrailRenderer>();
            }

            if (tr != null)
            {
                Debug.Log($">>> TrailRenderer found on {tr.gameObject.name}, enabling and clearing");
                tr.enabled = true;
                tr.Clear(); // Clear any old trail data
                trailRenderers.Add(tr);
            }
            else
            {
                Debug.LogError($">>> NO TrailRenderer component found on {trailObject.name} or its children!");
            }

            activeTrailObjects.Add(trailObject);
        }

        Debug.Log($">>> [WeaponTrail] Trail STARTED - {activeTrailObjects.Count} objects spawned, {trailRenderers.Count} trail renderers active");
    }

    // REACTIVE: Only stops when explicitly called by combat controller
    public void StopTrail()
    {
        Debug.Log(">>> StopTrail() called!");

        // Prevent double-stop
        if (!isTrailActive)
        {
            Debug.Log(">>> Trail already inactive, skipping StopTrail");
            return;
        }

        isTrailActive = false;

        // Disable all trail renderers (they'll naturally fade based on their time settings)
        foreach (var tr in trailRenderers)
        {
            if (tr != null)
            {
                tr.enabled = false;
                Debug.Log($">>> Disabled TrailRenderer on {tr.gameObject.name}");
            }
        }

        // Calculate proper lifetime for destruction
        float destroyDelay = 2f; // Default fallback
        if (trailRenderers.Count > 0 && trailRenderers[0] != null)
        {
            destroyDelay = trailRenderers[0].time + 0.5f; // Trail time + buffer
        }

        // Destroy all trail objects after trails naturally fade
        foreach (var trailObj in activeTrailObjects)
        {
            if (trailObj != null)
            {
                Destroy(trailObj, destroyDelay);
            }
        }

        Debug.Log($">>> [WeaponTrail] Trail STOPPED - trails will fade in {destroyDelay}s");

        // Clear references immediately
        activeTrailObjects.Clear();
        trailRenderers.Clear();
    }

    // Position tracking - follows blade transforms
    private void UpdateTrailPositions()
    {
        if (lineTipTransform == null || lineBottomTransform == null)
            return;

        Vector3 tipPos = lineTipTransform.position;
        Vector3 bottomPos = lineBottomTransform.position;
        Vector3 direction = tipPos - bottomPos;

        // Update each trail object position along the blade
        for (int i = 0; i < activeTrailObjects.Count; i++)
        {
            if (activeTrailObjects[i] != null)
            {
                float t = trailSpawnCount > 1 ? (float)i / (trailSpawnCount - 1) : 0.5f;
                Vector3 targetPos = Vector3.Lerp(bottomPos, tipPos, t);

                activeTrailObjects[i].transform.position = targetPos;

                // Orient trail object along the blade direction
                if (direction.magnitude > 0.001f)
                {
                    activeTrailObjects[i].transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        if (debugMode)
        {
            Debug.DrawLine(bottomPos, tipPos, Color.cyan);
        }
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

            // Draw spawn points preview
            for (int i = 0; i < trailSpawnCount; i++)
            {
                float t = trailSpawnCount > 1 ? (float)i / (trailSpawnCount - 1) : 0.5f;
                Vector3 spawnPos = Vector3.Lerp(lineBottomTransform.position, lineTipTransform.position, t);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPos, 0.03f);
            }
        }
    }

    // Utility methods
    public void SetTrailCount(int count)
    {
        if (count > 0)
        {
            trailSpawnCount = count;
            if (isTrailActive)
            {
                StopTrail();
                StartTrail();
            }
        }
    }

    public void SetTrailScale(float scale)
    {
        trailScale = scale;
        foreach (var trailObj in activeTrailObjects)
        {
            if (trailObj != null)
            {
                trailObj.transform.localScale = Vector3.one * trailScale;
            }
        }
    }

    // Public getter for trail state
    public bool IsTrailActive => isTrailActive;

    // Cleanup on destroy
    private void OnDestroy()
    {
        foreach (var trailObj in activeTrailObjects)
        {
            if (trailObj != null)
            {
                Destroy(trailObj);
            }
        }
        activeTrailObjects.Clear();
        trailRenderers.Clear();
    }
}