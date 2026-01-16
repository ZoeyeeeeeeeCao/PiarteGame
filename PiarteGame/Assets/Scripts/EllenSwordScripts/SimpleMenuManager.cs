using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple Main Menu Manager
/// Connects the Play button to Howard's Scene Loader
/// </summary>
public class SimpleMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Scene to Load")]
    [Tooltip("The exact name of the scene you want to play (e.g., 'GameScene')")]
    [SerializeField] private string gameSceneName = "GameScene";

    // REMOVED: loadingSceneName is no longer needed because Howard loads an overlay, not a scene.

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
    }

    // ===== MAIN MENU BUTTONS =====

    public void PlayGame()
    {
        Debug.Log($"▶️ Requesting Load for: {gameSceneName}");

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
        Debug.Log("⚙️ Settings opened");
    }

    public void OpenCredits()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);
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

    // ===== BACK BUTTON LOGIC (Unchanged) =====
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
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