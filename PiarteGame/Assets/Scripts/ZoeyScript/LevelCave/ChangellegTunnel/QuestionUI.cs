using System.Collections;
using UnityEngine;
using TMPro;

public class QuestionUI : MonoBehaviour
{
    [Header("UI Root")]
    [Tooltip("Canvas root. Can be active all the time; we will show/hide panel.")]
    public GameObject uiCanvas;

    [Tooltip("Drag the PANEL (RectTransform) you want to slide, NOT the Canvas.")]
    public RectTransform uiPanel;

    public TMP_Text messageText;

    [Header("Message Content")]
    [TextArea]
    public string message = "Do you want to proceed?";

    [Header("Input")]
    public KeyCode proceedKey = KeyCode.Return;

    [Header("Pause")]
    public bool pauseTimeWhileShowing = true;
    public bool showCursorWhileShowing = true;

    [Header("Slide Animation")]
    public float slideDuration = 0.25f;
    public float slideOffsetY = -300f;

    [Header("Typewriter")]
    public float charInterval = 0.03f;

    bool playerInside;
    bool hasProceeded;
    bool showing;
    bool typing;

    Vector2 targetPos;
    Coroutine slideRoutine;
    Coroutine typeRoutine;

    void Start()
    {
        // Initial UI state
        if (uiCanvas) uiCanvas.SetActive(false);

        if (uiPanel)
        {
            targetPos = uiPanel.anchoredPosition;
            uiPanel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[{name}] uiPanel is NULL. Slide will not work. Please assign a Panel RectTransform.");
        }
    }

    void Update()
    {
        if (!showing) return;

        if (Input.GetKeyDown(proceedKey))
        {
            if (typing)
            {
                FinishTypingInstant();
                return;
            }

            if (!hasProceeded)
                Proceed();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        hasProceeded = false;

        ShowUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        hasProceeded = false;

        HideUI();
    }

    void ShowUI()
    {
        showing = true;

        if (uiCanvas) uiCanvas.SetActive(true);

        // Pause + cursor
        ApplyPause(true);

        // Set text (typewriter)
        if (messageText)
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypeText(message));
        }

        // Slide up
        ShowPanelAnimated(true);
    }

    void HideUI()
    {
        showing = false;

        // Stop typing
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typing = false;

        // Slide down then close
        ShowPanelAnimated(false);

        // Resume + cursor back
        ApplyPause(false);
    }

    void Proceed()
    {
        hasProceeded = true;

        Debug.Log("Proceed pressed");

        HideUI();

        // TODO: 在这里加你后续逻辑：
        // SceneManager.LoadScene(...)
        // doorAnimator.SetBool(...)
        // Timeline.Play()
    }

    IEnumerator TypeText(string line)
    {
        typing = true;
        messageText.text = "";

        foreach (char c in line)
        {
            messageText.text += c;
            yield return new WaitForSecondsRealtime(charInterval);
        }

        typing = false;
    }

    void FinishTypingInstant()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        if (messageText) messageText.text = message;
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
            uiPanel.gameObject.SetActive(true);
            uiPanel.anchoredPosition = targetPos + Vector2.up * slideOffsetY;
        }

        Vector2 start = uiPanel.anchoredPosition;
        Vector2 end = show ? targetPos : (targetPos + Vector2.up * slideOffsetY);

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
            if (uiCanvas) uiCanvas.SetActive(false);
        }
    }

    void ApplyPause(bool pause)
    {
        if (pauseTimeWhileShowing)
            Time.timeScale = pause ? 0f : 1f;

        if (showCursorWhileShowing)
        {
            Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = pause;
        }
    }
}
