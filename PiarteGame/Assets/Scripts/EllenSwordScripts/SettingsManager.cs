using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using FS_ThirdPerson;

public class SettingsManager : MonoBehaviour
{
    [Header("Settings Panels")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Tab Buttons")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button controlsTabButton;

    [Header("Smart Navigation")]
    [Tooltip("The button at the top left of your UI")]
    [SerializeField] private Button universalBackButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Audio Manager Reference")]
    [Tooltip("Reference to AudioManager - handles all audio")]
    [SerializeField] private AudioManager audioManager;

    [Header("Audio Settings (Optional - if not using AudioManager)")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private AudioMixer audioMixer;

    [Header("Control Settings")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Button openKeyboardSettingsButton;

    [Header("Keyboard Sub-Menu (Paginated)")]
    [SerializeField] private GameObject keyboardSettingsContainer;
    [SerializeField] private GameObject[] keyboardPages;
    [SerializeField] private Button nextKeyboardPageButton;
    [SerializeField] private Button prevKeyboardPageButton;

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedTabColor = Color.white;
    [SerializeField] private Color unselectedTabColor = Color.gray;

    private int currentKeyboardPageIndex = 0;

    // Made public so SimpleMenuManager can check this
    public bool isInsideKeyboardSubmenu { get; private set; } = false;

    // Brightness overlay variables
    private Image brightnessOverlay;
    private Canvas brightnessCanvas;

    // PlayerPrefs Keys
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
    private const string BRIGHTNESS_KEY = "Brightness";

    // Defaults (0-100 scale for mouse/brightness)
    private const float DEFAULT_MOUSE_SENSITIVITY = 50f;
    private const float DEFAULT_BRIGHTNESS = 50f;

    private void Awake()
    {
        CreateBrightnessOverlay();
    }

    private void Start()
    {
        // 1. Setup Navigation - DON'T use universalBackButton here
        // Let SimpleMenuManager handle the back button instead

        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(() => ShowPanel(audioPanel, audioTabButton));

        if (controlsTabButton != null)
            controlsTabButton.onClick.AddListener(() => ShowPanel(controlsPanel, controlsTabButton));

        // 2. Setup Keyboard Menu
        if (openKeyboardSettingsButton != null)
            openKeyboardSettingsButton.onClick.AddListener(OpenKeyboardSettings);

        if (nextKeyboardPageButton != null)
            nextKeyboardPageButton.onClick.AddListener(NextKeyboardPage);

        if (prevKeyboardPageButton != null)
            prevKeyboardPageButton.onClick.AddListener(PreviousKeyboardPage);

        if (keyboardSettingsContainer != null)
            keyboardSettingsContainer.SetActive(false);

        // 3. Setup Audio - Use AudioManager if available
        if (audioManager != null)
        {
            // AudioManager handles its own sliders, just initialize it
            audioManager.InitializeAudio();
            Debug.Log("✅ Using AudioManager for audio controls");
        }
        else
        {
            // Fallback: Manual audio slider setup
            Debug.LogWarning("⚠️ AudioManager not assigned! Using manual audio setup.");
            SetupManualAudioSliders();
        }

        // 4. Setup Control Sliders
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness);

        // 5. Load Settings
        LoadControlSettings();
        ShowPanel(audioPanel, audioTabButton);
    }

    private void SetupManualAudioSliders()
    {
        // Only use this if AudioManager is not assigned
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolumeManual);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolumeManual);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolumeManual);
    }

    #region Smart Navigation Logic

    public void HandleBackNavigation()
    {
        if (isInsideKeyboardSubmenu)
        {
            CloseKeyboardSettings();
        }
        else
        {
            ReturnToMainMenu();
        }
    }

    public void OpenKeyboardSettings()
    {
        isInsideKeyboardSubmenu = true;
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (keyboardSettingsContainer != null) keyboardSettingsContainer.SetActive(true);

        currentKeyboardPageIndex = 0;
        UpdateKeyboardPageVisibility();

        Debug.Log("⌨️ Opened keyboard settings submenu");
    }

    public void CloseKeyboardSettings()
    {
        isInsideKeyboardSubmenu = false;
        if (keyboardSettingsContainer != null) keyboardSettingsContainer.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);

        Debug.Log("⌨️ Closed keyboard settings submenu");
    }

    public void ReturnToMainMenu()
    {
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion

    #region Looping Pagination Logic

    public void NextKeyboardPage()
    {
        if (keyboardPages.Length == 0) return;
        currentKeyboardPageIndex = (currentKeyboardPageIndex + 1) % keyboardPages.Length;
        UpdateKeyboardPageVisibility();
    }

    public void PreviousKeyboardPage()
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

    #region Panel Management

    private void ShowPanel(GameObject panelToShow, Button selectedButton)
    {
        isInsideKeyboardSubmenu = false;

        if (audioPanel != null) audioPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (keyboardSettingsContainer != null) keyboardSettingsContainer.SetActive(false);

        if (panelToShow != null) panelToShow.SetActive(true);
        UpdateButtonVisuals(selectedButton);
    }

    private void UpdateButtonVisuals(Button selectedButton)
    {
        if (audioTabButton != null) SetButtonColor(audioTabButton, unselectedTabColor);
        if (controlsTabButton != null) SetButtonColor(controlsTabButton, unselectedTabColor);

        if (selectedButton != null)
            SetButtonColor(selectedButton, selectedTabColor);
    }

    private void SetButtonColor(Button btn, Color col)
    {
        var colors = btn.colors;
        colors.normalColor = col;
        btn.colors = colors;
    }

    #endregion

    #region Manual Audio Settings (Fallback)

    private void SetMasterVolumeManual(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float db = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat("MasterVolume", db);
        }
    }

    private void SetSFXVolumeManual(float volume)
    {
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float db = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat("SFXVolume", db);
        }
    }

    private void SetBGMVolumeManual(float volume)
    {
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float db = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat("MusicVolume", db);
        }
    }

    #endregion

    #region Control Settings - SAME AS PAUSEMANAGER

    public void SetMouseSensitivity(float value)
    {
        ApplyMouseSensitivity(value);
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, value);
        PlayerPrefs.Save();
    }

    private void ApplyMouseSensitivity(float value)
    {
        var cameraController = FindObjectOfType<FS_ThirdPerson.CameraController>();

        if (cameraController != null)
        {
            // Map 0-100 slider to 0.1-2.0 sensitivity range
            float sensitivity = Mathf.Lerp(0.1f, 2.0f, value / 100f);

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
            Debug.LogWarning("⚠️ CameraController (FS_ThirdPerson) not found!");
        }
    }

    public void SetBrightness(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, value);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness(float value)
    {
        // Method 1: Try Post-Processing (if available)
        bool postProcessingWorked = TryApplyPostProcessingBrightness(value);

        // Method 2: Fallback to overlay (always works)
        if (!postProcessingWorked && brightnessOverlay != null)
        {
            if (value < 50f)
            {
                // Darken with black overlay
                float alpha = Mathf.Lerp(0.7f, 0f, value / 50f);
                brightnessOverlay.color = new Color(0, 0, 0, alpha);
            }
            else if (value > 50f)
            {
                // Brighten with white overlay
                float alpha = Mathf.Lerp(0f, 0.3f, (value - 50f) / 50f);
                brightnessOverlay.color = new Color(1, 1, 1, alpha);
            }
            else
            {
                // Normal brightness (50)
                brightnessOverlay.color = new Color(0, 0, 0, 0);
            }

            Debug.Log($"💡 Brightness (Overlay): {value}");
        }
    }

    private bool TryApplyPostProcessingBrightness(float value)
    {
        try
        {
            UnityEngine.Rendering.Volume volume = FindObjectOfType<UnityEngine.Rendering.Volume>();

            if (volume != null && volume.profile != null)
            {
                if (volume.profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustments))
                {
                    float exposure = ((value - 50f) / 50f) * 2f;
                    colorAdjustments.postExposure.overrideState = true;
                    colorAdjustments.postExposure.value = exposure;
                    Debug.Log($"💡 Brightness (Post-Processing): {value} | Exposure: {exposure:F2}");
                    return true;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Post-Processing not available: {e.Message}");
        }

        return false;
    }

    private void CreateBrightnessOverlay()
    {
        GameObject canvasObj = new GameObject("BrightnessOverlay_Settings");
        brightnessCanvas = canvasObj.AddComponent<Canvas>();
        brightnessCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        brightnessCanvas.sortingOrder = 9999;
        DontDestroyOnLoad(canvasObj);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);

        brightnessOverlay = overlayObj.AddComponent<Image>();
        brightnessOverlay.color = new Color(0, 0, 0, 0);
        brightnessOverlay.raycastTarget = false;

        RectTransform rt = overlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    #endregion

    #region Save/Load System

    private void LoadControlSettings()
    {
        // Control settings (0-100 scale)
        float sensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, DEFAULT_MOUSE_SENSITIVITY);
        float brightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, DEFAULT_BRIGHTNESS);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0f;
            mouseSensitivitySlider.maxValue = 100f;
            mouseSensitivitySlider.value = sensitivity;
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 100f;
            brightnessSlider.value = brightness;
        }

        // Apply settings
        ApplyMouseSensitivity(sensitivity);
        ApplyBrightness(brightness);
    }

    public void ResetToDefaults()
    {
        // Use AudioManager's reset if available
        if (audioManager != null)
        {
            audioManager.ResetToDefaults();
        }
        else
        {
            // Manual reset
            if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
            if (bgmVolumeSlider != null) bgmVolumeSlider.value = 0.8f;
        }

        // Reset controls
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = DEFAULT_MOUSE_SENSITIVITY;
        if (brightnessSlider != null) brightnessSlider.value = DEFAULT_BRIGHTNESS;
    }

    #endregion

    #region Auto-Application

    public static float GetMouseSensitivity() => PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, DEFAULT_MOUSE_SENSITIVITY);

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMouseSensitivity(GetMouseSensitivity());
        ApplyBrightness(PlayerPrefs.GetFloat(BRIGHTNESS_KEY, DEFAULT_BRIGHTNESS));
    }

    #endregion
}