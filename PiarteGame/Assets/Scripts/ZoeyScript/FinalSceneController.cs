using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class FinalSceneController : MonoBehaviour
{
    [Header("Easter Eggs")]
    public List<ItemData> easterEggItems = new List<ItemData>();
    public int totalEggCountOverride = 0;

    [Header("UI")]
    public TMP_Text messageText;
    public Button actionButton;

    [Tooltip("Drag your Canvas (or any UI root) here so it can be hidden during video.")]
    public GameObject uiCanvasRoot;

    [Header("Message Text (Editable in Inspector)")]
    [TextArea(2, 5)]
    public string allCollectedMessage =
        "Congratulations!\nYou found all of our Easter Eggs.";

    [TextArea(1, 3)]
    public string notAllCollectedSuffix =
        "Keep going!";

    [Header("Video (optional)")]
    public VideoPlayer videoPlayer;

    [Header("Main Menu Scene")]
    public string mainMenuSceneName = "MainMenu";

    private int collectedCount;
    private int totalCount;
    private bool allCollected;

    private void OnEnable()
    {
        EnableCursorForUI();
    }

    private void Start()
    {
        EnableCursorForUI();

        totalCount = (totalEggCountOverride > 0) ? totalEggCountOverride : easterEggItems.Count;
        if (totalCount < 0) totalCount = 0;

        RecalculateProgress();
        UpdateMessage();

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }

        if (videoPlayer)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;

            // 建议：初始隐藏（你按钮触发再显示）
            videoPlayer.gameObject.SetActive(false);
        }
    }

    private void EnableCursorForUI()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RecalculateProgress()
    {
        collectedCount = 0;
        foreach (var egg in easterEggItems)
        {
            if (egg != null)
            {
                if (StaticInventory.Has(egg, 1))
                    collectedCount++;
            }
        }

        allCollected = (totalCount > 0) && (collectedCount >= totalCount);
    }

    private void UpdateMessage()
    {
        if (!messageText) return;

        if (allCollected)
            messageText.text = allCollectedMessage;
        else
            messageText.text = $"{collectedCount} / {totalCount} Easter Eggs collected.\n{notAllCollectedSuffix}";
    }

    private void OnActionClicked()
    {
        if (allCollected)
        {
            if (videoPlayer) PlayEndingVideo();
            else GoToMainMenu();
        }
        else
        {
            GoToMainMenu();
        }
    }

    private void PlayEndingVideo()
    {
        // ✅ Hide UI so it won't cover the video
        if (uiCanvasRoot) uiCanvasRoot.SetActive(false);

        videoPlayer.gameObject.SetActive(true);
        videoPlayer.Stop();
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        GoToMainMenu();
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
