using UnityEngine;
using System.Collections.Generic;
using FS_ThirdPerson;

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

    [Header("Respawn Options")]
    [Tooltip("Call Fantacode LocomotionController.HardResetForRespawn() to make sure speed is 0.")]
    [SerializeField] private bool resetFantacodeLocomotion = true;

    [Tooltip("After teleport, push slightly down to snap ground (recommended).")]
    [SerializeField] private bool snapToGround = true;

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

        // Sequential compass marker
        AdvanceCompassMarkerIfThisIsNext(point);
    }

    private void AdvanceCompassMarkerIfThisIsNext(Transform point)
    {
        if (checkpointOrder == null || checkpointOrder.Count == 0) return;

        int idx = checkpointOrder.IndexOf(point);
        if (idx < 0) return;

        if (idx == currentCheckpointIndex + 1)
        {
            currentCheckpointIndex = idx;
            ShowOnlyMarkerForIndex(currentCheckpointIndex + 1);
        }
    }

    private void ShowOnlyMarkerForIndex(int idx)
    {
        if (compass == null) return;
        if (checkpointMarkerIDs == null) return;

        if (idx < 0 || idx >= checkpointMarkerIDs.Count)
        {
            compass.HideAllMarkers();
            return;
        }

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

        // ====== ✅ KEY: Grab components ======
        var cc = lastPlayerTransform.GetComponent<CharacterController>();
        var loco = lastPlayerTransform.GetComponent<LocomotionController>(); // Fantacode
        var rb = lastPlayerTransform.GetComponent<Rigidbody>();

        // 1) Disable CharacterController first (important)
        if (cc != null) cc.enabled = false;

        // 2) Teleport
        lastPlayerTransform.position = currentCheckpoint.position;
        lastPlayerTransform.rotation = currentCheckpoint.rotation;

        // 3) Restore revertibles (world state)
        foreach (var r in revertibles)
            r.RestoreState();

        // 4) Clear physics velocity if any Rigidbody exists (some setups have both)
        if (rb != null)
        {
            // Unity 2022+ uses velocity/angularVelocity (linearVelocity is DOTS Physics)
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 5) ✅ Clear Fantacode locomotion internal velocity & states
        if (resetFantacodeLocomotion && loco != null)
        {
            loco.HardResetForRespawn(snapToGround);
        }

        // 6) Re-enable CharacterController LAST
        if (cc != null) cc.enabled = true;

        Debug.Log("✅ Respawned to checkpoint + restored revertibles + Fantacode velocity reset (speed=0)");
    }
}
