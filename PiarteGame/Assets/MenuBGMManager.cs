using System.Collections;
using UnityEngine;

/// <summary>
/// Simple menu music manager - plays different music for each panel
/// Attach this to your existing BGM AudioSource GameObject
/// </summary>
public class MenuBGMManager : MonoBehaviour
{
    [Header("Menu Music Clips")]
    [Tooltip("Music for Main Menu")]
    public AudioClip mainMenuMusic;
    [Tooltip("Music for Settings")]
    public AudioClip settingsMusic;
    [Tooltip("Music for Credits")]
    public AudioClip creditsMusic;

    [Header("Audio Source")]
    [Tooltip("Your existing AudioSource component")]
    public AudioSource audioSource;

    [Header("Settings")]
    [Tooltip("Fade duration when switching music")]
    [Range(0f, 3f)]
    public float fadeDuration = 1f;

    [Tooltip("Default volume (0-1)")]
    [Range(0f, 1f)]
    public float defaultVolume = 0.7f;

    [Tooltip("Auto-play main menu music on start")]
    public bool playOnStart = true;

    private Coroutine fadeCoroutine;

    void Start()
    {
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
        Debug.Log($"🔍 AudioSource Settings:");
        Debug.Log($"  - Mute: {audioSource.mute}");
        Debug.Log($"  - Volume: {audioSource.volume}");
        Debug.Log($"  - Output: {(audioSource.outputAudioMixerGroup != null ? audioSource.outputAudioMixerGroup.name : "None (AudioListener)")}");
        Debug.Log($"  - Is Playing: {audioSource.isPlaying}");

        // Play main menu music on start
        if (playOnStart && mainMenuMusic != null)
        {
            PlayMainMenuMusic();
        }
        else if (mainMenuMusic == null)
        {
            Debug.LogError("❌ Main Menu Music clip not assigned!");
        }

        Debug.Log("✅ Menu BGM Manager ready");
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
        if (newClip == null)
        {
            Debug.LogWarning($"⚠️ No music assigned for {menuName}");
            return;
        }

        // If same music is already playing, don't restart
        if (audioSource.clip == newClip && audioSource.isPlaying)
        {
            Debug.Log($"🎵 {menuName} music already playing");
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
}