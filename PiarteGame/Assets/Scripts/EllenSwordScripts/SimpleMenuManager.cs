using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple Main Menu Manager
/// Attach to an empty GameObject in your Main Menu scene
/// </summary>
public class SimpleMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Scene to Load")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string loadingSceneName = "LoadingScene";

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

    private void FindAllBackButtons()
    {
        // Find all buttons in the scene with "back" in their name (case insensitive)
        Button[] allButtons = FindObjectsOfType<Button>(true);
        System.Collections.Generic.List<Button> foundBackButtons = new System.Collections.Generic.List<Button>();

        foreach (Button button in allButtons)
        {
            if (button.name.ToLower().Contains("back"))
            {
                foundBackButtons.Add(button);
                Debug.Log($"✅ Found back button: {button.name}");
            }
        }

        backButtons = foundBackButtons.ToArray();
        Debug.Log($"🔍 Auto-found {backButtons.Length} back button(s)");
    }

    private void SetupBackButtons()
    {
        if (backButtons == null || backButtons.Length == 0)
        {
            Debug.LogWarning("⚠️ No back buttons assigned!");
            return;
        }

        // Add ShowMainMenu listener to each back button
        foreach (Button backButton in backButtons)
        {
            if (backButton != null)
            {
                // Remove any existing listeners to avoid duplicates
                backButton.onClick.RemoveListener(ShowMainMenu);
                // Add the listener
                backButton.onClick.AddListener(ShowMainMenu);
                Debug.Log($"✅ Back button '{backButton.name}' configured");
            }
        }

        Debug.Log($"🎮 {backButtons.Length} back button(s) configured");
    }

    // ===== MAIN MENU BUTTONS =====
    public void PlayGame()
    {
        Debug.Log("▶️ Loading game scene...");
        LoadingScreen.sceneToLoad = gameSceneName;
        SceneManager.LoadScene(loadingSceneName);
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

    // ===== BACK BUTTON =====
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        Debug.Log("🏠 Returned to main menu");
    }
}