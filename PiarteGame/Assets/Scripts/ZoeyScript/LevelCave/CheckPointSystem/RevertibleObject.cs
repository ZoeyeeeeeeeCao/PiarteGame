using UnityEngine;

public class RevertibleObject : MonoBehaviour
{
    private Vector3 savedPos;
    private Quaternion savedRot;
    private bool savedActive;

    private void Awake()
    {
        // 先存一份初始状态（防止还没触发checkpoint时也能恢复到初始）
        SaveState();
    }

    private void Start()
    {
        // ★ 放在 Start 里注册：此时 LevelCheckpointManager 的 Awake 一定已经跑完
        if (LevelCheckpointManager.Instance != null)
        {
            LevelCheckpointManager.Instance.Register(this);
        }
        else
        {
            Debug.LogWarning($"{name}: LevelCheckpointManager.Instance is null in Start, will try lazy register later.");
        }
    }

    private void OnEnable()
    {
        // 再兜一层：如果之前没注册成功，这里再试一次
        if (LevelCheckpointManager.Instance != null)
        {
            LevelCheckpointManager.Instance.Register(this);
        }
    }

    public void SaveState()
    {
        savedPos = transform.position;
        savedRot = transform.rotation;
        savedActive = gameObject.activeSelf;
    }

    public void RestoreState()
    {
        transform.position = savedPos;
        transform.rotation = savedRot;
        gameObject.SetActive(savedActive);
    }

    // 用这个代替 Destroy
    public void FakeDestroy()
    {
        gameObject.SetActive(false);
    }
}
