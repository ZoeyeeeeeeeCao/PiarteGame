using UnityEngine;
using TMPro;
using System.Collections;

public class NpcDialogueController_Space_TMP : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public KeyCode nextKey = KeyCode.Space;

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

    [Header("Optional: Pause game while talking (PC)")]
    public bool pauseTimeWhileTalking = false;
    public bool showCursorWhileTalking = false;

    [Header("UI Animation")]
    public float slideDuration = 0.25f;
    public float slideOffsetY = -300f;

    [Header("Typewriter")]
    public float charInterval = 0.03f;

    int dialogueIndex;
    bool playerInRange;
    bool dialoguePlaying;
    bool typing;

    string[] activeDialogue;
    AudioClip[] activeVoice;

    Vector2 panelTargetPos;
    Coroutine slideRoutine;
    Coroutine typeRoutine;

    enum TalkState { First, Second, Done }
    TalkState talkState = TalkState.First;

    void Start()
    {
        if (pressEIndicator) pressEIndicator.SetActive(false);

        if (npcNameText) npcNameText.text = npcName;

        // AudioSource: use existing, do not force spatialBlend here
        if (!voiceSource)
        {
            voiceSource = GetComponent<AudioSource>();
            if (!voiceSource) voiceSource = gameObject.AddComponent<AudioSource>();
        }
        voiceSource.playOnAwake = false;

        // UI initial state
        if (dialogueCanvas) dialogueCanvas.SetActive(false);

        if (dialoguePanel)
        {
            panelTargetPos = dialoguePanel.anchoredPosition;
            dialoguePanel.gameObject.SetActive(false); // ✅ panel hidden at start
        }
        else
        {
            Debug.LogWarning($"[{name}] dialoguePanel is NULL. Slide animation won't work. Please assign the Panel RectTransform.");
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

        if (Input.GetKeyDown(nextKey))
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

        if (dialogueCanvas) dialogueCanvas.SetActive(true);
        ShowPanelAnimated(true);

        if (npcNameText) npcNameText.text = npcName;

        PickDialogueAndVoiceForThisTalk();

        if (activeDialogue == null || activeDialogue.Length == 0)
        {
            EndDialogue();
            return;
        }

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
        typeRoutine = StartCoroutine(TypeText(activeDialogue[dialogueIndex]));

        PlayVoiceForLine(dialogueIndex);
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
    }

    void FinishTypingInstant()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);

        if (dialogueText && activeDialogue != null && dialogueIndex < activeDialogue.Length)
            dialogueText.text = activeDialogue[dialogueIndex];

        typing = false;
    }

    void PlayVoiceForLine(int index)
    {
        if (!voiceSource) return;
        if (activeVoice == null) return;
        if (index < 0 || index >= activeVoice.Length) return;

        var clip = activeVoice[index];
        if (!clip) return;

        if (stopPreviousOnNext) voiceSource.Stop();
        voiceSource.PlayOneShot(clip, voiceVolume);
    }

    void NextLine()
    {
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
        if (voiceSource && stopPreviousOnNext)
            voiceSource.Stop();

        dialoguePlaying = false;
        ApplyTalkingPause(false);

        ShowPanelAnimated(false);

        // task logic
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

            // If you want canvas off too after slide down:
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
