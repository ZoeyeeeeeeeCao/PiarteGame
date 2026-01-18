using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Simple Main Menu Manager with Multiple Video Background Support
/// Handles separate video players for Main Menu, Settings, and Credits
/// </summary>
public class SimpleMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Video Players (One per Panel)")]
    [Tooltip("VideoPlayer on Main Menu canvas")]
    [SerializeField] private VideoPlayer mainMenuVideo;
    [Tooltip("VideoPlayer on Settings canvas")]
    [SerializeField] private VideoPlayer settingsVideo;
    [Tooltip("VideoPlayer on Credits canvas")]
    [SerializeField] private VideoPlayer creditsVideo;

    [Header("Auto-Find Video Players")]
    [Tooltip("Automatically find VideoPlayer components in each panel")]
    [SerializeField] private bool autoFindVideos = true;

    [Header("Scene to Load")]
    [Tooltip("The exact name of the scene you want to play (e.g., 'GameScene')")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Back Buttons")]
    [SerializeField] private Button[] backButtons;
    [Tooltip("Automatically find all buttons named 'BackButton' if array is empty")]
    [SerializeField] private bool autoFindBackButtons = true;

    private void Start()
    {
        // Auto-find video players if enabled
        if (autoFindVideos)
        {
            FindAllVideoPlayers();
        }

        // Auto-find back buttons if enabled
        if (autoFindBackButtons && (backButtons == null || backButtons.Length == 0))
        {
            FindAllBackButtons();
        }

        // Assign ShowMainMenu to all back buttons
        SetupBackButtons();

        // Show only main menu at start
        ShowMainMenu();
    }

    // ===== MAIN MENU BUTTONS =====
    public void PlayGame()
    {
        Debug.Log($"▶️ Requesting Load for: {gameSceneName}");

        // Stop all videos before loading
        StopAllVideos();

        // Check if Howard's loader exists
        if (SceneLoaderHoward.Instance != null)
        {
            SceneLoaderHoward.Instance.LoadLevel(gameSceneName);
        }
        else
        {
            Debug.LogError("❌ SceneLoaderHoward not found! Make sure the 'SceneLoaderHoward' script is attached to a GameObject in this scene.");
        }
    }

    public void OpenSettings()
    {
        // Switch panels
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        creditsPanel.SetActive(false);

        // DON'T call PlayVideo - let the ping-pong script handle it via OnEnable
        Debug.Log("⚙️ Settings opened");
    }

    public void OpenCredits()
    {
        // Switch panels
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);

        // DON'T call PlayVideo - let the ping-pong script handle it via OnEnable
        Debug.Log("📜 Credits opened");
    }

    public void QuitGame()
    {
        Debug.Log("🚪 Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===== BACK BUTTON LOGIC =====
    public void ShowMainMenu()
    {
        // Switch panels
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        // DON'T call PlayVideo - let the ping-pong script handle it via OnEnable
        Debug.Log("🏠 Returned to main menu");
    }

    // ===== VIDEO MANAGEMENT =====
    private void PlayVideo(VideoPlayer video)
    {
        if (video == null)
        {
            Debug.LogWarning("⚠️ VideoPlayer is null!");
            return;
        }

        Debug.Log($"🎬 Starting video: {video.gameObject.name}");

        // Force stop first
        video.Stop();

        // Check for ping-pong script and restart it
        VideoBackgroundPingPong pingPong = video.GetComponent<VideoBackgroundPingPong>();
        if (pingPong != null)
        {
            Debug.Log("🔄 Restarting ping-pong script");
            pingPong.RestartFromBeginning();
        }
        else
        {
            // No ping-pong script, just play normally
            video.time = 0;
            video.Play();
            Debug.Log("▶️ Playing video normally");
        }
    }

    private void StopAllVideos()
    {
        if (mainMenuVideo != null) mainMenuVideo.Stop();
        if (settingsVideo != null) settingsVideo.Stop();
        if (creditsVideo != null) creditsVideo.Stop();
    }

    private void FindAllVideoPlayers()
    {
        // Find video in main menu panel
        if (mainMenuPanel != null && mainMenuVideo == null)
        {
            mainMenuVideo = mainMenuPanel.GetComponentInChildren<VideoPlayer>(true);
            if (mainMenuVideo != null)
                Debug.Log("🎥 Found Main Menu video player");
        }

        // Find video in settings panel
        if (settingsPanel != null && settingsVideo == null)
        {
            settingsVideo = settingsPanel.GetComponentInChildren<VideoPlayer>(true);
            if (settingsVideo != null)
                Debug.Log("🎥 Found Settings video player");
        }

        // Find video in credits panel
        if (creditsPanel != null && creditsVideo == null)
        {
            creditsVideo = creditsPanel.GetComponentInChildren<VideoPlayer>(true);
            if (creditsVideo != null)
                Debug.Log("🎥 Found Credits video player");
        }
    }

    private void FindAllBackButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        System.Collections.Generic.List<Button> foundBackButtons = new System.Collections.Generic.List<Button>();

        foreach (Button button in allButtons)
        {
            if (button.name.ToLower().Contains("back"))
            {
                foundBackButtons.Add(button);
            }
        }

        backButtons = foundBackButtons.ToArray();
        Debug.Log($"🔍 Auto-found {backButtons.Length} back buttons");
    }

    private void SetupBackButtons()
    {
        if (backButtons == null || backButtons.Length == 0) return;

        foreach (Button backButton in backButtons)
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowMainMenu);
                backButton.onClick.AddListener(ShowMainMenu);
            }
        }
    }
}