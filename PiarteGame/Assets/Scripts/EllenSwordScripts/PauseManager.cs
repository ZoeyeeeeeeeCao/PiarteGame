using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using FS_ThirdPerson;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject generalSettingsPanel;
    [SerializeField] private GameObject keyboardControlsPanel;

    [Header("Pause Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button mainMenuButton;

    [Header("General Settings Panel")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Button viewKeyboardControlsButton;

    [Header("Keyboard Controls (Paginated)")]
    [SerializeField] private GameObject[] keyboardPages;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;

    [Header("Universal Back Button")]
    [SerializeField] private Button backButton;

    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private int currentKeyboardPageIndex = 0;
    private MenuState currentMenuState = MenuState.MainPause;

    // PlayerPrefs Keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";

    // Defaults
    private const float DEFAULT_MASTER_VOLUME = 1f;
    private const float DEFAULT_SFX_VOLUME = 1f;
    private const float DEFAULT_MOUSE_SENSITIVITY = 2f;

    private enum MenuState
    {
        MainPause,
        GeneralSettings,
        KeyboardControls
    }

    void Start()
    {
        // Hide all menus at start
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        // Setup button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (controlsButton != null)
            controlsButton.onClick.AddListener(ShowGeneralSettings);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (viewKeyboardControlsButton != null)
            viewKeyboardControlsButton.onClick.AddListener(ShowKeyboardControls);

        if (backButton != null)
            backButton.onClick.AddListener(HandleBackNavigation);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PreviousPage);

        // Setup sliders
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);

        // Load saved settings
        LoadSettings();
    }

    void Update()
    {
        // Check for ESC key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                HandleBackNavigation();
            }
            else
            {
                PauseGame();
            }
        }
    }

    #region Smart Navigation

    private void HandleBackNavigation()
    {
        switch (currentMenuState)
        {
            case MenuState.KeyboardControls:
                ShowGeneralSettings();
                break;
            case MenuState.GeneralSettings:
                ShowMainPause();
                break;
            case MenuState.MainPause:
                ResumeGame();
                break;
        }
    }

    #endregion

    #region Menu Navigation

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        ShowMainPause();

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Hide pause menu
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        currentMenuState = MenuState.MainPause;

        // Lock cursor back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowMainPause()
    {
        currentMenuState = MenuState.MainPause;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);

        // Hide all sub-panels
        if (generalSettingsPanel != null)
            generalSettingsPanel.SetActive(false);
        if (keyboardControlsPanel != null)
            keyboardControlsPanel.SetActive(false);

        // Hide pagination buttons
        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(false);
        if (prevPageButton != null)
            prevPageButton.gameObject.SetActive(false);

        // Hide back button on main pause
        if (backButton != null)
            backButton.gameObject.SetActive(false);
    }

    public void ShowGeneralSettings()
    {
        currentMenuState = MenuState.GeneralSettings;

        // Show general settings panel, hide keyboard controls
        if (generalSettingsPanel != null)
            generalSettingsPanel.SetActive(true);
        if (keyboardControlsPanel != null)
            keyboardControlsPanel.SetActive(false);

        // Hide pagination buttons
        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(false);
        if (prevPageButton != null)
            prevPageButton.gameObject.SetActive(false);

        // Show back button
        if (backButton != null)
            backButton.gameObject.SetActive(true);
    }

    public void ShowKeyboardControls()
    {
        currentMenuState = MenuState.KeyboardControls;

        // Hide general settings, show keyboard controls
        if (generalSettingsPanel != null)
            generalSettingsPanel.SetActive(false);
        if (keyboardControlsPanel != null)
            keyboardControlsPanel.SetActive(true);

        // Show pagination buttons
        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(true);
        if (prevPageButton != null)
            prevPageButton.gameObject.SetActive(true);

        // Show back button
        if (backButton != null)
            backButton.gameObject.SetActive(true);

        currentKeyboardPageIndex = 0;
        UpdateKeyboardPageVisibility();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerPrefs.Save();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion

    #region Keyboard Controls Pagination

    public void NextPage()
    {
        if (keyboardPages.Length == 0) return;
        currentKeyboardPageIndex = (currentKeyboardPageIndex + 1) % keyboardPages.Length;
        UpdateKeyboardPageVisibility();
    }

    public void PreviousPage()
    {
        if (keyboardPages.Length == 0) return;
        currentKeyboardPageIndex--;
        if (currentKeyboardPageIndex < 0)
            currentKeyboardPageIndex = keyboardPages.Length - 1;
        UpdateKeyboardPageVisibility();
    }

    private void UpdateKeyboardPageVisibility()
    {
        for (int i = 0; i < keyboardPages.Length; i++)
        {
            if (keyboardPages[i] != null)
                keyboardPages[i].SetActive(i == currentKeyboardPageIndex);
        }
    }

    #endregion

    #region Audio & Control Settings

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float db = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat("MasterVolume", db);
        }
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float db = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat("SFXVolume", db);
        }
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, sensitivity);
        PlayerPrefs.Save();
        ApplyMouseSensitivityToAllCameras(sensitivity);
    }

    private void ApplyMouseSensitivityToAllCameras(float sensitivity)
    {
        var cameraControllers = FindObjectsOfType<CameraController>();
        foreach (var camController in cameraControllers)
        {
            if (camController.thirdPersonCamera != null && camController.thirdPersonCamera.defaultSettings != null)
            {
                camController.thirdPersonCamera.defaultSettings.sensitivity = sensitivity;
                if (camController.thirdPersonCamera.overrideCameraSettings != null)
                {
                    foreach (var o in camController.thirdPersonCamera.overrideCameraSettings)
                        if (o.settings != null) o.settings.sensitivity = sensitivity;
                }
            }
            if (camController.firstPersonCamera != null && camController.firstPersonCamera.defaultSettings != null)
            {
                camController.firstPersonCamera.defaultSettings.sensitivity = sensitivity;
                if (camController.firstPersonCamera.overrideCameraSettings != null)
                {
                    foreach (var o in camController.firstPersonCamera.overrideCameraSettings)
                        if (o.settings != null) o.settings.sensitivity = sensitivity;
                }
            }
        }
    }

    #endregion

    #region Save/Load System

    private void LoadSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_MASTER_VOLUME);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        float sensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, DEFAULT_MOUSE_SENSITIVITY);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVol;
            SetMasterVolume(masterVol);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVol;
            SetSFXVolume(sfxVol);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = sensitivity;
            SetMouseSensitivity(sensitivity);
        }
    }

    #endregion
}