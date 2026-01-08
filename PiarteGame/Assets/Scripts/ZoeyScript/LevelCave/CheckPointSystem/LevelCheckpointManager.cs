using UnityEngine;
using System.Collections.Generic;

public class LevelCheckpointManager : MonoBehaviour
{
    public static LevelCheckpointManager Instance;

    private Transform currentCheckpoint;
    private Transform lastPlayerTransform;

    private readonly List<RevertibleObject> revertibles = new();

    public bool HasCheckpoint => currentCheckpoint != null;

    private void Awake()
    {
        Instance = this;
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
