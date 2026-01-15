using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject controlsCanvas;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Make sure menus are hidden at start
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (controlsCanvas != null)
            controlsCanvas.SetActive(false);
    }

    void Update()
    {
        // Check for ESC key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                // If controls menu is open, go back to pause menu
                if (controlsCanvas != null && controlsCanvas.activeSelf)
                {
                    ShowPauseMenu();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze game time
        AudioListener.pause = true; // Pause all audio

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume game time
        AudioListener.pause = false; // Resume all audio

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (controlsCanvas != null)
            controlsCanvas.SetActive(false);

        // Lock cursor back (adjust based on your game needs)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowControls()
    {
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (controlsCanvas != null)
            controlsCanvas.SetActive(true);
    }

    public void ShowPauseMenu()
    {
        if (controlsCanvas != null)
            controlsCanvas.SetActive(false);

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);
    }

    public void GoToMainMenu()
    {
        // Resume time before loading main menu
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }
}