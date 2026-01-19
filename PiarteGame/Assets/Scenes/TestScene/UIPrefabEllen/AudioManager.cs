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

    // While video/cutscene mute is active, we ignore music slider writes
    private bool _videoMuteActive = false;

    void Start()
    {
        InitializeAudio();
    }

    // Public method that can be called by other scripts (like PauseManager)
    public void InitializeAudio()
    {
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

    public void SetMasterVolume(float sliderValue)
    {
        float volume = Mathf.Lerp(minVolume, maxVolume, sliderValue);

        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", volume);

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, sliderValue);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float sliderValue)
    {
        // If video mute is active, don't let slider fight the mute
        if (_videoMuteActive) return;

        float volume = Mathf.Lerp(minVolume, maxVolume, sliderValue);

        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", volume);

        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, sliderValue);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float sliderValue)
    {
        float volume = Mathf.Lerp(minVolume, maxVolume, sliderValue);

        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", volume);

        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sliderValue);
        PlayerPrefs.Save();
    }

    // ===== MUTE TOGGLE METHODS =====

    public void ToggleMasterMute(bool isMuted)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", isMuted ? minVolume : 0f);

        Debug.Log($"🔇 Master Muted: {isMuted}");
    }

    public void ToggleMusicMute(bool isMuted)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", isMuted ? minVolume : 0f);

        Debug.Log($"🔇 Music Muted: {isMuted}");
    }

    public void ToggleSFXMute(bool isMuted)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", isMuted ? minVolume : 0f);

        Debug.Log($"🔇 SFX Muted: {isMuted}");
    }

    // ===== LOAD SAVED SETTINGS =====

    private void LoadVolumeSettings()
    {
        // Load Master Volume
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        if (masterSlider != null) masterSlider.value = masterVol;
        SetMasterVolume(masterVol);

        // Load Music Volume
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.8f);
        if (musicSlider != null) musicSlider.value = musicVol;

        // Apply music directly to mixer (ignore _videoMuteActive during init)
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", Mathf.Lerp(minVolume, maxVolume, musicVol));

        // Load SFX Volume
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        if (sfxSlider != null) sfxSlider.value = sfxVol;
        SetSFXVolume(sfxVol);

        Debug.Log("📂 Volume settings loaded");
    }

    // ===== RESET TO DEFAULTS (RE-ADDED for your SettingsManager) =====

    public void ResetToDefaults()
    {
        if (masterSlider != null)
        {
            masterSlider.value = 1f;
            SetMasterVolume(1f);
        }
        else
        {
            // Still save/apply even if no slider
            SetMasterVolume(1f);
        }

        if (musicSlider != null)
        {
            musicSlider.value = 0.8f;
            // If video mute active, temporarily allow writing
            bool prev = _videoMuteActive;
            _videoMuteActive = false;
            SetMusicVolume(0.8f);
            _videoMuteActive = prev;
        }
        else
        {
            bool prev = _videoMuteActive;
            _videoMuteActive = false;
            SetMusicVolume(0.8f);
            _videoMuteActive = prev;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = 1f;
            SetSFXVolume(1f);
        }
        else
        {
            SetSFXVolume(1f);
        }

        Debug.Log("🔄 Audio settings reset to defaults");
    }

    // ===== SCENE TRANSITION HELPERS (RE-ADDED for FourBowlPuzzleManager) =====

    public void MuteMusicImmediate()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", minVolume);
            Debug.Log("🎵 Music muted immediate");
        }
    }

    // ===== VIDEO / CUTSCENE HELPERS (NEW) =====

    public void MuteMusicForVideo()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("⚠️ AudioManager: audioMixer not assigned, can't mute music via mixer.");
            return;
        }

        _videoMuteActive = true;

        audioMixer.SetFloat("MusicVolume", minVolume);

        if (audioMixer.GetFloat("MusicVolume", out float db))
            Debug.Log($"🎬 Music muted for video. MusicVolume now = {db:F1} dB");
    }

    public void RestoreMusicAfterVideo()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("⚠️ AudioManager: audioMixer not assigned, can't restore music via mixer.");
            return;
        }

        _videoMuteActive = false;

        float musicSliderValue = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.8f);
        float db = Mathf.Lerp(minVolume, maxVolume, musicSliderValue);

        audioMixer.SetFloat("MusicVolume", db);

        if (audioMixer.GetFloat("MusicVolume", out float nowDb))
            Debug.Log($"🎬 Music restored after video. MusicVolume now = {nowDb:F1} dB (slider={musicSliderValue:F2})");
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
