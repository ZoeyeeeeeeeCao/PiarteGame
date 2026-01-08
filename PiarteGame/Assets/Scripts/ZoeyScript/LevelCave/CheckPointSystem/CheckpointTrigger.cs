using UnityEngine;

[DisallowMultipleComponent]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Play 模式下自动隐藏自己的 Mesh / Renderer")]
    public bool hideRenderersOnPlay = true;

    private void Start()
    {
        if (hideRenderersOnPlay)
        {
            // 把自己和子物体上的所有 Renderer 关掉（MeshRenderer / SkinnedMeshRenderer 都包括）
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (LevelCheckpointManager.Instance != null)
        {
            // 记录：这个存档点 + 当前 player 位置
            LevelCheckpointManager.Instance.SetCheckpoint(transform, other.transform);
        }
    }
}
