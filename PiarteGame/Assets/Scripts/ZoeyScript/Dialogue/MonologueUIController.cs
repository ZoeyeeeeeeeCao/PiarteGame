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

    [Header("Auto Next (when voice ends)")]
    public bool autoNextWhenVoiceEnds = true;
    public float autoNextDelay = 0.1f;

    [Header("Auto End When No Voice")]
    public bool autoEndWhenNoVoice = true;   // ✅ NEW: 没语音也能自动结束
    public float autoNoVoiceDelay = 0.8f;    // ✅ NEW

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

    // ✅ auto-next / auto-end coroutine
    private Coroutine autoRoutine;

    // ✅ track current voice timing (PlayOneShot safe)
    private float currentVoiceLen = 0f;
    private float currentVoiceStartUnscaled = 0f;

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

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = null;

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

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = null;

        // play voice first and remember timing
        currentVoiceLen = PlayVoiceForLine(i);
        currentVoiceStartUnscaled = Time.unscaledTime;

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

        // ✅ after typing ends, schedule auto next/end
        TryScheduleAutoAdvanceOrEnd();
    }

    void FinishTypingInstant()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;

        typing = false;

        if (contentText && data != null && index < data.lines.Length)
            contentText.text = data.lines[index];

        // ✅ pressing Enter to finish typing should ALSO keep auto behavior
        TryScheduleAutoAdvanceOrEnd();
    }

    void TryScheduleAutoAdvanceOrEnd()
    {
        if (!showing) return;

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = null;

        // only do auto if enabled
        if (!autoNextWhenVoiceEnds && !autoEndWhenNoVoice) return;

        // if there is voice: wait remaining voice + delay
        if (autoNextWhenVoiceEnds && currentVoiceLen > 0.01f)
        {
            float elapsed = Time.unscaledTime - currentVoiceStartUnscaled;
            float remaining = Mathf.Max(0f, currentVoiceLen - elapsed);
            autoRoutine = StartCoroutine(AutoAdvanceOrEndAfterDelay(index, remaining + Mathf.Max(0f, autoNextDelay)));
            return;
        }

        // no voice: optionally auto end (or auto advance) — for monologue we usually end when reaches last line
        if (autoEndWhenNoVoice)
        {
            autoRoutine = StartCoroutine(AutoAdvanceOrEndAfterDelay(index, Mathf.Max(0f, autoNoVoiceDelay)));
        }
    }

    IEnumerator AutoAdvanceOrEndAfterDelay(int lineIndexAtStart, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (!showing) yield break;
        if (typing) yield break;
        if (data == null) yield break;
        if (index != lineIndexAtStart) yield break;

        // ✅ same behavior as pressing Enter:
        Next();
    }

    void Next()
    {
        // manual next cancels pending auto
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = null;

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
        typeRoutine = null;

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = null;

        typing = false;

        if (stopPreviousOnNext && voiceSource)
            voiceSource.Stop();

        ShowPanelAnimated(false);
        ApplyLocks(false);
    }

    float PlayVoiceForLine(int i)
    {
        if (!voiceSource) return 0f;
        if (data == null || data.voiceClips == null) return 0f;
        if (i < 0 || i >= data.voiceClips.Length) return 0f;

        var clip = data.voiceClips[i];
        if (!clip) return 0f;

        if (stopPreviousOnNext) voiceSource.Stop();
        voiceSource.PlayOneShot(clip, voiceVolume);
        return clip.length;
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
        if (disablePlayerScripts != null)
        {
            for (int i = 0; i < disablePlayerScripts.Length; i++)
                if (disablePlayerScripts[i])
                    disablePlayerScripts[i].enabled = !on;
        }

        if (playerAnimator && disableAnimatorComponentWhileShowing)
        {
            if (on)
            {
                animatorWasEnabled = playerAnimator.enabled;

                if (animatorWasEnabled && !string.IsNullOrEmpty(idleFullPath))
                {
                    playerAnimator.Play(idleFullPath, 0, 0f);
                    playerAnimator.Update(0f);
                }

                playerAnimator.enabled = false;
            }
            else
            {
                playerAnimator.enabled = animatorWasEnabled;
            }
        }

        SetOtherUIActive(!on);

        if (pauseTimeScale)
            Time.timeScale = on ? 0f : 1f;

        if (showCursor)
        {
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = on;
        }
    }
}
