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

    [Header("Dialogue Content (TMP)")]
    [TextArea(2, 6)]
    public string[] firstDialogue;      // 第一次：开启点火盆

    [TextArea(2, 6)]
    public string[] secondDialogue;     // 第二次且已完成：开门

    [TextArea(2, 6)]
    public string[] notReadyDialogue;   // 第二次但没完成：提示回去点火

    [Header("Optional: Pause game while talking (PC)")]
    public bool pauseTimeWhileTalking = false;
    public bool showCursorWhileTalking = false;

    private int dialogueIndex;
    private bool playerInRange;
    private bool dialoguePlaying;

    private string[] activeDialogue;

    private enum TalkState { First, Second, Done }
    private TalkState talkState = TalkState.First;

    void Start()
    {
        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        if (pressEIndicator) pressEIndicator.SetActive(false);

        if (npcNameText) npcNameText.text = npcName;
    }

    void Update()
    {
        if (!playerInRange) return;

        // 未在对话中：按 E 开始
        if (!dialoguePlaying)
        {
            if (Input.GetKeyDown(interactKey))
                StartDialogue();
            return;
        }

        // 对话中：按 Space 下一句
        if (Input.GetKeyDown(nextKey))
            NextLine();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;

        // Done 状态就不提示了（可按你需要改）
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
        // Done 后不再对话（可改成允许重复播放）
        if (talkState == TalkState.Done) return;

        dialoguePlaying = true;
        dialogueIndex = 0;

        if (pressEIndicator) pressEIndicator.SetActive(false);
        if (dialogueCanvas) dialogueCanvas.SetActive(true);

        if (npcNameText) npcNameText.text = npcName;

        // 选择本次要播放的台词组
        activeDialogue = PickDialogueForThisTalk();

        // 防呆：空数组就直接结束
        if (activeDialogue == null || activeDialogue.Length == 0)
        {
            EndDialogue();
            return;
        }

        ApplyTalkingPause(true);
        ShowCurrentLine();
    }

    string[] PickDialogueForThisTalk()
    {
        if (talkState == TalkState.First)
            return firstDialogue;

        if (talkState == TalkState.Second)
        {
            // 第二次：如果火盆没点完，播放 notReady
            if (QuestFinalSceneManager.Instance != null &&
                !QuestFinalSceneManager.Instance.AllBraziersLit())
            {
                if (notReadyDialogue != null && notReadyDialogue.Length > 0)
                    return notReadyDialogue;
            }

            return secondDialogue;
        }

        return null;
    }

    void ShowCurrentLine()
    {
        if (!dialogueText) return;
        dialogueText.text = activeDialogue[dialogueIndex];
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
        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        dialoguePlaying = false;

        ApplyTalkingPause(false);

        // 对话结束触发任务逻辑
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

        // 玩家仍在范围内，且没 Done，就重新显示 E 提示
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
