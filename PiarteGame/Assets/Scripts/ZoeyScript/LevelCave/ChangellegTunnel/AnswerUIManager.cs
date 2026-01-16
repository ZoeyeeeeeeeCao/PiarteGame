using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AnswerUIManager : MonoBehaviour
{
    public static AnswerUIManager Instance;

    [Header("Answer UI (Screen Space)")]
    [Tooltip("Canvas root or a parent object. Can stay active; we will toggle panel.")]
    public GameObject uiRoot;

    [Tooltip("Drag the PANEL RectTransform you want to slide (NOT the canvas).")]
    public RectTransform uiPanel;

    public TMP_Text mainText;

    [Header("Buttons")]
    public Button proceedButton;
    public Button exitButton;

    [Header("Pause & Cursor")]
    public bool pauseTimeWhileShowing = true;
    public bool showCursorWhileShowing = true;

    [Header("Slide Animation")]
    public float slideDuration = 0.25f;
    public float slideOffsetY = -300f;

    [Header("Typewriter")]
    public float charInterval = 0.03f;

    private AnswerDoor currentDoor;
    private bool showing;

    private bool typing;
    private Coroutine slideRoutine;
    private Coroutine typeRoutine;
    private Vector2 panelTargetPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (proceedButton)
        {
            proceedButton.onClick.RemoveAllListeners();
            proceedButton.onClick.AddListener(OnProceedClicked);
        }

        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitClicked);
        }

        // Cache target position for sliding
        if (uiPanel)
            panelTargetPos = uiPanel.anchoredPosition;
        else
            Debug.LogWarning("[AnswerUIManager] uiPanel is NULL. Slide animation won't work. Please assign a Panel RectTransform.");

        Hide(immediate: true);
    }

    public void Show(AnswerDoor door)
    {
        currentDoor = door;
        showing = true;

        if (uiRoot) uiRoot.SetActive(true);

        // Show & slide up
        if (uiPanel)
        {
            uiPanel.gameObject.SetActive(true);
            ShowPanelAnimated(true);
        }

        // Typewriter text
        if (mainText)
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypeText(door != null ? door.uiText : ""));
        }

        ApplyPause(true);
    }

    public void Hide(bool immediate = false)
    {
        showing = false;
        currentDoor = null;

        // Stop typing
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typing = false;

        if (immediate)
        {
            // Immediately hide without sliding
            if (uiPanel) uiPanel.gameObject.SetActive(false);
            if (uiRoot) uiRoot.SetActive(false);
            ApplyPause(false);
            return;
        }

        // Slide down, then disable
        if (uiPanel && uiPanel.gameObject.activeSelf)
        {
            ShowPanelAnimated(false);
        }
        else
        {
            if (uiRoot) uiRoot.SetActive(false);
        }

        ApplyPause(false);
    }

    private void OnProceedClicked()
    {
        // If text still typing, first reveal full text
        if (typing)
        {
            FinishTypingInstant();
            return;
        }

        currentDoor?.ConfirmProceed();
        Hide();
    }

    private void OnExitClicked()
    {
        // If text still typing, first reveal full text
        if (typing)
        {
            FinishTypingInstant();
            return;
        }

        Hide();
    }

    IEnumerator TypeText(string line)
    {
        typing = true;
        mainText.text = "";

        if (string.IsNullOrEmpty(line))
        {
            typing = false;
            yield break;
        }

        foreach (char c in line)
        {
            mainText.text += c;
            yield return new WaitForSecondsRealtime(charInterval);
        }

        typing = false;
    }

    void FinishTypingInstant()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);

        // show full text
        if (mainText)
            mainText.text = currentDoor != null ? currentDoor.uiText : "";

        typing = false;
    }

    void ShowPanelAnimated(bool show)
    {
        if (!uiPanel) return;

        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlidePanel(show));
    }

    IEnumerator SlidePanel(bool show)
    {
        if (show)
        {
            uiPanel.anchoredPosition = panelTargetPos + Vector2.up * slideOffsetY;
        }

        Vector2 start = uiPanel.anchoredPosition;
        Vector2 end = show ? panelTargetPos : (panelTargetPos + Vector2.up * slideOffsetY);

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            uiPanel.anchoredPosition = Vector2.Lerp(start, end, t / slideDuration);
            yield return null;
        }

        uiPanel.anchoredPosition = end;

        if (!show)
        {
            uiPanel.gameObject.SetActive(false);
            if (uiRoot) uiRoot.SetActive(false);
        }
    }

    void ApplyPause(bool showingUI)
    {
        if (pauseTimeWhileShowing)
            Time.timeScale = showingUI ? 0f : 1f;

        if (showCursorWhileShowing)
        {
            Cursor.lockState = showingUI ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = showingUI;
        }
    }

    public bool IsShowing => showing;
}
