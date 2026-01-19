using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Audio Manager Reference")]
    [Tooltip("Reference to your AudioManager - it will handle all audio")]
    [SerializeField] private AudioManager audioManager;

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
    [Tooltip("Audio sliders - these should match the ones in AudioManager")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [Tooltip("Non-audio sliders")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider brightnessSlider;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Player Reference")]
    [SerializeField] private GameObject player;

    private bool isPaused = false;
    private int currentControlPageIndex = 0;
    private bool isInControlsView = false;
    private bool isProcessingStateChange = false;

    // Default values
    private const float DEFAULT_MOUSE_SENSITIVITY = 50f;
    private const float DEFAULT_BRIGHTNESS = 50f;

    void Start()
    {
        isPaused = false;
        isInControlsView = false;
        Time.timeScale = 1f;

        // Hide all menus at start
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
        InitializeNonAudioSliders();
        LoadNonAudioSettings();

        // No need to connect audio sliders - they're already connected in AudioManager!
        if (audioManager != null)
        {
            Debug.Log("✅ AudioManager found - audio sliders should already be working");
        }
        else
        {
            Debug.LogError("⚠️ AudioManager not assigned! Audio controls won't work.");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !IsTypingInInputField())
        {
            HandleBackNavigation();
        }
    }

    private bool IsTypingInInputField()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null &&
               UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null;
    }

    // ===== NON-AUDIO SETTINGS =====

    private void InitializeNonAudioSliders()
    {
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

    private void LoadNonAudioSettings()
    {
        float mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", DEFAULT_MOUSE_SENSITIVITY);
        float brightness = PlayerPrefs.GetFloat("Brightness", DEFAULT_BRIGHTNESS);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = mouseSensitivity;
        if (brightnessSlider != null)
            brightnessSlider.value = brightness;

        ApplyMouseSensitivity(mouseSensitivity);
        ApplyBrightness(brightness);
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
        // Find the Fantacode Studios CameraController
        var cameraController = FindObjectOfType<FS_ThirdPerson.CameraController>();

        if (cameraController != null)
        {
            // Map 0-100 slider to 0.1-2.0 sensitivity range (adjust as needed)
            // The CameraSettings.sensitivity field uses 0-10 range typically
            float sensitivity = Mathf.Lerp(0.1f, 2.0f, value / 100f);

            // Update both third-person and first-person camera settings
            if (cameraController.thirdPersonCamera != null && cameraController.thirdPersonCamera.defaultSettings != null)
            {
                cameraController.thirdPersonCamera.defaultSettings.sensitivity = sensitivity;
            }

            if (cameraController.firstPersonCamera != null && cameraController.firstPersonCamera.defaultSettings != null)
            {
                cameraController.firstPersonCamera.defaultSettings.sensitivity = sensitivity;
            }

            Debug.Log($"🖱️ Camera Sensitivity set to: {sensitivity:F2} (Slider: {value})");
        }
        else
        {
            Debug.LogWarning("⚠️ CameraController (FS_ThirdPerson) not found! Mouse sensitivity not applied.");
        }
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
        // Use Post-Processing Volume (URP Color Adjustments)
        UnityEngine.Rendering.Volume volume = FindObjectOfType<UnityEngine.Rendering.Volume>();

        if (volume != null && volume.profile != null)
        {
            // Try to get Color Adjustments override
            if (volume.profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustments))
            {
                // Map 0-100 slider to post-exposure range
                // 50 = normal (0 exposure)
                // 0 = darkest (-2 exposure)
                // 100 = brightest (+2 exposure)
                float exposure = ((value - 50f) / 50f) * 2f;

                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = exposure;

                Debug.Log($"💡 Brightness: {value} | Post-Exposure: {exposure:F2}");
            }
            else
            {
                Debug.LogError("⚠️ Color Adjustments not found in Volume Profile! Add it in the Post-Processing Volume.");
            }
        }
        else
        {
            Debug.LogError("⚠️ Post-Processing Volume not found! Make sure you have a Global Volume with a Profile in your scene.");
        }
    }

    // ===== PAUSE/RESUME =====

    private void HandleBackNavigation()
    {
        if (!isPaused)
        {
            PauseGame();
        }
        else if (isInControlsView)
        {
            CloseControlsView();
        }
        else
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused || isProcessingStateChange) return;

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
        Debug.Log($"⏸️ Game Paused");
    }

    public void ResumeGame()
    {
        if (!isPaused || isProcessingStateChange) return;

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
        Debug.Log($"▶️ Game Resumed");
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
            CloseControlsView();
        else
            ResumeGame();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnDestroy()
    {
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
    }
}