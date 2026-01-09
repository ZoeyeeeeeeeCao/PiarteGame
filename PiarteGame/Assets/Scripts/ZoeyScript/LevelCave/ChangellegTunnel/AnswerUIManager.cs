using UnityEngine;
using TMPro;

public class AnswerUIManager : MonoBehaviour
{
    public static AnswerUIManager Instance;

    [Header("Answer UI (Screen Space)")]
    public GameObject uiRoot;   // AnswerCanvas / UI Root
    public TMP_Text mainText;   // 显示内容

    [Header("Buttons")]
    public UnityEngine.UI.Button proceedButton;
    public UnityEngine.UI.Button exitButton;

    private AnswerDoor currentDoor;
    private bool showing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 绑定按钮事件
        if (proceedButton)
            proceedButton.onClick.AddListener(OnProceedClicked);

        if (exitButton)
            exitButton.onClick.AddListener(OnExitClicked);

        Hide();
    }

    public void Show(AnswerDoor door)
    {
        currentDoor = door;
        showing = true;

        if (uiRoot) uiRoot.SetActive(true);
        if (mainText) mainText.text = door.uiText;

        // ✅ 打开 UI 时：启用鼠标
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        showing = false;
        currentDoor = null;

        if (uiRoot) uiRoot.SetActive(false);

        // ✅ 关闭 UI 时：恢复游戏
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnProceedClicked()
    {
        currentDoor?.ConfirmProceed();
        Hide();
    }

    private void OnExitClicked()
    {
        Hide();
    }

    public bool IsShowing => showing;
}
