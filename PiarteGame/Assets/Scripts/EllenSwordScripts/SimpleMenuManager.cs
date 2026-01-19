using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Simple Main Menu Manager with Multiple Video Background Support
/// Now connects to SettingsManager for audio/control settings
/// </summary>
public class SimpleMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Manager References")]
    [Tooltip("Reference to SettingsManager (handles all settings)")]
    [SerializeField] private SettingsManager settingsManager;
    [Tooltip("Reference to AudioManager (handles audio)")]
    [SerializeField] private AudioManager audioManager;
    [Tooltip("Reference to MenuBGMManager (handles menu music)")]
    [SerializeField] private MenuBGMManager menuBGMManager;

    [Header("Video Players (One per Panel)")]
    [Tooltip("VideoPlayer on Main Menu canvas")]
    [SerializeField] private VideoPlayer mainMenuVideo;
    [Tooltip("VideoPlayer on Settings canvas")]
    [SerializeField] private VideoPlayer settingsVideo;
    [Tooltip("VideoPlayer on Credits canvas")]
    [SerializeField] private VideoPlayer creditsVideo;

    [Header("Auto-Find Components")]
    [Tooltip("Automatically find VideoPlayer components in each panel")]
    [SerializeField] private bool autoFindVideos = true;
    [Tooltip("Automatically find SettingsManager and AudioManager")]
    [SerializeField] private bool autoFindManagers = true;

    [Header("Scene to Load")]
    [Tooltip("The exact name of the scene you want to play (e.g., 'GameScene')")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Back Buttons")]
    [SerializeField] private Button[] backButtons;
    [Tooltip("Automatically find all buttons named 'BackButton' if array is empty")]
    [SerializeField] private bool autoFindBackButtons = true;

    private bool hasStartedMusic = false; // Track if music has been started

    private void Start()
    {
        // Auto-find managers if enabled
        if (autoFindManagers)
        {
            FindAllManagers();
        }

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

        // Initialize managers
        InitializeManagers();

        // Show only main menu at start
        ShowMainMenu();
    }

    // ===== INITIALIZATION =====

    private void InitializeManagers()
    {
        // Initialize AudioManager if available - ONLY ONCE
        if (audioManager != null)
        {
            audioManager.InitializeAudio();
            Debug.Log("✅ AudioManager initialized from SimpleMenuManager");
        }
        else
        {
            Debug.LogWarning("⚠️ AudioManager not found! Audio controls may not work.");
        }

        // SettingsManager initializes itself in its own Start()
        if (settingsManager != null)
        {
            Debug.Log("✅ SettingsManager connected");
        }
        else
        {
            Debug.LogWarning("⚠️ SettingsManager not found! Settings panel may not work properly.");
        }

        // Start the music immediately on initialization
        if (menuBGMManager != null && !hasStartedMusic)
        {
            menuBGMManager.PlayMainMenuMusic();
            hasStartedMusic = true;
            Debug.Log("🎵 Started music during initialization");
        }
    }

    private void FindAllManagers()
    {
        // Find SettingsManager
        if (settingsManager == null)
        {
            settingsManager = FindObjectOfType<SettingsManager>();
            if (settingsManager != null)
                Debug.Log("🔍 Found SettingsManager");
        }

        // Find AudioManager
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null)
                Debug.Log("🔍 Found AudioManager");
        }

        // Find MenuBGMManager
        if (menuBGMManager == null)
        {
            menuBGMManager = FindObjectOfType<MenuBGMManager>();
            if (menuBGMManager != null)
                Debug.Log("🔍 Found MenuBGMManager");
        }
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

        // DON'T do anything with music - let it continue playing naturally
        Debug.Log("⚙️ Settings panel opened (music continues)");
    }

    public void OpenCredits()
    {
        // Switch panels
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);

        // DON'T do anything with music - let it continue playing naturally
        Debug.Log("📜 Credits opened (music continues)");
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

        // Music is already started in InitializeManagers, don't touch it here
        Debug.Log("🏠 Returned to main menu (music continues)");
    }

    // You can also call SettingsManager's back navigation if needed
    public void HandleBackFromSettings()
    {
        if (settingsManager != null && settingsManager.isInsideKeyboardSubmenu)
        {
            // If inside keyboard submenu, close it and return to main settings
            settingsManager.CloseKeyboardSettings();
        }
        else
        {
            // Otherwise, go back to main menu
            ShowMainMenu();
        }
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
                // Check if this back button is in the settings panel
                bool isSettingsBackButton = settingsPanel != null &&
                                           backButton.transform.IsChildOf(settingsPanel.transform);

                // Check if this back button is in the credits panel
                bool isCreditsBackButton = creditsPanel != null &&
                                           backButton.transform.IsChildOf(creditsPanel.transform);

                if (isSettingsBackButton && settingsManager != null)
                {
                    // Use SettingsManager's smart back navigation
                    backButton.onClick.RemoveListener(HandleBackFromSettings);
                    backButton.onClick.AddListener(HandleBackFromSettings);
                    Debug.Log($"✅ Setup back button for Settings: {backButton.name}");
                }
                else
                {
                    // Regular back button - go to main menu PANEL (don't reload scene!)
                    backButton.onClick.RemoveListener(ShowMainMenu);
                    backButton.onClick.AddListener(ShowMainMenu);
                    Debug.Log($"✅ Setup back button for panel switching: {backButton.name}");
                }
            }
        }
    }

    // ===== PUBLIC HELPER METHODS =====

    /// <summary>
    /// Get reference to AudioManager
    /// </summary>
    public AudioManager GetAudioManager()
    {
        return audioManager;
    }

    /// <summary>
    /// Get reference to SettingsManager
    /// </summary>
    public SettingsManager GetSettingsManager()
    {
        return settingsManager;
    }
}