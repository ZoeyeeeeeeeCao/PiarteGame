using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Manages audio volume controls for Master, Music, and SFX
/// Connect your sliders to this script's public methods
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [Tooltip("Drag your Audio Mixer asset here")]
    public AudioMixer audioMixer;

    [Header("Volume Sliders")]
    [Tooltip("Master volume slider (controls everything)")]
    public Slider masterSlider;
    [Tooltip("Music volume slider")]
    public Slider musicSlider;
    [Tooltip("SFX volume slider")]
    public Slider sfxSlider;

    [Header("Settings")]
    [Tooltip("Minimum volume in dB (-80 is silent, 0 is full volume)")]
    public float minVolume = -80f;
    [Tooltip("Maximum volume in dB")]
    public float maxVolume = 0f;

    // PlayerPrefs keys for saving settings
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    void Start()
    {
        InitializeAudio();
    }

    // Public method that can be called by other scripts (like PauseManager)
    public void InitializeAudio()
    {
        // Load saved volumes or set defaults
        LoadVolumeSettings();

        // Add listeners to sliders (remove any existing first)
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        Debug.Log("✅ AudioManager initialized");
    }

    // ===== VOLUME CONTROL METHODS =====
    // These methods can be called from UI sliders or buttons

    public void SetMasterVolume(float sliderValue)
    {
        // Convert 0-1 slider value to -80 to 0 dB
        float volume = Mathf.Lerp(minVolume, maxVolume, sliderValue);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", volume);
        }

        // Save to PlayerPrefs
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, sliderValue);
        PlayerPrefs.Save();

        Debug.Log($"🔊 Master Volume: {volume:F1} dB (Slider: {sliderValue:F2})");
    }

    public void SetMusicVolume(float sliderValue)
    {
        float volume = Mathf.Lerp(minVolume, maxVolume, sliderValue);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", volume);
        }

        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, sliderValue);
        PlayerPrefs.Save();

        Debug.Log($"🎵 Music Volume: {volume:F1} dB (Slider: {sliderValue:F2})");
    }

    public void SetSFXVolume(float sliderValue)
    {
        float volume = Mathf.Lerp(minVolume, maxVolume, sliderValue);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", volume);
        }

        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sliderValue);
        PlayerPrefs.Save();

        Debug.Log($"🔔 SFX Volume: {volume:F1} dB (Slider: {sliderValue:F2})");
    }

    // ===== MUTE TOGGLE METHODS =====

    public void ToggleMasterMute(bool isMuted)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", isMuted ? minVolume : 0f);
        }
        Debug.Log($"🔇 Master Muted: {isMuted}");
    }

    public void ToggleMusicMute(bool isMuted)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", isMuted ? minVolume : 0f);
        }
        Debug.Log($"🔇 Music Muted: {isMuted}");
    }

    public void ToggleSFXMute(bool isMuted)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", isMuted ? minVolume : 0f);
        }
        Debug.Log($"🔇 SFX Muted: {isMuted}");
    }

    // ===== LOAD SAVED SETTINGS =====

    private void LoadVolumeSettings()
    {
        // Load Master Volume
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f); // Default to 100%
        if (masterSlider != null)
        {
            masterSlider.value = masterVol;
            Debug.Log($"📂 Loaded Master: {masterVol} (Slider now at: {masterSlider.value})");
        }
        SetMasterVolume(masterVol);

        // Load Music Volume
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.8f); // Default to 80%
        if (musicSlider != null)
        {
            musicSlider.value = musicVol;
            Debug.Log($"📂 Loaded Music: {musicVol} (Slider now at: {musicSlider.value})");
        }
        SetMusicVolume(musicVol);

        // Load SFX Volume
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f); // Default to 100%
        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVol;
            Debug.Log($"📂 Loaded SFX: {sfxVol} (Slider now at: {sfxSlider.value})");
        }
        SetSFXVolume(sfxVol);

        Debug.Log("📂 Volume settings loaded - Check if sliders match expected values!");
    }

    // ===== RESET TO DEFAULTS =====

    public void ResetToDefaults()
    {
        if (masterSlider != null)
        {
            masterSlider.value = 1f;
            SetMasterVolume(1f);
        }

        if (musicSlider != null)
        {
            musicSlider.value = 0.8f;
            SetMusicVolume(0.8f);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = 1f;
            SetSFXVolume(1f);
        }

        Debug.Log("🔄 Audio settings reset to defaults");
    }
    // ===== SCENE TRANSITION HELPERS =====

    public void MuteMusicImmediate()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", minVolume);
            Debug.Log("🎵 Music muted for scene transition");
        }
    }

    void OnDestroy()
    {
        // Remove listeners to prevent errors
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }
}