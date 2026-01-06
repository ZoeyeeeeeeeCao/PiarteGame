using System.Collections;
using UnityEngine;

public class DoorCameraCut : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("玩家第三人称 Camera（带 AudioListener 的那台）")]
    public Camera playerCamera;

    [Tooltip("门前展示用 Camera（不要挂 AudioListener）")]
    public Camera doorCamera;

    [Header("Timing")]
    [Tooltip("门前镜头停留时间（秒）")]
    public float holdTime = 3f;

    [Header("Optional: Disable Player Control Scripts")]
    [Tooltip("播放镜头时要临时禁用的脚本（如 ThirdPersonController）")]
    public MonoBehaviour[] disableDuringCut;

    [Header("Debug")]
    public bool debugLog = false;

    bool playing;
    bool subscribed;
    int originalPlayerMask;

    private void Awake()
    {
        // ✅ 门前相机开局必须关
        if (doorCamera) doorCamera.enabled = false;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // ✅ 防执行顺序：Start 更晚，通常能拿到 Instance
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (QuestFinalSceneManager.Instance == null) return;

        QuestFinalSceneManager.Instance.OnDoorOpened += PlayCut;
        subscribed = true;

        if (debugLog) Debug.Log("[DoorCameraCut] Subscribed to OnDoorOpened.");
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (QuestFinalSceneManager.Instance != null)
            QuestFinalSceneManager.Instance.OnDoorOpened -= PlayCut;

        subscribed = false;

        if (debugLog) Debug.Log("[DoorCameraCut] Unsubscribed from OnDoorOpened.");
    }

    public void PlayCut()
    {
        if (playing) return;
        StartCoroutine(CutRoutine());
    }

    IEnumerator CutRoutine()
    {
        playing = true;

        if (!playerCamera || !doorCamera)
        {
            Debug.LogError("[DoorCameraCut] Missing playerCamera or doorCamera reference!");
            playing = false;
            yield break;
        }

        // ✅ 保存玩家相机原始渲染层（关键）
        originalPlayerMask = playerCamera.cullingMask;

        // 禁用玩家控制（可选）
        if (disableDuringCut != null)
        {
            for (int i = 0; i < disableDuringCut.Length; i++)
                if (disableDuringCut[i]) disableDuringCut[i].enabled = false;
        }

        // =============================
        // ✅ 切到门前镜头（不断声版本）
        // 不关闭 playerCamera（AudioListener 还在）
        // 只让 playerCamera 不渲染任何东西
        // =============================
        playerCamera.cullingMask = 0;
        doorCamera.enabled = true;

        if (debugLog) Debug.Log("[DoorCameraCut] Switched to door camera (player audio stays).");

        yield return new WaitForSeconds(holdTime);

        // =============================
        // ✅ 切回玩家镜头
        // =============================
        doorCamera.enabled = false;
        playerCamera.cullingMask = originalPlayerMask;

        if (debugLog) Debug.Log("[DoorCameraCut] Switched back to player camera.");

        // 恢复玩家控制
        if (disableDuringCut != null)
        {
            for (int i = 0; i < disableDuringCut.Length; i++)
                if (disableDuringCut[i]) disableDuringCut[i].enabled = true;
        }

        playing = false;
    }
}
