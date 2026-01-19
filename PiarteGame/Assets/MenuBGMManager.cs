using System.Collections;
using UnityEngine;

/// <summary>
/// Persistent menu music manager - survives scene changes and never restarts
/// Attach this to your existing BGM AudioSource GameObject
/// </summary>
public class MenuBGMManager : MonoBehaviour
{
    [Header("Menu Music Clips")]
    [Tooltip("Music for Main Menu")]
    public AudioClip mainMenuMusic;
    [Tooltip("Music for Settings (leave empty to use Main Menu music)")]
    public AudioClip settingsMusic;
    [Tooltip("Music for Credits (leave empty to use Main Menu music)")]
    public AudioClip creditsMusic;

    [Header("Continuous Music Option")]
    [Tooltip("If true, uses same music for all menus (never switches)")]
    public bool useContinuousMusic = true;

    [Header("Audio Source")]
    [Tooltip("Your existing AudioSource component")]
    public AudioSource audioSource;

    [Header("Settings")]
    [Tooltip("Fade duration when switching music")]
    [Range(0f, 3f)]
    public float fadeDuration = 1f;

    [Tooltip("Default volume (0-1) - Set higher since AudioMixer controls final output")]
    [Range(0f, 1f)]
    public float defaultVolume = 1f;

    [Tooltip("Auto-play main menu music on start")]
    public bool playOnStart = true;

    private Coroutine fadeCoroutine;

    // Singleton to prevent multiple instances
    private static MenuBGMManager instance;
    public static MenuBGMManager Instance => instance;

    void Awake()
    {
        // Singleton pattern - keep only one instance alive
        if (instance != null && instance != this)
        {
            Debug.Log($"🔄 MenuBGMManager already exists - destroying duplicate on '{gameObject.name}' and keeping music playing");
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Move to root if it's a child of something
        if (transform.parent != null)
        {
            Debug.LogWarning("⚠️ MenuBGMManager should be at root level! Moving to root...");
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject); // Make it survive scene changes
        Debug.Log($"✅ MenuBGMManager set as persistent singleton on '{gameObject.name}'");
    }

    void Start()
    {
        // Only initialize if this is the singleton instance
        if (instance != this)
        {
            Debug.LogWarning($"⚠️ MenuBGMManager Start() called on non-singleton instance '{gameObject.name}' - ignoring");
            return;
        }

        // Find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError("❌ No AudioSource found! Please assign one or attach this script to an AudioSource.");
            return;
        }

        // Setup AudioSource
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // DEBUG: Check AudioSource settings
        Debug.Log($"🔍 AudioSource Settings on '{gameObject.name}':");
        Debug.Log($"  - Mute: {audioSource.mute}");
        Debug.Log($"  - Volume: {audioSource.volume}");
        Debug.Log($"  - Output: {(audioSource.outputAudioMixerGroup != null ? audioSource.outputAudioMixerGroup.name : "None (AudioListener)")}");
        Debug.Log($"  - Is Playing: {audioSource.isPlaying}");
        Debug.Log($"  - Clip: {(audioSource.clip != null ? audioSource.clip.name : "None")}");
        Debug.Log($"  - Time: {audioSource.time:F2}");

        // DEBUG: Check AudioMixer values
        if (audioSource.outputAudioMixerGroup != null && audioSource.outputAudioMixerGroup.audioMixer != null)
        {
            float mixerVolume;
            if (audioSource.outputAudioMixerGroup.audioMixer.GetFloat("MusicVolume", out mixerVolume))
            {
                Debug.Log($"🎚️ AudioMixer MusicVolume: {mixerVolume} dB");
            }
            if (audioSource.outputAudioMixerGroup.audioMixer.GetFloat("MasterVolume", out mixerVolume))
            {
                Debug.Log($"🎚️ AudioMixer MasterVolume: {mixerVolume} dB");
            }
        }

        // Only play main menu music if not already playing
        if (playOnStart && mainMenuMusic != null && !audioSource.isPlaying)
        {
            PlayMainMenuMusic();
        }
        else if (audioSource.isPlaying)
        {
            Debug.Log($"🎵 Music already playing from previous scene - continuing seamlessly (Time: {audioSource.time:F2})");
        }
        else if (mainMenuMusic == null)
        {
            Debug.LogError("❌ Main Menu Music clip not assigned!");
        }

        Debug.Log($"✅ Menu BGM Manager ready on '{gameObject.name}'");
    }

    // ===== PUBLIC METHODS (Call from SimpleMenuManager) =====

    public void PlayMainMenuMusic()
    {
        SwitchMusic(mainMenuMusic, "Main Menu");
    }

    public void PlaySettingsMusic()
    {
        SwitchMusic(settingsMusic, "Settings");
    }

    public void PlayCreditsMusic()
    {
        SwitchMusic(creditsMusic, "Credits");
    }

    public void StopMusic()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        audioSource.Stop();
        Debug.Log("⏹️ Music stopped");
    }

    // ===== PRIVATE METHODS =====

    private void SwitchMusic(AudioClip newClip, string menuName)
    {
        // If continuous music is enabled, only play main menu music
        if (useContinuousMusic)
        {
            newClip = mainMenuMusic;
        }

        if (newClip == null)
        {
            Debug.LogWarning($"⚠️ No music assigned for {menuName}");
            return;
        }

        // If same music is already playing, don't restart
        if (audioSource.clip == newClip && audioSource.isPlaying)
        {
            Debug.Log($"🎵 {menuName} music already playing (continuous - no restart)");
            return;
        }

        Debug.Log($"🎵 Switching to {menuName} music");
        Debug.Log($"  - Clip: {newClip.name}");
        Debug.Log($"  - AudioSource Volume: {audioSource.volume}");
        Debug.Log($"  - AudioSource Mute: {audioSource.mute}");

        // Stop any existing fade
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Start fade transition
        if (fadeDuration > 0 && audioSource.isPlaying)
        {
            fadeCoroutine = StartCoroutine(FadeToNewMusic(newClip));
        }
        else
        {
            // Immediate switch (no fade)
            audioSource.Stop();
            audioSource.clip = newClip;
            audioSource.volume = defaultVolume;
            audioSource.Play();

            Debug.Log($"▶️ Started playing {menuName} music");
            Debug.Log($"  - Is Playing: {audioSource.isPlaying}");
            Debug.Log($"  - Time: {audioSource.time}");
        }
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for menus
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2f));
            yield return null;
        }

        // Switch track
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in
        elapsed = 0f;
        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0f, defaultVolume, elapsed / (fadeDuration / 2f));
            yield return null;
        }

        audioSource.volume = defaultVolume;
        fadeCoroutine = null;
    }

    void OnDestroy()
    {
        // Only clear instance if this is the singleton
        if (instance == this)
        {
            Debug.Log("🔴 MenuBGMManager singleton destroyed");
            instance = null;
        }
    }
}