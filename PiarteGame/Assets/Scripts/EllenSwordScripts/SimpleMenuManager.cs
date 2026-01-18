using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Simple Main Menu Manager with Video Background Support
/// Connects the Play button to Howard's Scene Loader
/// </summary>
public class SimpleMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Video Background (Optional)")]
    [Tooltip("Drag your VideoPlayer or RawImage with VideoPlayer here")]
    [SerializeField] private VideoPlayer backgroundVideo;
    [Tooltip("Pause video when in Settings/Credits?")]
    [SerializeField] private bool pauseVideoInSubmenus = false; // Changed default to false

    [Header("Scene to Load")]
    [Tooltip("The exact name of the scene you want to play (e.g., 'GameScene')")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Back Buttons")]
    [SerializeField] private Button[] backButtons;
    [Tooltip("Automatically find all buttons named 'BackButton' if array is empty")]
    [SerializeField] private bool autoFindBackButtons = true;

    private void Start()
    {
        // Auto-find back buttons if enabled and array is empty
        if (autoFindBackButtons && (backButtons == null || backButtons.Length == 0))
        {
            FindAllBackButtons();
        }

        // Assign ShowMainMenu to all back buttons
        SetupBackButtons();

        // Show only main menu at start, hide everything else
        ShowMainMenu();

        // Start video if assigned
        if (backgroundVideo != null && !backgroundVideo.isPlaying)
        {
            backgroundVideo.Play();
        }
    }

    // ===== MAIN MENU BUTTONS =====
    public void PlayGame()
    {
        Debug.Log($"▶️ Requesting Load for: {gameSceneName}");

        // Stop video before loading (optional)
        if (backgroundVideo != null)
        {
            backgroundVideo.Stop();
        }

        // Check if Howard's loader exists
        if (SceneLoaderHoward.Instance != null)
        {
            // Trigger Howard's loading logic
            SceneLoaderHoward.Instance.LoadLevel(gameSceneName);
        }
        else
        {
            Debug.LogError("❌ SceneLoaderHoward not found! Make sure the 'SceneLoaderHoward' script is attached to a GameObject in this scene.");
        }
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        creditsPanel.SetActive(false);

        // Only pause video if enabled
        if (pauseVideoInSubmenus && backgroundVideo != null && backgroundVideo.isPlaying)
        {
            backgroundVideo.Pause();
        }

        Debug.Log("⚙️ Settings opened");
    }

    public void OpenCredits()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);

        // Only pause video if enabled
        if (pauseVideoInSubmenus && backgroundVideo != null && backgroundVideo.isPlaying)
        {
            backgroundVideo.Pause();
        }

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
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        // Resume video when returning to main menu (only if it was paused)
        if (pauseVideoInSubmenus && backgroundVideo != null && !backgroundVideo.isPlaying)
        {
            backgroundVideo.Play();
        }

        Debug.Log("🏠 Returned to main menu");
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