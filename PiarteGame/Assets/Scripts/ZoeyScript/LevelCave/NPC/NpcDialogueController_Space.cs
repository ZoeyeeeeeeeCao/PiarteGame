using UnityEngine;
using TMPro;

public class NpcDialogueController_Space_TMP : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public KeyCode nextKey = KeyCode.Space;

    [Tooltip("Optional: World-space UI / sprite that shows 'Press E'")]
    public GameObject pressEIndicator;

    [Header("Dialogue UI (TMP)")]
    [Tooltip("Canvas root GameObject (set inactive by default)")]
    public GameObject dialogueCanvas;

    [Tooltip("Optional: NPC name text")]
    public TMP_Text npcNameText;

    [Tooltip("Required: dialogue content text")]
    public TMP_Text dialogueText;

    [Header("NPC Name (optional)")]
    public string npcName = "NPC";

    [Header("Dialogue Content")]
    [TextArea(2, 6)]
    public string[] firstDialogue;      // 第一次：开启点火盆

    [TextArea(2, 6)]
    public string[] secondDialogue;     // 第二次且已完成：开门

    [TextArea(2, 6)]
    public string[] notReadyDialogue;   // 第二次但没完成：提示回去点火

    [Header("Voice Clips (one clip per line, optional)")]
    public AudioClip[] firstVoice;
    public AudioClip[] secondVoice;
    public AudioClip[] notReadyVoice;

    [Header("Audio Settings")]
    [Tooltip("Optional. If empty, will auto add/find an AudioSource on this NPC.")]
    public AudioSource voiceSource;
    [Range(0f, 1f)] public float voiceVolume = 1f;

    [Tooltip("If true, stop previous voice when switching to next line.")]
    public bool stopPreviousOnNext = true;

    [Header("Optional: Pause game while talking (PC)")]
    public bool pauseTimeWhileTalking = false;
    public bool showCursorWhileTalking = false;

    int dialogueIndex;
    bool playerInRange;
    bool dialoguePlaying;

    string[] activeDialogue;
    AudioClip[] activeVoice;

    enum TalkState { First, Second, Done }
    TalkState talkState = TalkState.First;

    void Start()
    {
        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        if (pressEIndicator) pressEIndicator.SetActive(false);

        if (npcNameText) npcNameText.text = npcName;

        // Prepare audio source
        if (!voiceSource)
        {
            voiceSource = GetComponent<AudioSource>();
            if (!voiceSource) voiceSource = gameObject.AddComponent<AudioSource>();
        }
        voiceSource.playOnAwake = false;

        // 通常对白建议 2D（不跟距离衰减）；你想 3D 就把下面改成 1
        voiceSource.spatialBlend = 0f;
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
            NextLine();
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

        if (npcNameText) npcNameText.text = npcName;

        PickDialogueAndVoiceForThisTalk();

        if (activeDialogue == null || activeDialogue.Length == 0)
        {
            EndDialogue();
            return;
        }

        ApplyTalkingPause(true);
        ShowCurrentLine(playVoice: true);
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

    void ShowCurrentLine(bool playVoice)
    {
        if (dialogueText)
            dialogueText.text = activeDialogue[dialogueIndex];

        if (!playVoice) return;

        // 播放对应行的音频（如果有）
        if (voiceSource && activeVoice != null &&
            dialogueIndex >= 0 && dialogueIndex < activeVoice.Length)
        {
            var clip = activeVoice[dialogueIndex];
            if (clip)
            {
                if (stopPreviousOnNext) voiceSource.Stop();
                voiceSource.PlayOneShot(clip, voiceVolume);
            }
        }
    }

    void NextLine()
    {
        dialogueIndex++;

        if (activeDialogue == null || dialogueIndex >= activeDialogue.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine(playVoice: true);
    }

    void EndDialogue()
    {
        if (voiceSource && stopPreviousOnNext)
            voiceSource.Stop();

        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        dialoguePlaying = false;

        ApplyTalkingPause(false);

        // 结束触发任务逻辑
        if (talkState == TalkState.First)
        {
            if (QuestFinalSceneManager.Instance != null)
                QuestFinalSceneManager.Instance.OnNpcTalk_StartQuest();

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
            // 没完成就保持 Second，让玩家回去点火盆
        }

        if (playerInRange && talkState != TalkState.Done && pressEIndicator)
            pressEIndicator.SetActive(true);
    }

    void ApplyTalkingPause(bool talking)
    {
        if (!pauseTimeWhileTalking) return;

        if (talking)
        {
            Time.timeScale = 0f;

            if (showCursorWhileTalking)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            Time.timeScale = 1f;

            if (showCursorWhileTalking)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
