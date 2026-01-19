using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NpcDialogueController_Space_TMP : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("Primary next key (e.g., Space)")]
    public KeyCode nextKey = KeyCode.Space;

    [Tooltip("Secondary next key (e.g., Enter). Set to None if you don't want it.")]
    public KeyCode altNextKey = KeyCode.Return; // ✅ NEW (Enter)

    [Tooltip("Optional: World-space UI / sprite that shows 'Press E'")]
    public GameObject pressEIndicator;

    [Header("Dialogue UI (TMP)")]
    [Tooltip("Canvas root GameObject (can stay active; we will show/hide panel)")]
    public GameObject dialogueCanvas;

    [Tooltip("IMPORTANT: Drag the Dialogue PANEL (RectTransform) here, not the Canvas.")]
    public RectTransform dialoguePanel;

    public TMP_Text npcNameText;
    public TMP_Text dialogueText;

    [Header("NPC Name")]
    public string npcName = "NPC";

    [Header("Dialogue Content")]
    [TextArea(2, 6)] public string[] firstDialogue;
    [TextArea(2, 6)] public string[] secondDialogue;
    [TextArea(2, 6)] public string[] notReadyDialogue;

    [Header("Voice Clips (one clip per line, optional)")]
    public AudioClip[] firstVoice;
    public AudioClip[] secondVoice;
    public AudioClip[] notReadyVoice;

    [Header("Audio Settings")]
    public AudioSource voiceSource;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool stopPreviousOnNext = true;

    [Header("Auto Next (when voice ends)")]
    public bool autoNextWhenVoiceEnds = true;
    public float autoNextDelay = 0.1f;

    [Header("Auto Advance When No Voice")]
    public bool autoAdvanceWhenNoVoice = true;     // ✅ NEW: 没有语音也能自动
    public float autoNoVoiceDelay = 0.8f;          // ✅ NEW: 无语音时的自动间隔

    [Header("Optional: Pause game while talking (PC)")]
    public bool pauseTimeWhileTalking = false;
    public bool showCursorWhileTalking = false;

    [Header("UI Animation")]
    public float slideDuration = 0.25f;
    public float slideOffsetY = -300f;

    [Header("Typewriter")]
    public float charInterval = 0.03f;

    [Header("Optional UI Lock (Hide While Talking)")]
    [Tooltip("Drag any number of UI roots/Canvases you want to disable while dialogue is showing.")]
    public List<GameObject> uiToDisableWhileTalking = new List<GameObject>();

    private readonly Dictionary<GameObject, bool> initialUIStates = new Dictionary<GameObject, bool>();

    int dialogueIndex;
    bool playerInRange;
    bool dialoguePlaying;
    bool typing;

    string[] activeDialogue;
    AudioClip[] activeVoice;

    Vector2 panelTargetPos;
    Coroutine slideRoutine;
    Coroutine typeRoutine;

    // auto-next coroutine
    Coroutine autoNextRoutine;

    // ✅ Track current voice timing (so FinishTypingInstant can still auto-advance correctly)
    float currentVoiceLen = 0f;
    float currentVoiceStartUnscaled = 0f;

    enum TalkState { First, Second, Done }
    TalkState talkState = TalkState.First;

    void Start()
    {
        if (pressEIndicator) pressEIndicator.SetActive(false);

        if (npcNameText) npcNameText.text = npcName;

        if (!voiceSource)
        {
            voiceSource = GetComponent<AudioSource>();
            if (!voiceSource) voiceSource = gameObject.AddComponent<AudioSource>();
        }
        voiceSource.playOnAwake = false;

        if (dialogueCanvas) dialogueCanvas.SetActive(false);

        if (dialoguePanel)
        {
            panelTargetPos = dialoguePanel.anchoredPosition;
            dialoguePanel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[{name}] dialoguePanel is NULL. Slide animation won't work. Please assign the Panel RectTransform.");
        }

        CacheInitialUIStates();
    }

    void CacheInitialUIStates()
    {
        initialUIStates.Clear();
        if (uiToDisableWhileTalking == null) return;

        foreach (var go in uiToDisableWhileTalking)
        {
            if (!go) continue;
            if (!initialUIStates.ContainsKey(go))
                initialUIStates.Add(go, go.activeSelf);
        }
    }

    void SetOtherUIActive(bool active)
    {
        if (uiToDisableWhileTalking == null) return;

        foreach (var go in uiToDisableWhileTalking)
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

    void Update()
    {
        if (!playerInRange) return;

        if (!dialoguePlaying)
        {
            if (Input.GetKeyDown(interactKey))
                StartDialogue();
            return;
        }

        bool nextPressed =
            Input.GetKeyDown(nextKey) ||
            (altNextKey != KeyCode.None && Input.GetKeyDown(altNextKey));

        if (nextPressed)
        {
            if (typing) FinishTypingInstant();
            else NextLine();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;

        if (talkState != TalkState.Done && pressEIndicator)
            pressEIndicator.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        if (pressEIndicator) pressEIndicator.SetActive(false);
    }

    void StartDialogue()
    {
        if (talkState == TalkState.Done) return;

        dialoguePlaying = true;
        dialogueIndex = 0;

        if (pressEIndicator) pressEIndicator.SetActive(false);

        SetOtherUIActive(false);

        if (dialogueCanvas) dialogueCanvas.SetActive(true);
        ShowPanelAnimated(true);

        if (npcNameText) npcNameText.text = npcName;

        PickDialogueAndVoiceForThisTalk();

        if (activeDialogue == null || activeDialogue.Length == 0)
        {
            EndDialogue();
            return;
        }

        if (autoNextRoutine != null) StopCoroutine(autoNextRoutine);
        autoNextRoutine = null;

        ApplyTalkingPause(true);
        ShowCurrentLine();
    }

    void PickDialogueAndVoiceForThisTalk()
    {
        if (talkState == TalkState.First)
        {
            activeDialogue = firstDialogue;
            activeVoice = firstVoice;
            return;
        }

        if (talkState == TalkState.Second)
        {
            bool ready = QuestFinalSceneManager.Instance != null &&
                         QuestFinalSceneManager.Instance.AllBraziersLit();

            if (!ready && notReadyDialogue != null && notReadyDialogue.Length > 0)
            {
                activeDialogue = notReadyDialogue;
                activeVoice = notReadyVoice;
                return;
            }

            activeDialogue = secondDialogue;
            activeVoice = secondVoice;
            return;
        }

        activeDialogue = null;
        activeVoice = null;
    }

    void ShowCurrentLine()
    {
        if (!dialogueText) return;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        if (autoNextRoutine != null) StopCoroutine(autoNextRoutine);
        autoNextRoutine = null;

        // play voice & remember timing
        currentVoiceLen = PlayVoiceForLine(dialogueIndex);
        currentVoiceStartUnscaled = Time.unscaledTime;

        typeRoutine = StartCoroutine(TypeText(activeDialogue[dialogueIndex]));
    }

    IEnumerator TypeText(string line)
    {
        typing = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(charInterval);
        }

        typing = false;

        TryScheduleAutoAdvance();
    }

    void FinishTypingInstant()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;

        if (dialogueText && activeDialogue != null && dialogueIndex < activeDialogue.Length)
            dialogueText.text = activeDialogue[dialogueIndex];

        typing = false;

        // ✅ 按键把字瞬间显示完后，也一样触发“等语音播完自动下一句/自动收起”
        TryScheduleAutoAdvance();
    }

    void TryScheduleAutoAdvance()
    {
        if (!dialoguePlaying) return;

        if (autoNextRoutine != null) StopCoroutine(autoNextRoutine);
        autoNextRoutine = null;

        if (!autoNextWhenVoiceEnds) return;

        int lineIndexAtStart = dialogueIndex;

        // 有语音：等剩余语音播完
        if (currentVoiceLen > 0.01f)
        {
            float elapsed = Time.unscaledTime - currentVoiceStartUnscaled;
            float remaining = Mathf.Max(0f, currentVoiceLen - elapsed);

            autoNextRoutine = StartCoroutine(AutoNextAfterDelay(lineIndexAtStart, remaining + Mathf.Max(0f, autoNextDelay)));
            return;
        }

        // 无语音：也自动（这样最后一句也能自动 slide down 结束）
        if (autoAdvanceWhenNoVoice)
        {
            autoNextRoutine = StartCoroutine(AutoNextAfterDelay(lineIndexAtStart, Mathf.Max(0f, autoNoVoiceDelay)));
        }
    }

    IEnumerator AutoNextAfterDelay(int lineIndexAtStart, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (!dialoguePlaying) yield break;
        if (typing) yield break;
        if (activeDialogue == null) yield break;
        if (dialogueIndex != lineIndexAtStart) yield break;

        // ✅ 这里会在最后一句时自动触发 EndDialogue() → slide down
        NextLine();
    }

    float PlayVoiceForLine(int index)
    {
        if (!voiceSource) return 0f;
        if (activeVoice == null) return 0f;
        if (index < 0 || index >= activeVoice.Length) return 0f;

        var clip = activeVoice[index];
        if (!clip) return 0f;

        if (stopPreviousOnNext) voiceSource.Stop();
        voiceSource.PlayOneShot(clip, voiceVolume);

        return clip.length;
    }

    void NextLine()
    {
        if (autoNextRoutine != null) StopCoroutine(autoNextRoutine);
        autoNextRoutine = null;

        dialogueIndex++;

        if (activeDialogue == null || dialogueIndex >= activeDialogue.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void EndDialogue()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;

        if (autoNextRoutine != null) StopCoroutine(autoNextRoutine);
        autoNextRoutine = null;

        typing = false;

        if (voiceSource && stopPreviousOnNext)
            voiceSource.Stop();

        dialoguePlaying = false;
        ApplyTalkingPause(false);

        ShowPanelAnimated(false);

        SetOtherUIActive(true);

        if (talkState == TalkState.First)
        {
            QuestFinalSceneManager.Instance?.OnNpcTalk_StartQuest();
            talkState = TalkState.Second;
        }
        else if (talkState == TalkState.Second)
        {
            bool ready = QuestFinalSceneManager.Instance != null &&
                         QuestFinalSceneManager.Instance.AllBraziersLit();

            if (ready)
            {
                QuestFinalSceneManager.Instance.OnNpcTalk_OpenDoorIfReady();
                talkState = TalkState.Done;
            }
        }

        if (playerInRange && talkState != TalkState.Done && pressEIndicator)
            pressEIndicator.SetActive(true);
    }

    void ShowPanelAnimated(bool show)
    {
        if (!dialoguePanel) return;

        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlidePanel(show));
    }

    IEnumerator SlidePanel(bool show)
    {
        if (show)
        {
            dialoguePanel.gameObject.SetActive(true);
            dialoguePanel.anchoredPosition = panelTargetPos + Vector2.up * slideOffsetY;
        }

        Vector2 start = dialoguePanel.anchoredPosition;
        Vector2 end = show ? panelTargetPos : (panelTargetPos + Vector2.up * slideOffsetY);

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            dialoguePanel.anchoredPosition = Vector2.Lerp(start, end, t / slideDuration);
            yield return null;
        }

        dialoguePanel.anchoredPosition = end;

        if (!show)
        {
            dialoguePanel.gameObject.SetActive(false);
            if (dialogueCanvas) dialogueCanvas.SetActive(false);
        }
    }

    void ApplyTalkingPause(bool talking)
    {
        if (!pauseTimeWhileTalking) return;

        Time.timeScale = talking ? 0f : 1f;

        if (showCursorWhileTalking)
        {
            Cursor.lockState = talking ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = talking;
        }
    }
}
