using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MonologueUIController : MonoBehaviour
{
    [Header("UI (match your hierarchy)")]
    public GameObject canvasRoot;     // Canvas
    public RectTransform panel;       // Panel
    public TMP_Text nameText;         // TMP_Text: Name
    public TMP_Text contentText;      // TMP_Text: contentText

    [Header("Input")]
    public KeyCode nextKey = KeyCode.Return;

    [Header("Typewriter")]
    public float charInterval = 0.03f;

    [Header("Slide Animation")]
    public float slideDuration = 0.25f;
    public float slideOffsetY = -300f;

    [Header("Audio (optional)")]
    public AudioSource voiceSource;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool stopPreviousOnNext = true;

    [Header("Lock Player While Showing")]
    public MonoBehaviour[] disablePlayerScripts;

    [Header("Disable Other UI While Showing (optional)")]
    public List<GameObject> uiToDisableWhileShowing = new List<GameObject>();
    private readonly Dictionary<GameObject, bool> initialUIStates = new();

    [Header("Animator (Hard Freeze)")]
    public Animator playerAnimator;

    [Tooltip("Optional: full path to Idle state. Example: Base Layer.Locomotion.Idle")]
    public string idleFullPath = "Base Layer.Locomotion.Idle";

    [Tooltip("Disable Animator component during monologue (MOST RELIABLE).")]
    public bool disableAnimatorComponentWhileShowing = true;

    private bool animatorWasEnabled = true;

    [Header("Optional Pause")]
    public bool pauseTimeScale = false;
    public bool showCursor = false;

    // runtime
    private bool showing;
    private bool typing;
    private int index;
    private MonologueData data;

    private Vector2 targetPos;
    private Coroutine slideRoutine;
    private Coroutine typeRoutine;

    public bool IsShowing => showing;

    void Awake()
    {
        if (canvasRoot) canvasRoot.SetActive(false);

        if (panel)
        {
            targetPos = panel.anchoredPosition;
            panel.gameObject.SetActive(false);
        }

        if (!voiceSource)
            voiceSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!showing) return;

        if (Input.GetKeyDown(nextKey))
        {
            if (typing)
            {
                FinishTypingInstant();
                return;
            }

            Next();
        }
    }

    public void Play(MonologueData monologue)
    {
        if (monologue == null || monologue.lines == null || monologue.lines.Length == 0)
            return;

        data = monologue;
        index = 0;

        CacheInitialUIStates();

        showing = true;
        ApplyLocks(true);

        if (canvasRoot) canvasRoot.SetActive(true);
        ShowPanelAnimated(true);

        if (nameText) nameText.text = data.speakerName;

        ShowLine(index);
    }

    void ShowLine(int i)
    {
        if (!contentText || data == null) return;

        PlayVoiceForLine(i);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(data.lines[i]));
    }

    IEnumerator TypeText(string line)
    {
        typing = true;
        contentText.text = "";

        foreach (char c in line)
        {
            contentText.text += c;
            yield return new WaitForSecondsRealtime(charInterval);
        }

        typing = false;
    }

    void FinishTypingInstant()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typing = false;

        if (contentText && data != null && index < data.lines.Length)
            contentText.text = data.lines[index];
    }

    void Next()
    {
        index++;

        if (data == null || index >= data.lines.Length)
        {
            End();
            return;
        }

        ShowLine(index);
    }

    void End()
    {
        showing = false;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typing = false;

        if (stopPreviousOnNext && voiceSource)
            voiceSource.Stop();

        ShowPanelAnimated(false);
        ApplyLocks(false);
    }

    void PlayVoiceForLine(int i)
    {
        if (!voiceSource) return;
        if (data == null || data.voiceClips == null) return;
        if (i < 0 || i >= data.voiceClips.Length) return;

        var clip = data.voiceClips[i];
        if (!clip) return;

        if (stopPreviousOnNext) voiceSource.Stop();
        voiceSource.PlayOneShot(clip, voiceVolume);
    }

    void ShowPanelAnimated(bool show)
    {
        if (!panel) return;

        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlidePanel(show));
    }

    IEnumerator SlidePanel(bool show)
    {
        if (show)
        {
            panel.gameObject.SetActive(true);
            panel.anchoredPosition = targetPos + Vector2.up * slideOffsetY;
        }

        Vector2 start = panel.anchoredPosition;
        Vector2 end = show ? targetPos : (targetPos + Vector2.up * slideOffsetY);

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            panel.anchoredPosition = Vector2.Lerp(start, end, t / slideDuration);
            yield return null;
        }

        panel.anchoredPosition = end;

        if (!show)
        {
            panel.gameObject.SetActive(false);
            if (canvasRoot) canvasRoot.SetActive(false);
        }
    }

    void CacheInitialUIStates()
    {
        initialUIStates.Clear();
        foreach (var go in uiToDisableWhileShowing)
        {
            if (!go) continue;
            if (!initialUIStates.ContainsKey(go))
                initialUIStates.Add(go, go.activeSelf);
        }
    }

    void SetOtherUIActive(bool active)
    {
        foreach (var go in uiToDisableWhileShowing)
        {
            if (!go) continue;

            if (active)
            {
                if (initialUIStates.TryGetValue(go, out bool wasActive) && wasActive)
                    go.SetActive(true);
            }
            else
            {
                go.SetActive(false);
            }
        }
    }

    void ApplyLocks(bool on)
    {
        // Disable player control scripts
        if (disablePlayerScripts != null)
        {
            for (int i = 0; i < disablePlayerScripts.Length; i++)
                if (disablePlayerScripts[i])
                    disablePlayerScripts[i].enabled = !on;
        }

        // ✅ Hard stop Animator (recommended for your BlendTree controller)
        if (playerAnimator && disableAnimatorComponentWhileShowing)
        {
            if (on)
            {
                animatorWasEnabled = playerAnimator.enabled;

                // Try to snap to idle pose first (optional, but helps avoid freezing mid-run)
                if (animatorWasEnabled && !string.IsNullOrEmpty(idleFullPath))
                {
                    playerAnimator.Play(idleFullPath, 0, 0f);
                    playerAnimator.Update(0f);
                }

                playerAnimator.enabled = false; // HARD FREEZE (all layers stop)
            }
            else
            {
                playerAnimator.enabled = animatorWasEnabled;
            }
        }

        // Disable other UI (HUD etc.)
        SetOtherUIActive(!on);

        // Optional pause
        if (pauseTimeScale)
            Time.timeScale = on ? 0f : 1f;

        // Optional cursor
        if (showCursor)
        {
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = on;
        }
    }
}
