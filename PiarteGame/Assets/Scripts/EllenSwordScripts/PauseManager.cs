using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class PauseManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [Tooltip("Drag your Audio Mixer asset here")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private GameObject mainButtons;

    [Header("Controls Pages (Paginated)")]
    [SerializeField] private GameObject controlsContainer;
    [SerializeField] private GameObject[] controlPages;
    [SerializeField] private GameObject nextPageButton;
    [SerializeField] private GameObject prevPageButton;
    [SerializeField] private GameObject backButton;

    [Header("Settings Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider brightnessSlider;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Player Reference")]
    [SerializeField] private GameObject player;

    private bool isPaused = false;
    private int currentControlPageIndex = 0;
    private bool isInControlsView = false;
    private bool isProcessingStateChange = false; // NEW: Prevent race conditions

    // Default values (0-1 range for audio mixer)
    private const float DEFAULT_MOUSE_SENSITIVITY = 50f;
    private const float DEFAULT_BRIGHTNESS = 50f;
    private const float DEFAULT_VOLUME = 0.8f; // 80% as default

    void Start()
    {
        // Ensure game starts unpaused
        isPaused = false;
        isInControlsView = false;
        Time.timeScale = 1f;

        // Make sure menus are hidden at start
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (controlsContainer != null)
            controlsContainer.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainButtons != null)
            mainButtons.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        HideAllControlPages();
        InitializeSliders();
        LoadSettings();

        // Ensure cursor is locked at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Only check for ESC if not typing in input field
        if (Input.GetKeyDown(KeyCode.Escape) && !IsTypingInInputField())
        {
            HandleBackNavigation();
        }
    }

    private bool IsTypingInInputField()
    {
        // Check if user is typing in an input field
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null &&
               UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null;
    }

    private void InitializeSliders()
    {
        // Audio sliders: 0-1 range (works better with Audio Mixer)
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.wholeNumbers = false;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.wholeNumbers = false;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.minValue = 0f;
            bgmVolumeSlider.maxValue = 1f;
            bgmVolumeSlider.wholeNumbers = false;
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0f;
            mouseSensitivitySlider.maxValue = 100f;
            mouseSensitivitySlider.wholeNumbers = false;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 100f;
            brightnessSlider.wholeNumbers = false;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }
    }

    private void LoadSettings()
    {
        // Load saved settings (0-1 range for audio)
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", DEFAULT_VOLUME);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", DEFAULT_VOLUME);
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", DEFAULT_VOLUME);
        float mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", DEFAULT_MOUSE_SENSITIVITY);
        float brightness = PlayerPrefs.GetFloat("Brightness", DEFAULT_BRIGHTNESS);

        // Set slider values
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgmVolume;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = mouseSensitivity;
        if (brightnessSlider != null) brightnessSlider.value = brightness;

        // Apply settings
        ApplyMasterVolume(masterVolume);
        ApplySFXVolume(sfxVolume);
        ApplyBGMVolume(bgmVolume);
        ApplyMouseSensitivity(mouseSensitivity);
        ApplyBrightness(brightness);
    }

    // ===== AUDIO CONTROLS =====

    private void OnMasterVolumeChanged(float value)
    {
        ApplyMasterVolume(value);
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplyMasterVolume(float value)
    {
        if (audioMixer != null)
        {
            // Convert 0-1 to -80dB to 0dB (logarithmic scale)
            float dB = value > 0 ? 20f * Mathf.Log10(value) : -80f;
            audioMixer.SetFloat("MasterVolume", dB);
        }
        else
        {
            Debug.LogWarning("Audio Mixer not assigned! Please assign it in the Inspector.");
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        ApplySFXVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplySFXVolume(float value)
    {
        if (audioMixer != null)
        {
            float dB = value > 0 ? 20f * Mathf.Log10(value) : -80f;
            audioMixer.SetFloat("SFXVolume", dB);
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        ApplyBGMVolume(value);
        PlayerPrefs.SetFloat("BGMVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplyBGMVolume(float value)
    {
        if (audioMixer != null)
        {
            float dB = value > 0 ? 20f * Mathf.Log10(value) : -80f;
            audioMixer.SetFloat("MusicVolume", dB); // Changed to MusicVolume to match AudioManager
        }
    }

    // ===== MOUSE SENSITIVITY =====

    private void OnMouseSensitivityChanged(float value)
    {
        ApplyMouseSensitivity(value);
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }

    private void ApplyMouseSensitivity(float value)
    {
        // Store for other scripts to read
        // Your player controller can read this via: PlayerPrefs.GetFloat("MouseSensitivity", 50f)

        // If you have a specific player controller, apply it here
        // Example:
        /*
        if (player != null)
        {
            var controller = player.GetComponent<YourPlayerController>();
            if (controller != null)
            {
                controller.mouseSensitivity = value;
            }
        }
        */
    }

    // ===== BRIGHTNESS =====

    private void OnBrightnessChanged(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat("Brightness", value);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness(float value)
    {
        float normalizedBrightness = value / 50f;
        RenderSettings.ambientIntensity = normalizedBrightness;

        // Optional: Use UI overlay
        GameObject brightnessOverlay = GameObject.Find("BrightnessOverlay");
        if (brightnessOverlay != null)
        {
            CanvasGroup canvasGroup = brightnessOverlay.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - (value / 100f);
            }
        }
    }

    // ===== PAUSE/RESUME =====

    private void HandleBackNavigation()
    {
        Debug.Log($"[ESC] isPaused: {isPaused}, isInControlsView: {isInControlsView}, TimeScale: {Time.timeScale}");

        if (!isPaused)
        {
            // Game is running, pause it
            Debug.Log("[ESC] → PauseGame()");
            PauseGame();
        }
        else if (isInControlsView)
        {
            // In controls view, go back to settings
            Debug.Log("[ESC] → CloseControlsView()");
            CloseControlsView();
        }
        else
        {
            // In settings view, resume game
            Debug.Log("[ESC] → ResumeGame()");
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        Debug.Log($"[PAUSE] Called - Current isPaused: {isPaused}, TimeScale: {Time.timeScale}");

        // Prevent double-pausing or race conditions
        if (isPaused || isProcessingStateChange)
        {
            Debug.LogWarning($"[PAUSE] Blocked! isPaused: {isPaused}, isProcessing: {isProcessingStateChange}");
            return;
        }

        isProcessingStateChange = true;
        isPaused = true;
        isInControlsView = false;
        Time.timeScale = 0f;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);

        ShowSettingsPanel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isProcessingStateChange = false;
        Debug.Log($"[PAUSE] Complete - isPaused: {isPaused}, TimeScale: {Time.timeScale}");
    }

    public void ResumeGame()
    {
        // Get the call stack to see what's calling this
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
        Debug.Log($"[RESUME] Called from: {stackTrace.GetFrame(1).GetMethod().Name}");
        Debug.Log($"[RESUME] Current isPaused: {isPaused}, TimeScale: {Time.timeScale}");

        // Prevent double-resuming or race conditions
        if (!isPaused || isProcessingStateChange)
        {
            Debug.LogWarning($"[RESUME] Blocked! isPaused: {isPaused}, isProcessing: {isProcessingStateChange}");
            return;
        }

        isProcessingStateChange = true;
        isPaused = false;
        isInControlsView = false;
        Time.timeScale = 1f;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainButtons != null)
            mainButtons.SetActive(false);

        if (controlsContainer != null)
            controlsContainer.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isProcessingStateChange = false;
        Debug.Log($"[RESUME] Complete - isPaused: {isPaused}, TimeScale: {Time.timeScale}");
    }

    // ===== UI NAVIGATION =====

    private void ShowSettingsPanel()
    {
        isInControlsView = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (mainButtons != null)
            mainButtons.SetActive(true);

        if (controlsContainer != null)
            controlsContainer.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);
    }

    public void OpenControlsView()
    {
        if (!isPaused) return;

        isInControlsView = true;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainButtons != null)
            mainButtons.SetActive(false);

        if (controlsContainer != null)
            controlsContainer.SetActive(true);

        if (backButton != null)
            backButton.SetActive(true);

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
        currentControlPageIndex = (currentControlPageIndex + 1) % controlPages.Length;
        UpdateControlPageVisibility();
    }

    public void PreviousControlPage()
    {
        if (controlPages == null || controlPages.Length == 0) return;
        currentControlPageIndex--;
        if (currentControlPageIndex < 0)
            currentControlPageIndex = controlPages.Length - 1;
        UpdateControlPageVisibility();
    }

    private void UpdateControlPageVisibility()
    {
        for (int i = 0; i < controlPages.Length; i++)
        {
            if (controlPages[i] != null)
                controlPages[i].SetActive(i == currentControlPageIndex);
        }

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
        if (isInControlsView)
        {
            CloseControlsView();
        }
        else
        {
            ResumeGame();
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
    }
}