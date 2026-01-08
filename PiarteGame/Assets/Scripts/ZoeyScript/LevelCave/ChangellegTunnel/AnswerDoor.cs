using UnityEngine;

public class AnswerDoor : MonoBehaviour
{
    [Header("Trigger & Interact")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E; // 靠近按E：打开选项UI（不做最终选择）

    [Header("Hint UI (World Space)")]
    public GameObject hintCanvas; // 拖门前面的World Space提示Canvas（只负责显示/隐藏）

    [Header("Answer UI Text (Shown on Screen Space AnswerCanvas)")]
    [TextArea(4, 10)]
    public string uiText =
        "Do you want to choose this door?\n\n" +
        "7 - Proceed\n" +
        "8 - Exit";

    [Header("Door Result")]
    public bool isCorrectDoor = true;

    [Header("Correct Door - Animator")]
    public Animator doorAnimator;                 // 正确门拖 Animator
    public string openTriggerName = "AnswerDoorOpen"; // Animator里Trigger参数名

    [Header("Wrong Door - Destroy Target")]
    public GameObject objectToDestroy;            // 错门按7时销毁的物体

    [Header("Lock After Proceed")]
    public bool lockAfterProceed = true;          // 按7后是否锁定这扇门（防止重复触发）
    private bool hasProceeded = false;

    private bool playerInside = false;

    private void Start()
    {
        SetHint(false);
    }

    private void Update()
    {
        if (!playerInside) return;
        if (AnswerUIManager.Instance == null) return;

        // 如果已经最终选择并锁定：不再提示、不再交互
        if (hasProceeded && lockAfterProceed)
        {
            SetHint(false);
            return;
        }

        // 如果屏幕答案UI正在显示：隐藏世界提示，避免叠UI
        if (AnswerUIManager.Instance.IsShowing)
        {
            SetHint(false);
            return;
        }

        // 可交互：显示提示
        SetHint(true);

        // 按E：只是打开选项UI（不是最终选择）
        if (Input.GetKeyDown(interactKey))
        {
            AnswerUIManager.Instance.Show(this);
            SetHint(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;

        if (!(hasProceeded && lockAfterProceed))
            SetHint(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        SetHint(false);
    }

    private void SetHint(bool show)
    {
        if (hintCanvas) hintCanvas.SetActive(show);
    }

    // UI里按7时调用（最终确认选择这个门）
    public void ConfirmProceed()
    {
        if (hasProceeded && lockAfterProceed) return;

        hasProceeded = true;
        SetHint(false);

        if (isCorrectDoor)
        {
            if (doorAnimator != null && !string.IsNullOrEmpty(openTriggerName))
            {
                doorAnimator.ResetTrigger(openTriggerName);
                doorAnimator.SetTrigger(openTriggerName);
            }
            else
            {
                Debug.LogWarning($"{name}: Correct door missing Animator or trigger name.");
            }
        }
        else
        {
            if (objectToDestroy != null)
            {
                Destroy(objectToDestroy);
            }
            else
            {
                Debug.LogWarning($"{name}: Wrong door missing objectToDestroy.");
            }
        }
    }
}
