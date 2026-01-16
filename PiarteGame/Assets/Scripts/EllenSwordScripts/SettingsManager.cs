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

    [Header("Audio Settings")]
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
    private bool isInsideKeyboardSubmenu = false;

    // Brightness overlay variables
    private Image brightnessOverlay;
    private Canvas brightnessCanvas;

    // PlayerPrefs Keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
    private const string BRIGHTNESS_KEY = "Brightness";

    // Defaults
    private const float DEFAULT_MASTER_VOLUME = 1f;
    private const float DEFAULT_SFX_VOLUME = 1f;
    private const float DEFAULT_BGM_VOLUME = 1f;
    private const float DEFAULT_MOUSE_SENSITIVITY = 2f;
    private const float DEFAULT_BRIGHTNESS = 1f;

    private void Awake()
    {
        CreateBrightnessOverlay();
    }

    private void Start()
    {
        // 1. Setup Navigation
        if (universalBackButton != null)
            universalBackButton.onClick.AddListener(HandleBackNavigation);

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

        // 3. Setup Sliders
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(SetBrightness);

        // 4. Initialization
        LoadSettings();
        ShowPanel(audioPanel, audioTabButton);
    }

    #region Smart Navigation Logic

    /// <summary>
    /// Handles the logic for the single top-left back button.
    /// </summary>
    public void HandleBackNavigation()
    {
        if (isInsideKeyboardSubmenu)
        {
            // If we are deep in keyboard settings, go back to the main Controls panel
            CloseKeyboardSettings();
        }
        else
        {
            // If we are on the main tabs, exit to the Main Menu
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
    }

    public void CloseKeyboardSettings()
    {
        isInsideKeyboardSubmenu = false;
        if (keyboardSettingsContainer != null) keyboardSettingsContainer.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
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

        // Loop using modulo
        currentKeyboardPageIndex = (currentKeyboardPageIndex + 1) % keyboardPages.Length;
        UpdateKeyboardPageVisibility();
    }

    public void PreviousKeyboardPage()
    {
        if (keyboardPages.Length == 0) return;

        currentKeyboardPageIndex--;

        // Loop back to end if below zero
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
        // If the user clicks a top tab, they are no longer in the keyboard sub-menu depth
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

    #region Audio Settings

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

    public void SetBGMVolume(float volume)
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float db = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat("BGMVolume", db);
        }
    }

    #endregion

    #region Control Settings

    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, sensitivity);
        PlayerPrefs.Save();
        ApplyMouseSensitivityToAllCameras(sensitivity);
    }

    public void SetBrightness(float brightness)
    {
        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, brightness);
        PlayerPrefs.Save();
        ApplyBrightness(brightness);
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

    private void CreateBrightnessOverlay()
    {
        GameObject canvasObj = new GameObject("BrightnessOverlay");
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
        brightnessOverlay.color = new Color(0, 0, 0, 0.5f);
        brightnessOverlay.raycastTarget = false;

        RectTransform rt = overlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private void ApplyBrightness(float brightness)
    {
        if (brightnessOverlay != null)
        {
            float alpha = Mathf.Lerp(0.8f, 0f, brightness);
            Color col = brightnessOverlay.color;
            col.a = alpha;
            brightnessOverlay.color = col;
        }
    }

    #endregion

    #region Save/Load System

    private void LoadSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_MASTER_VOLUME);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        float bgmVol = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, DEFAULT_BGM_VOLUME);

        if (masterVolumeSlider != null) { masterVolumeSlider.value = masterVol; SetMasterVolume(masterVol); }
        if (sfxVolumeSlider != null) { sfxVolumeSlider.value = sfxVol; SetSFXVolume(sfxVol); }
        if (bgmVolumeSlider != null) { bgmVolumeSlider.value = bgmVol; SetBGMVolume(bgmVol); }

        float sensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, DEFAULT_MOUSE_SENSITIVITY);
        float brightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, DEFAULT_BRIGHTNESS);

        if (mouseSensitivitySlider != null) { mouseSensitivitySlider.value = sensitivity; SetMouseSensitivity(sensitivity); }
        if (brightnessSlider != null) { brightnessSlider.value = brightness; SetBrightness(brightness); }
    }

    public void ResetToDefaults()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = DEFAULT_MASTER_VOLUME;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = DEFAULT_SFX_VOLUME;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = DEFAULT_BGM_VOLUME;
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
        ApplyMouseSensitivityToAllCameras(GetMouseSensitivity());
        ApplyBrightness(PlayerPrefs.GetFloat(BRIGHTNESS_KEY, DEFAULT_BRIGHTNESS));
    }

    #endregion
}