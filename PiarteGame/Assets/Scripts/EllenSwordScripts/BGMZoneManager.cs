using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // Play default BGM if assigned
        if (defaultBGM != null)
        {
            PlayBGM(defaultBGM, defaultVolume, true);
        }

        Debug.Log($"🎵 BGM Zone Manager initialized with {musicZones.Count} zones");
    }

    void RegisterZoneTriggers()
    {
        foreach (var zone in musicZones)
        {
            if (zone.zoneTrigger != null)
            {
                // Add MusicZoneTrigger component to handle collision
                var triggerHandler = zone.zoneTrigger.gameObject.GetComponent<MusicZoneTrigger>();
                if (triggerHandler == null)
                {
                    triggerHandler = zone.zoneTrigger.gameObject.AddComponent<MusicZoneTrigger>();
                }

                triggerHandler.Initialize(this, zone);

                // Ensure it's a trigger
                zone.zoneTrigger.isTrigger = true;

                // Set initial active state
                zone.zoneTrigger.enabled = zone.colliderActiveAtStart;

                Debug.Log($"🎵 Registered zone: {zone.zoneName} (Collider {(zone.colliderActiveAtStart ? "Active" : "Inactive")})");
            }
            else
            {
                Debug.Log($"🎵 Zone '{zone.zoneName}' has no collider (manual trigger only)");
            }
        }
    }

    // ===== PUBLIC API FOR MANUAL TRIGGERS =====

    /// <summary>
    /// Play music from a specific zone by name (for dialogue events, cutscenes, etc.)
    /// </summary>
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

    /// <summary>
    /// Play music from a specific zone by index
    /// </summary>
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

    /// <summary>
    /// Enable a zone's collider trigger
    /// </summary>
    public void EnableZoneCollider(string zoneName)
    {
        MusicZone zone = musicZones.Find(z => z.zoneName == zoneName);
        if (zone != null && zone.zoneTrigger != null)
        {
            zone.zoneTrigger.enabled = true;
            Debug.Log($"✅ Enabled collider for zone: {zoneName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Cannot enable collider for zone '{zoneName}'");
        }
    }

    /// <summary>
    /// Disable a zone's collider trigger
    /// </summary>
    public void DisableZoneCollider(string zoneName)
    {
        MusicZone zone = musicZones.Find(z => z.zoneName == zoneName);
        if (zone != null && zone.zoneTrigger != null)
        {
            zone.zoneTrigger.enabled = false;
            Debug.Log($"❌ Disabled collider for zone: {zoneName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Cannot disable collider for zone '{zoneName}'");
        }
    }

    /// <summary>
    /// Enable zone collider by index
    /// </summary>
    public void EnableZoneColliderByIndex(int index)
    {
        if (index >= 0 && index < musicZones.Count)
        {
            var zone = musicZones[index];
            if (zone.zoneTrigger != null)
            {
                zone.zoneTrigger.enabled = true;
                Debug.Log($"✅ Enabled collider for zone {index}: {zone.zoneName}");
            }
        }
    }

    /// <summary>
    /// Disable zone collider by index
    /// </summary>
    public void DisableZoneColliderByIndex(int index)
    {
        if (index >= 0 && index < musicZones.Count)
        {
            var zone = musicZones[index];
            if (zone.zoneTrigger != null)
            {
                zone.zoneTrigger.enabled = false;
                Debug.Log($"❌ Disabled collider for zone {index}: {zone.zoneName}");
            }
        }
    }

    // ===== INTERNAL ZONE HANDLING =====

    /// <summary>
    /// Called when player enters a music zone (from collider OR manual trigger)
    /// </summary>
    public void OnPlayerEnterZone(MusicZone zone)
    {
        // Prevent restarting same music
        if (preventRestart && currentZone == zone && bgmAudioSource.isPlaying)
        {
            Debug.Log($"🎵 Already playing {zone.zoneName}, skipping transition");
            return;
        }

        Debug.Log($"🎵 Entering zone: {zone.zoneName}");
        currentZone = zone;

        // Switch music based on transition type
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
        bgmAudioSource.clip = newClip;
        bgmAudioSource.volume = volume;
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

        // Fade out
        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2f));
            yield return null;
        }

        // Switch track
        bgmAudioSource.Stop();
        bgmAudioSource.clip = newClip;
        bgmAudioSource.Play();

        // Fade in
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
        bgmAudioSource.time = secondaryAudioSource.time;
        bgmAudioSource.Play();

        secondaryAudioSource.Stop();
        secondaryAudioSource.volume = 0f;

        transitionCoroutine = null;
    }

    // ===== UTILITY METHODS =====

    public void PlayBGM(AudioClip clip, float volume = 0.7f, bool immediate = false)
    {
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

    public void PauseMusic()
    {
        bgmAudioSource.Pause();
        secondaryAudioSource.Pause();
    }

    public void ResumeMusic()
    {
        bgmAudioSource.UnPause();
        secondaryAudioSource.UnPause();
    }

    /// <summary>
    /// Get the name of the currently playing zone
    /// </summary>
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
        if (other.CompareTag(manager.playerTag))
        {
            manager.OnPlayerEnterZone(zone);
        }
    }
}