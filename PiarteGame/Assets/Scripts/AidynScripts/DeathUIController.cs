using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Image blackFadeImage;
    [SerializeField] private Image deathTextImage;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject mainMenuButton;

    [Header("Blackout Effect")]
    [SerializeField] private bool useVignetteEffect = false;
    [SerializeField] private float vignetteSpreadDuration = 0.3f;

    [Header("Animation Settings")]
    [SerializeField] private float textFadeDelay = 0.8f;
    [SerializeField] private float textFadeDuration = 2.5f;
    [SerializeField] private float textScaleFrom = 0.5f;
    [SerializeField] private float textScaleTo = 1f;
    [SerializeField] private float buttonFadeDelay = 0.5f;
    [SerializeField] private float buttonFadeDuration = 1f;

    [Header("Pause Game When Shown")]
    [SerializeField] private bool pauseGame = true;

    [Header("Gameplay UI (Optional)")]
    [SerializeField] private GameObject questCanvas;
    [SerializeField] private GameObject conversationCanvas;

    [Header("Death Audio")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip deathSceneSound;
    [SerializeField] private float deathSoundVolume = 1f;
    [SerializeField] private float deathSoundDelay = 0.5f;

    private bool questCanvasWasActiveBeforeDeath;
    private bool conversationCanvasWasActiveBeforeDeath;

    private Coroutine animationRoutine;
    private bool subscribed;

    private CanvasGroup restartButtonGroup;
    private CanvasGroup mainMenuButtonGroup;

    private Material vignetteMaterial;
    private static readonly int VignetteRadius = Shader.PropertyToID("_Radius");

    private void Awake()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (blackFadeImage != null)
        {
            Color c = blackFadeImage.color;
            c.a = 0f;
            blackFadeImage.color = c;

            if (useVignetteEffect)
            {
                vignetteMaterial = new Material(Shader.Find("UI/Default"));
                blackFadeImage.material = vignetteMaterial;
            }
        }

        if (deathTextImage != null)
        {
            Color c = deathTextImage.color;
            c.a = 0f;
            deathTextImage.color = c;
            deathTextImage.transform.localScale = Vector3.one * textScaleFrom;
        }

        if (restartButton != null)
        {
            restartButtonGroup = restartButton.GetComponent<CanvasGroup>();
            if (restartButtonGroup == null)
                restartButtonGroup = restartButton.AddComponent<CanvasGroup>();
            restartButtonGroup.alpha = 0f;
            restartButtonGroup.interactable = false;
        }

        if (mainMenuButton != null)
        {
            mainMenuButtonGroup = mainMenuButton.GetComponent<CanvasGroup>();
            if (mainMenuButtonGroup == null)
                mainMenuButtonGroup = mainMenuButton.AddComponent<CanvasGroup>();
            mainMenuButtonGroup.alpha = 0f;
            mainMenuButtonGroup.interactable = false;
        }

        if (deathAudioSource == null)
        {
            GameObject audioObj = new GameObject("DeathSceneAudio");
            audioObj.transform.SetParent(transform);
            deathAudioSource = audioObj.AddComponent<AudioSource>();
            deathAudioSource.playOnAwake = false;
        }
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
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(DeathAnimationSequence());
    }

    private IEnumerator DeathAnimationSequence()
    {
        if (deathPanel != null)
            deathPanel.SetActive(true);

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseGame)
            Time.timeScale = 0f;

        if (useVignetteEffect)
        {
            yield return StartCoroutine(VignetteBlackout());
        }
        else
        {
            if (blackFadeImage != null)
            {
                Color c = blackFadeImage.color;
                c.a = 1f;
                blackFadeImage.color = c;
            }
        }

        yield return new WaitForSecondsRealtime(deathSoundDelay);

        if (deathSceneSound != null && deathAudioSource != null)
        {
            deathAudioSource.PlayOneShot(deathSceneSound, deathSoundVolume);
        }

        yield return new WaitForSecondsRealtime(textFadeDelay - deathSoundDelay);

        yield return StartCoroutine(AnimateDeathText());

        yield return new WaitForSecondsRealtime(buttonFadeDelay);
        yield return StartCoroutine(FadeInButtons());
    }

    private IEnumerator VignetteBlackout()
    {
        if (blackFadeImage == null) yield break;

        Color c = blackFadeImage.color;
        c.a = 1f;
        blackFadeImage.color = c;

        float elapsed = 0f;
        while (elapsed < vignetteSpreadDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / vignetteSpreadDuration;

            blackFadeImage.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);

            yield return null;
        }

        blackFadeImage.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateDeathText()
    {
        if (deathTextImage == null) yield break;

        float elapsed = 0f;
        Color startColor = deathTextImage.color;
        startColor.a = 0f;
        Color targetColor = startColor;
        targetColor.a = 1f;

        Vector3 startScale = Vector3.one * textScaleFrom;
        Vector3 targetScale = Vector3.one * textScaleTo;

        while (elapsed < textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / textFadeDuration;

            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            deathTextImage.color = Color.Lerp(startColor, targetColor, t);

            deathTextImage.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            yield return null;
        }

        deathTextImage.color = targetColor;
        deathTextImage.transform.localScale = targetScale;
    }

    private IEnumerator FadeInButtons()
    {
        float elapsed = 0f;
        float startAlpha = 0f;
        float targetAlpha = 1f;

        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / buttonFadeDuration;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (restartButtonGroup != null)
                restartButtonGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);

            if (mainMenuButtonGroup != null)
                mainMenuButtonGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);

            yield return null;
        }

        if (restartButtonGroup != null)
        {
            restartButtonGroup.alpha = targetAlpha;
            restartButtonGroup.interactable = true;
        }

        if (mainMenuButtonGroup != null)
        {
            mainMenuButtonGroup.alpha = targetAlpha;
            mainMenuButtonGroup.interactable = true;
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        if (PlayerHealthController.Instance != null)
            PlayerHealthController.Instance.ResetHealth();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var cp = LevelCheckpointManager.Instance;
        if (cp != null && cp.HasCheckpoint)
        {
            if (deathPanel != null)
                deathPanel.SetActive(false);

            ResetUIElements();

            if (questCanvas != null && questCanvasWasActiveBeforeDeath)
                questCanvas.SetActive(true);

            if (conversationCanvas != null && conversationCanvasWasActiveBeforeDeath)
                conversationCanvas.SetActive(true);

            cp.RespawnToCheckpoint();

            if (PlayerTriggerResetOnDeath.Instance != null)
                PlayerTriggerResetOnDeath.Instance.OnRespawn();

            Debug.Log("Restart -> Respawn to checkpoint (no scene reload)");
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(0);
    }

    private void ResetUIElements()
    {
        if (blackFadeImage != null)
        {
            Color c = blackFadeImage.color;
            c.a = 0f;
            blackFadeImage.color = c;
            blackFadeImage.transform.localScale = Vector3.one;
        }

        if (deathTextImage != null)
        {
            Color c = deathTextImage.color;
            c.a = 0f;
            deathTextImage.color = c;
            deathTextImage.transform.localScale = Vector3.one * textScaleFrom;
        }

        if (restartButtonGroup != null)
        {
            restartButtonGroup.alpha = 0f;
            restartButtonGroup.interactable = false;
        }

        if (mainMenuButtonGroup != null)
        {
            mainMenuButtonGroup.alpha = 0f;
            mainMenuButtonGroup.interactable = false;
        }
    }
}