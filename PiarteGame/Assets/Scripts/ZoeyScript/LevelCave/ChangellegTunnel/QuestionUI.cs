using UnityEngine;
using TMPro;

public class QuestionUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject uiCanvas;
    public TMP_Text messageText;

    [Header("Message Content")]
    [TextArea]
    public string message = "Do you want to proceed?";

    [Header("Input")]
    public KeyCode proceedKey = KeyCode.Return;

    private bool playerInside = false;
    private bool hasProceeded = false;

    void Start()
    {
        if (uiCanvas)
            uiCanvas.SetActive(false);
    }

    void Update()
    {
        if (playerInside && !hasProceeded && Input.GetKeyDown(proceedKey))
        {
            Proceed();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        hasProceeded = false;

        if (uiCanvas)
            uiCanvas.SetActive(true);

        if (messageText)
            messageText.text = message;

        // ⏸ 暂停游戏
        Time.timeScale = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        hasProceeded = false;

        if (uiCanvas)
            uiCanvas.SetActive(false);

        // ▶ 恢复游戏
        Time.timeScale = 1f;
    }

    void Proceed()
    {
        hasProceeded = true;

        Debug.Log("Proceed pressed");

        if (uiCanvas)
            uiCanvas.SetActive(false);

        // ▶ 恢复游戏
        Time.timeScale = 1f;

        // 你之后可以在这里加：
        // Load Scene
        // Open Door
        // Trigger Timeline
    }
}
