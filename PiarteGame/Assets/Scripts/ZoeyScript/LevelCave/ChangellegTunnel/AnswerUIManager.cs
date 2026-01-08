using UnityEngine;
using TMPro;

public class AnswerUIManager : MonoBehaviour
{
    public static AnswerUIManager Instance;

    [Header("Answer UI (Screen Space)")]
    public GameObject uiRoot;   // 拖你的 AnswerCanvas（或你要显示/隐藏的UI根物体）
    public TMP_Text mainText;   // 拖唯一的 TMP_Text

    [Header("Keys")]
    public KeyCode proceedKey = KeyCode.Alpha7; // 7
    public KeyCode exitKey = KeyCode.Alpha8;    // 8

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
        Hide();
    }

    private void Update()
    {
        if (!showing) return;

        if (Input.GetKeyDown(proceedKey))
        {
            currentDoor?.ConfirmProceed();
            Hide();
        }
        else if (Input.GetKeyDown(exitKey))
        {
            Hide();
        }
    }

    public void Show(AnswerDoor door)
    {
        currentDoor = door;
        showing = true;

        if (uiRoot) uiRoot.SetActive(true);
        if (mainText) mainText.text = door.uiText;
    }

    public void Hide()
    {
        showing = false;
        currentDoor = null;

        if (uiRoot) uiRoot.SetActive(false);
    }

    public bool IsShowing => showing;
}
