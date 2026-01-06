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

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
