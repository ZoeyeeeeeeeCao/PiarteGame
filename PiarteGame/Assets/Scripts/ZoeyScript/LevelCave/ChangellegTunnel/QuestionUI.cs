using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestionUI : MonoBehaviour
{
    [Header("UI Root")]
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

    [Header("Ignore These Colliders")]
    public Collider[] ignoredColliders;

    // ✅ NEW: Multiple UI roots to disable while QuestionUI is showing
    [Header("Optional UI Lock (Hide While Showing)")]
    [Tooltip("Drag any number of UI roots / Canvases you want to disable while this Question UI is showing.")]
    public List<GameObject> uiToDisableWhileShowing = new List<GameObject>();

    // ✅ Cache original active states
    private readonly Dictionary<GameObject, bool> initialUIStates = new Dictionary<GameObject, bool>();

    bool playerInside;
    bool hasProceeded;
    bool showing;
    bool typing;

    Vector2 targetPos;
    Coroutine slideRoutine;
    Coroutine typeRoutine;

    void Start()
    {
        if (uiCanvas) uiCanvas.SetActive(false);

        if (uiPanel)
        {
            targetPos = uiPanel.anchoredPosition;
            uiPanel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[{name}] uiPanel is NULL.");
        }

        CacheInitialUIStates();
    }

    void CacheInitialUIStates()
    {
        initialUIStates.Clear();

        if (uiToDisableWhileShowing == null) return;

        foreach (var go in uiToDisableWhileShowing)
        {
            if (!go) continue;
            if (!initialUIStates.ContainsKey(go))
                initialUIStates.Add(go, go.activeSelf);
        }
    }

    void SetOtherUIActive(bool active)
    {
        if (uiToDisableWhileShowing == null) return;

        foreach (var go in uiToDisableWhileShowing)
        {
            if (!go) continue;

            if (active)
            {
                // Restore only if it was originally active
                if (initialUIStates.TryGetValue(go, out bool wasActive) && wasActive)
                    go.SetActive(true);
            }
            else
            {
                go.SetActive(false);
            }
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

    bool IsIgnored(Collider col)
    {
        if (ignoredColliders == null) return false;

        foreach (var c in ignoredColliders)
        {
            if (c == col)
                return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (IsIgnored(other)) return;

        playerInside = true;
        hasProceeded = false;

        ShowUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (IsIgnored(other)) return;

        playerInside = false;
        hasProceeded = false;

        HideUI();
    }

    void ShowUI()
    {
        showing = true;

        if (uiCanvas) uiCanvas.SetActive(true);

        // ✅ Disable other UI
        SetOtherUIActive(false);

        ApplyPause(true);

        if (messageText)
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypeText(message));
        }

        ShowPanelAnimated(true);
    }

    void HideUI()
    {
        showing = false;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typing = false;

        ShowPanelAnimated(false);

        ApplyPause(false);

        // ✅ Restore other UI
        SetOtherUIActive(true);
    }

    void Proceed()
    {
        hasProceeded = true;
        Debug.Log("Proceed pressed");

        HideUI();

        // Your follow-up logic here
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
