using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enhanced BGM Manager - Supports both collider triggers and manual events
/// Attach this to a GameObject in your scene (e.g., "BGM Manager")
/// </summary>
public class BGMZoneManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicZone
    {
        [Tooltip("Name of this music zone (for debugging)")]
        public string zoneName = "Zone 1";

        [Tooltip("The background music for this zone")]
        public AudioClip bgmClip;

        [Tooltip("Trigger collider that activates this music (optional - can be null for manual triggers)")]
        public Collider zoneTrigger;

        [Tooltip("Volume for this BGM (0-1)")]
        [Range(0f, 1f)]
        public float volume = 0.7f;

        [Header("Auto-Activation Settings")]
        [Tooltip("Should this zone's collider be active at start?")]
        public bool colliderActiveAtStart = true;
    }

    [Header("Music Zones")]
    [Tooltip("List of all music zones in your game")]
    public List<MusicZone> musicZones = new List<MusicZone>();

    [Header("Audio Settings")]
    [Tooltip("The AudioSource that plays background music")]
    public AudioSource bgmAudioSource;

    [Tooltip("Fade transition duration in seconds")]
    [Range(0.1f, 5f)]
    public float fadeDuration = 1.5f;

    [Tooltip("Type of transition between tracks")]
    public TransitionType transitionType = TransitionType.Fade;

    [Header("Default Music")]
    [Tooltip("Default BGM to play at start (optional)")]
    public AudioClip defaultBGM;

    [Tooltip("Default BGM volume")]
    [Range(0f, 1f)]
    public float defaultVolume = 0.7f;

    [Header("Advanced Settings")]
    [Tooltip("Prevent the same music from restarting when re-entering zone")]
    public bool preventRestart = true;

    [Tooltip("Tag required on player GameObject")]
    public string playerTag = "Player";

    // Private variables
    private MusicZone currentZone;
    private Coroutine transitionCoroutine;
    private AudioSource secondaryAudioSource; // For crossfade

    // ⭐ NEW: last played track memory (so restart can resume it)
    private AudioClip _lastPlayedClip;
    private float _lastPlayedVolume = 0.7f;

    // ⭐ NEW: if player is dead, block zone triggers from starting music again
    private bool _musicLockedBecauseDead = false;

    // Singleton instance for easy access from other scripts
    public static BGMZoneManager Instance { get; private set; }

    public enum TransitionType
    {
        Fade,           // Fade out → Fade in
        Crossfade,      // Both tracks play during transition
        Immediate       // No transition
    }

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ⭐ NEW: listen for scene loads so we can restart the last song after death reload
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Setup audio sources
        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;

        // Create secondary audio source for crossfading
        secondaryAudioSource = gameObject.AddComponent<AudioSource>();
        secondaryAudioSource.loop = true;
        secondaryAudioSource.playOnAwake = false;
        secondaryAudioSource.volume = 0f;

        // Register all zone triggers
        RegisterZoneTriggers();

        // Play default BGM if assigned (only if nothing remembered yet)
        if (_lastPlayedClip == null && defaultBGM != null)
        {
            PlayBGM(defaultBGM, defaultVolume, true);
        }

        Debug.Log($"🎵 BGM Zone Manager initialized with {musicZones.Count} zones");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-register triggers because colliders are scene objects
        RegisterZoneTriggers();

        // If we died and reloaded, resume the last song from the beginning
        if (_musicLockedBecauseDead)
        {
            _musicLockedBecauseDead = false;

            if (_lastPlayedClip != null)
            {
                // Start it immediately (no fade needed here)
                PlayBGM(_lastPlayedClip, _lastPlayedVolume, immediate: true);
                Debug.Log($"🎵 Restarted last BGM after death reload: {_lastPlayedClip.name}");
            }
            else if (defaultBGM != null)
            {
                PlayBGM(defaultBGM, defaultVolume, immediate: true);
                Debug.Log($"🎵 Restarted default BGM after death reload: {defaultBGM.name}");
            }
        }
    }

    void RegisterZoneTriggers()
    {
        foreach (var zone in musicZones)
        {
            if (zone.zoneTrigger != null)
            {
                var triggerHandler = zone.zoneTrigger.gameObject.GetComponent<MusicZoneTrigger>();
                if (triggerHandler == null)
                {
                    triggerHandler = zone.zoneTrigger.gameObject.AddComponent<MusicZoneTrigger>();
                }

                triggerHandler.Initialize(this, zone);

                zone.zoneTrigger.isTrigger = true;
                zone.zoneTrigger.enabled = zone.colliderActiveAtStart;
            }
        }
    }

    // ===== PUBLIC API FOR MANUAL TRIGGERS =====

    public void PlayZoneByName(string zoneName)
    {
        MusicZone zone = musicZones.Find(z => z.zoneName == zoneName);
        if (zone != null)
        {
            OnPlayerEnterZone(zone);
            Debug.Log($"🎵 Manual trigger: Playing {zoneName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Zone '{zoneName}' not found!");
        }
    }

    public void PlayZoneByIndex(int index)
    {
        if (index >= 0 && index < musicZones.Count)
        {
            OnPlayerEnterZone(musicZones[index]);
            Debug.Log($"🎵 Manual trigger: Playing zone {index} - {musicZones[index].zoneName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Zone index {index} out of range!");
        }
    }

    public void EnableZoneCollider(string zoneName)
    {
        MusicZone zone = musicZones.Find(z => z.zoneName == zoneName);
        if (zone != null && zone.zoneTrigger != null)
        {
            zone.zoneTrigger.enabled = true;
        }
    }

    public void DisableZoneCollider(string zoneName)
    {
        MusicZone zone = musicZones.Find(z => z.zoneName == zoneName);
        if (zone != null && zone.zoneTrigger != null)
        {
            zone.zoneTrigger.enabled = false;
        }
    }

    public void EnableZoneColliderByIndex(int index)
    {
        if (index >= 0 && index < musicZones.Count)
        {
            var zone = musicZones[index];
            if (zone.zoneTrigger != null)
            {
                zone.zoneTrigger.enabled = true;
            }
        }
    }

    public void DisableZoneColliderByIndex(int index)
    {
        if (index >= 0 && index < musicZones.Count)
        {
            var zone = musicZones[index];
            if (zone.zoneTrigger != null)
            {
                zone.zoneTrigger.enabled = false;
            }
        }
    }

    // ===== INTERNAL ZONE HANDLING =====

    public void OnPlayerEnterZone(MusicZone zone)
    {
        // ⭐ NEW: do not allow zone triggers to start music while dead
        if (_musicLockedBecauseDead)
            return;

        if (zone == null || zone.bgmClip == null) return;

        // Prevent restarting same music
        if (preventRestart && currentZone == zone && bgmAudioSource.isPlaying)
        {
            return;
        }

        currentZone = zone;

        // Remember last played track
        _lastPlayedClip = zone.bgmClip;
        _lastPlayedVolume = zone.volume;

        switch (transitionType)
        {
            case TransitionType.Fade:
                SwitchMusicWithFade(zone.bgmClip, zone.volume);
                break;

            case TransitionType.Crossfade:
                SwitchMusicWithCrossfade(zone.bgmClip, zone.volume);
                break;

            case TransitionType.Immediate:
                SwitchMusicImmediate(zone.bgmClip, zone.volume);
                break;
        }
    }

    // ===== TRANSITION METHODS =====

    void SwitchMusicImmediate(AudioClip newClip, float volume)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        bgmAudioSource.Stop();
        secondaryAudioSource.Stop();

        bgmAudioSource.clip = newClip;
        bgmAudioSource.volume = volume;
        bgmAudioSource.time = 0f;
        bgmAudioSource.Play();
    }

    void SwitchMusicWithFade(AudioClip newClip, float targetVolume)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(FadeTransition(newClip, targetVolume));
    }

    IEnumerator FadeTransition(AudioClip newClip, float targetVolume)
    {
        float startVolume = bgmAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2f));
            yield return null;
        }

        bgmAudioSource.Stop();
        bgmAudioSource.clip = newClip;
        bgmAudioSource.time = 0f;
        bgmAudioSource.Play();

        elapsed = 0f;
        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (fadeDuration / 2f));
            yield return null;
        }

        bgmAudioSource.volume = targetVolume;
        transitionCoroutine = null;
    }

    void SwitchMusicWithCrossfade(AudioClip newClip, float targetVolume)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CrossfadeTransition(newClip, targetVolume));
    }

    IEnumerator CrossfadeTransition(AudioClip newClip, float targetVolume)
    {
        secondaryAudioSource.clip = newClip;
        secondaryAudioSource.volume = 0f;
        secondaryAudioSource.time = 0f;
        secondaryAudioSource.Play();

        float elapsed = 0f;
        float startVolume = bgmAudioSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            secondaryAudioSource.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        bgmAudioSource.Stop();
        bgmAudioSource.clip = newClip;
        bgmAudioSource.volume = targetVolume;
        bgmAudioSource.time = 0f;
        bgmAudioSource.Play();

        secondaryAudioSource.Stop();
        secondaryAudioSource.volume = 0f;

        transitionCoroutine = null;
    }

    // ===== UTILITY METHODS =====

    public void PlayBGM(AudioClip clip, float volume = 0.7f, bool immediate = false)
    {
        if (clip == null) return;

        // Remember last played track
        _lastPlayedClip = clip;
        _lastPlayedVolume = volume;

        if (_musicLockedBecauseDead)
            return;

        if (immediate)
        {
            SwitchMusicImmediate(clip, volume);
        }
        else
        {
            SwitchMusicWithFade(clip, volume);
        }
    }

    public void StopAllMusic()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        bgmAudioSource.Stop();
        secondaryAudioSource.Stop();
        currentZone = null;
    }

    // ⭐ NEW: call this when player dies
    // Call this when player dies
    public void OnPlayerDied_StopMusic()
    {
        _musicLockedBecauseDead = true;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (bgmAudioSource != null) bgmAudioSource.Stop();
        if (secondaryAudioSource != null) secondaryAudioSource.Stop();
    }

    // Call this when player restarts/respawns (restart button)
    public void OnPlayerRestarted_PlayLastMusic()
    {
        _musicLockedBecauseDead = false;

        // If something was playing before, resume it (from start)
        if (_lastPlayedClip != null)
        {
            SwitchMusicImmediate(_lastPlayedClip, _lastPlayedVolume);
            return;
        }

        // Otherwise fallback to default
        if (defaultBGM != null)
        {
            SwitchMusicImmediate(defaultBGM, defaultVolume);
            return;
        }

        Debug.LogWarning("⚠️ No BGM to play on restart (no last track + no default).");
    }


    public void PauseMusic()
    {
        bgmAudioSource.Pause();
        secondaryAudioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (_musicLockedBecauseDead) return;
        bgmAudioSource.UnPause();
        secondaryAudioSource.UnPause();
    }

    public string GetCurrentZoneName()
    {
        return currentZone != null ? currentZone.zoneName : "None";
    }
}

/// <summary>
/// Helper component attached to zone trigger colliders
/// </summary>
public class MusicZoneTrigger : MonoBehaviour
{
    private BGMZoneManager manager;
    private BGMZoneManager.MusicZone zone;

    public void Initialize(BGMZoneManager bgmManager, BGMZoneManager.MusicZone musicZone)
    {
        manager = bgmManager;
        zone = musicZone;
    }

    void OnTriggerEnter(Collider other)
    {
        if (manager == null) return;

        if (other.CompareTag(manager.playerTag))
        {
            manager.OnPlayerEnterZone(zone);
        }
    }


}
