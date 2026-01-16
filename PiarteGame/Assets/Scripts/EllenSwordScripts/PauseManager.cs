using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject settingsPanel; // Panel with sliders (audio, sfx, mouse sensitivity)

    [Header("Buttons")]
    [SerializeField] private GameObject mainButtons; // Container for Resume, Controls, Main Menu buttons

    [Header("Controls Pages (Paginated)")]
    [SerializeField] private GameObject controlsContainer; // Parent container for all control pages
    [SerializeField] private GameObject[] controlPages; // Array of control page GameObjects
    [SerializeField] private GameObject nextPageButton;
    [SerializeField] private GameObject prevPageButton;
    [SerializeField] private GameObject backButton; // Back button that appears in controls view

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private int currentControlPageIndex = 0;
    private bool isInControlsView = false;

    void Start()
    {
        // Make sure menus are hidden at start
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (controlsContainer != null)
            controlsContainer.SetActive(false);

        // Hide all control pages initially
        HideAllControlPages();
    }

    void Update()
    {
        // Check for ESC key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackNavigation();
        }
    }

    private void HandleBackNavigation()
    {
        if (isPaused)
        {
            if (isInControlsView)
            {
                // If in controls view, go back to settings
                CloseControlsView();
            }
            else
            {
                // If in settings view, resume game
                ResumeGame();
            }
        }
        else
        {
            // Game is running, open pause menu
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        isInControlsView = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);

        // Show settings panel
        ShowSettingsPanel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        isInControlsView = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowSettingsPanel()
    {
        isInControlsView = false;

        // Show settings and buttons
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (mainButtons != null)
            mainButtons.SetActive(true);

        // Hide controls
        if (controlsContainer != null)
            controlsContainer.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);
    }

    public void OpenControlsView()
    {
        isInControlsView = true;

        // Hide settings panel
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Hide main buttons
        if (mainButtons != null)
            mainButtons.SetActive(false);

        // Show controls container
        if (controlsContainer != null)
            controlsContainer.SetActive(true);

        // Show back button
        if (backButton != null)
            backButton.SetActive(true);

        // Reset to first page
        currentControlPageIndex = 0;
        UpdateControlPageVisibility();
    }

    public void CloseControlsView()
    {
        ShowSettingsPanel();
    }

    public void NextControlPage()
    {
        if (controlPages == null || controlPages.Length == 0) return;

        // Loop using modulo (same as SettingsManager)
        currentControlPageIndex = (currentControlPageIndex + 1) % controlPages.Length;
        UpdateControlPageVisibility();
    }

    public void PreviousControlPage()
    {
        if (controlPages == null || controlPages.Length == 0) return;

        currentControlPageIndex--;

        // Loop back to end if below zero
        if (currentControlPageIndex < 0)
            currentControlPageIndex = controlPages.Length - 1;

        UpdateControlPageVisibility();
    }

    private void UpdateControlPageVisibility()
    {
        // Hide all pages first
        for (int i = 0; i < controlPages.Length; i++)
        {
            if (controlPages[i] != null)
                controlPages[i].SetActive(i == currentControlPageIndex);
        }

        // Show/hide navigation buttons (hide if only 1 page)
        bool hasMultiplePages = controlPages != null && controlPages.Length > 1;

        if (nextPageButton != null)
            nextPageButton.SetActive(hasMultiplePages);

        if (prevPageButton != null)
            prevPageButton.SetActive(hasMultiplePages);
    }

    private void HideAllControlPages()
    {
        if (controlPages != null)
        {
            foreach (GameObject page in controlPages)
            {
                if (page != null)
                    page.SetActive(false);
            }
        }
    }

    public void OnBackButtonPressed()
    {
        HandleBackNavigation();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
