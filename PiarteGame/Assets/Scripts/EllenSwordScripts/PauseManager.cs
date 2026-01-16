using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject settingsPanel; // Panel containing sliders
    [SerializeField] private GameObject controlsPanel; // Panel containing controls pages

    [Header("Controls Pages")]
    [SerializeField] private GameObject[] controlPages; // Array of control page GameObjects
    [SerializeField] private GameObject previousButton;
    [SerializeField] private GameObject nextButton;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private int currentControlPage = 0;
    private bool isInControlsView = false;

    void Start()
    {
        // Make sure menus are hidden at start
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

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
                ShowSettings();
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

        ShowSettings();

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

        HideAllControlPages();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowControls()
    {
        isInControlsView = true;
        currentControlPage = 0;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(true);

        UpdateControlPage();
    }

    public void ShowSettings()
    {
        isInControlsView = false;

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        HideAllControlPages();
    }

    public void NextControlPage()
    {
        if (controlPages == null || controlPages.Length == 0)
            return;

        currentControlPage = (currentControlPage + 1) % controlPages.Length;
        UpdateControlPage();
    }

    public void PreviousControlPage()
    {
        if (controlPages == null || controlPages.Length == 0)
            return;

        currentControlPage--;
        if (currentControlPage < 0)
            currentControlPage = controlPages.Length - 1;

        UpdateControlPage();
    }

    private void UpdateControlPage()
    {
        // Hide all pages first
        HideAllControlPages();

        // Show current page
        if (controlPages != null && currentControlPage >= 0 && currentControlPage < controlPages.Length)
        {
            if (controlPages[currentControlPage] != null)
                controlPages[currentControlPage].SetActive(true);
        }

        // Update navigation buttons visibility (optional - always show for looping)
        // You can hide these if you only have 1 page
        if (previousButton != null)
            previousButton.SetActive(controlPages != null && controlPages.Length > 1);

        if (nextButton != null)
            nextButton.SetActive(controlPages != null && controlPages.Length > 1);
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
        // Smart back button - same as ESC
        HandleBackNavigation();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}