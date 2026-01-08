using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject deathPanel;

    [Header("Delay")]
    [SerializeField] private float showDelay = 0.6f;

    [Header("Pause Game When Shown")]
    [SerializeField] private bool pauseGame = true;

    // =========================
    // Gameplay UI to hide while death panel is shown (all optional)
    // =========================
    [Header("Gameplay UI (Optional)")]
    [SerializeField] private GameObject questCanvas;        // 任务栏
    [SerializeField] private GameObject conversationCanvas; // 对话Canvas

    // ⭐ 新增：记录“死亡前是否是开启状态”
    private bool questCanvasWasActiveBeforeDeath;
    private bool conversationCanvasWasActiveBeforeDeath;

    private Coroutine showRoutine;
    private bool subscribed;

    private void Awake()
    {
        // UI is recreated per scene → always start hidden
        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Wait for PlayerHealthController singleton
        while (PlayerHealthController.Instance == null)
            yield return null;

        if (!subscribed)
        {
            PlayerHealthController.Instance.OnDeath += HandleDeath;
            subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (subscribed && PlayerHealthController.Instance != null)
        {
            PlayerHealthController.Instance.OnDeath -= HandleDeath;
            subscribed = false;
        }
    }

    private void HandleDeath()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        // Delay using unscaled time
        float t = 0f;
        while (t < showDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (deathPanel != null)
            deathPanel.SetActive(true);

        // ⭐ 在隐藏之前，先记录当前是否是开启状态
        if (questCanvas != null)
        {
            questCanvasWasActiveBeforeDeath = questCanvas.activeSelf;
            if (questCanvasWasActiveBeforeDeath)
                questCanvas.SetActive(false);
        }

        if (conversationCanvas != null)
        {
            conversationCanvasWasActiveBeforeDeath = conversationCanvas.activeSelf;
            if (conversationCanvasWasActiveBeforeDeath)
                conversationCanvas.SetActive(false);
        }

        // ✅ ENABLE CURSOR FOR UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseGame)
            Time.timeScale = 0f;
    }

    // Hook this to Restart button OnClick
    public void Restart()
    {
        // Unpause
        Time.timeScale = 1f;

        // Reset health (controller persists)
        if (PlayerHealthController.Instance != null)
            PlayerHealthController.Instance.ResetHealth();

        // 🔒 Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var cp = LevelCheckpointManager.Instance;
        if (cp != null && cp.HasCheckpoint)
        {
            // 关闭死亡UI
            if (deathPanel != null)
                deathPanel.SetActive(false);

            // ✅ 只恢复那些“死亡前本来就是开的” Gameplay UI
            if (questCanvas != null && questCanvasWasActiveBeforeDeath)
                questCanvas.SetActive(true);

            if (conversationCanvas != null && conversationCanvasWasActiveBeforeDeath)
                conversationCanvas.SetActive(true);

            // 传送到 checkpoint + 恢复可回滚物体
            cp.RespawnToCheckpoint();

            // 通知玩家把 Collider 打开（触发器系统）
            if (PlayerTriggerResetOnDeath.Instance != null)
                PlayerTriggerResetOnDeath.Instance.OnRespawn();

            Debug.Log("✅ Restart -> Respawn to checkpoint (no scene reload)");
            return;
        }

        // 没有 checkpoint：走原逻辑 reload
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
