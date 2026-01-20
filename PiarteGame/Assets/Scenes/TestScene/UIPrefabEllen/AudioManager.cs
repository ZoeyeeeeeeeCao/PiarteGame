using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Manages audio volume controls for Master, Music, and SFX
/// Scene-local AudioManager (no singleton, no DontDestroyOnLoad)
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

    // PlayerPrefs keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    // Prevent slider fighting during video / death mute
    private bool _videoMuteActive = false;

    private void Start()
    {
        InitializeAudio();
    }

    // ==========================
    // INITIALIZATION
    // ==========================

    public void InitializeAudio()
    {
        LoadVolumeSettings();

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

        Debug.Log("✅ AudioManager initialized (scene-local)");
    }

    // ==========================
    // VOLUME CONTROLS
    // ==========================

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

    // ==========================
    // MUTE TOGGLES
    // ==========================

    public void ToggleMasterMute(bool isMuted)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", isMuted ? minVolume : 0f);
    }

    public void ToggleMusicMute(bool isMuted)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", isMuted ? minVolume : 0f);
    }

    public void ToggleSFXMute(bool isMuted)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", isMuted ? minVolume : 0f);
    }

    // ==========================
    // LOAD SAVED SETTINGS
    // ==========================

    private void LoadVolumeSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        if (masterSlider != null) masterSlider.value = masterVol;
        SetMasterVolume(masterVol);

        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.8f);
        if (musicSlider != null) musicSlider.value = musicVol;
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", Mathf.Lerp(minVolume, maxVolume, musicVol));

        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        if (sfxSlider != null) sfxSlider.value = sfxVol;
        SetSFXVolume(sfxVol);

        Debug.Log("📂 Volume settings loaded");
    }

    // ==========================
    // RESET
    // ==========================

    public void ResetToDefaults()
    {
        SetMasterVolume(1f);

        bool prev = _videoMuteActive;
        _videoMuteActive = false;
        SetMusicVolume(0.8f);
        _videoMuteActive = prev;

        SetSFXVolume(1f);

        if (masterSlider) masterSlider.value = 1f;
        if (musicSlider) musicSlider.value = 0.8f;
        if (sfxSlider) sfxSlider.value = 1f;
    }

    // ==========================
    // VIDEO / CUTSCENE
    // ==========================

    public void MuteMusicForVideo()
    {
        if (audioMixer == null) return;

        _videoMuteActive = true;
        audioMixer.SetFloat("MusicVolume", minVolume);
    }

    public void RestoreMusicAfterVideo()
    {
        if (audioMixer == null) return;

        _videoMuteActive = false;

        float musicSliderValue = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.8f);
        audioMixer.SetFloat(
            "MusicVolume",
            Mathf.Lerp(minVolume, maxVolume, musicSliderValue)
        );
    }

    // ==========================
    // PLAYER DEATH / RESTART
    // ==========================

    public void MuteMusicOnDeath()
    {
        if (audioMixer == null) return;

        _videoMuteActive = true;
        audioMixer.SetFloat("MusicVolume", minVolume);
    }

    public void RestoreMusicOnRestart()
    {
        if (audioMixer == null) return;

        _videoMuteActive = false;

        float musicSliderValue = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.8f);
        audioMixer.SetFloat(
            "MusicVolume",
            Mathf.Lerp(minVolume, maxVolume, musicSliderValue)
        );
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    // ==========================
    // IMMEDIATE MUSIC MUTE
    // ==========================

    public void MuteMusicImmediate()
    {
        if (audioMixer == null) return;

        _videoMuteActive = true;
        audioMixer.SetFloat("MusicVolume", minVolume);

        Debug.Log("🎵 Music muted immediately");
    }

}
