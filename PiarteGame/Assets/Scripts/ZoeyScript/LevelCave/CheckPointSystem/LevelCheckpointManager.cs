using UnityEngine;
using System.Collections.Generic;

public class LevelCheckpointManager : MonoBehaviour
{
    public static LevelCheckpointManager Instance;

    private Transform currentCheckpoint;
    private Transform lastPlayerTransform;

    private readonly List<RevertibleObject> revertibles = new();

    public bool HasCheckpoint => currentCheckpoint != null;

    [Header("Compass Marker (Sequential Checkpoints)")]
    [Tooltip("Assign checkpoints in the exact order the player should reach them.")]
    [SerializeField] private List<Transform> checkpointOrder = new();

    [Tooltip("Quest IDs in the Compass.questPoints list (same order as checkpointOrder).")]
    [SerializeField] private List<string> checkpointMarkerIDs = new();

    [Tooltip("Reference to Compass in the scene (optional). If null, we'll find it.")]
    [SerializeField] private Compass compass;

    private int currentCheckpointIndex = -1;

    private void Awake()
    {
        Instance = this;

        if (compass == null)
            compass = FindFirstObjectByType<Compass>(); // Unity 6 friendly
    }

    private void Start()
    {
        // Start by showing first marker (if configured)
        ShowOnlyMarkerForIndex(0);

    }

    public void Register(RevertibleObject obj)
    {
        if (obj != null && !revertibles.Contains(obj))
            revertibles.Add(obj);
    }

    public void SetCheckpoint(Transform point, Transform player)
    {
        currentCheckpoint = point;

        if (player != null)
            lastPlayerTransform = player;

        // ★ 兜底：如果还没任何东西注册，就扫一遍场景，把所有 RevertibleObject 加进来
        if (revertibles.Count == 0)
        {
            var all = FindObjectsOfType<RevertibleObject>();
            foreach (var r in all)
                Register(r);

            Debug.Log($"[Checkpoint] Auto-registered {all.Length} RevertibleObjects.");
        }

        foreach (var r in revertibles)
            r.SaveState();

        Debug.Log($"✅ Checkpoint saved: {point.name} (player={player?.name})");

        // ===== NEW: Sequential compass marker handling =====
        AdvanceCompassMarkerIfThisIsNext(point);
    }

    private void AdvanceCompassMarkerIfThisIsNext(Transform point)
    {
        if (checkpointOrder == null || checkpointOrder.Count == 0) return;

        // Determine which checkpoint index this point is in the ordered list
        int idx = checkpointOrder.IndexOf(point);
        if (idx < 0)
        {
            // Not part of the ordered list (still a valid checkpoint, just no compass step)
            return;
        }

        // Only advance if player reached the current "next" checkpoint
        // (prevents weird jumps if they touch checkpoint #3 before #2)
        if (idx == currentCheckpointIndex + 1)
        {
            int oldIndex = currentCheckpointIndex;
            currentCheckpointIndex = idx;

            ShowOnlyMarkerForIndex(currentCheckpointIndex + 1);
        }
        else
        {
            // If you want it to *force* sync instead, uncomment:
            // HideMarkerForIndex(currentCheckpointIndex);
            // currentCheckpointIndex = idx;
            // ShowMarkerForIndex(currentCheckpointIndex + 1);
        }
    }

    private void ShowOnlyMarkerForIndex(int idx)
    {
        if (compass == null) return;
        if (checkpointMarkerIDs == null) return;
        if (idx < 0 || idx >= checkpointMarkerIDs.Count)
        {
            // End of checkpoint chain -> hide everything
            compass.HideAllMarkers();
            return;
        }

        // ✅ this is the key line that prevents 2 markers:
        compass.HideAllMarkers();

        string id = checkpointMarkerIDs[idx];
        if (!string.IsNullOrWhiteSpace(id))
            compass.ShowMarker(id);
    }

    private void ShowMarkerForIndex(int idx)
    {
        if (compass == null) return;
        if (checkpointMarkerIDs == null) return;
        if (idx < 0 || idx >= checkpointMarkerIDs.Count) return;

        string id = checkpointMarkerIDs[idx];
        if (!string.IsNullOrWhiteSpace(id))
            compass.ShowMarker(id);
    }

    private void HideMarkerForIndex(int idx)
    {
        if (compass == null) return;
        if (checkpointMarkerIDs == null) return;
        if (idx < 0 || idx >= checkpointMarkerIDs.Count) return;

        string id = checkpointMarkerIDs[idx];
        if (!string.IsNullOrWhiteSpace(id))
            compass.HideMarker(id);
    }

    public void RespawnToCheckpoint()
    {
        if (!HasCheckpoint)
        {
            Debug.LogWarning("RespawnToCheckpoint called but no checkpoint exists.");
            return;
        }

        if (lastPlayerTransform == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) lastPlayerTransform = go.transform;
        }

        if (lastPlayerTransform == null)
        {
            Debug.LogWarning("RespawnToCheckpoint: no player transform recorded/found.");
            return;
        }

        var cc = lastPlayerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        lastPlayerTransform.position = currentCheckpoint.position;
        lastPlayerTransform.rotation = currentCheckpoint.rotation;

        foreach (var r in revertibles)
            r.RestoreState();

        var rb = lastPlayerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cc != null) cc.enabled = true;

        Debug.Log("✅ Respawned to checkpoint + restored revertibles");
    }
}
