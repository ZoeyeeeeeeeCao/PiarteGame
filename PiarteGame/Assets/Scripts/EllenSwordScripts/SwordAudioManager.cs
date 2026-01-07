using UnityEngine;

/// <summary>
/// Manages all sword-related audio effects
/// Attach this to your sword prefab or player
/// </summary>
public class SwordAudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Draw/Sheath Sounds")]
    [Tooltip("Sound when drawing the sword")]
    [SerializeField] private AudioClip drawSwordSound;
    [Tooltip("Sound when sheathing the sword")]
    [SerializeField] private AudioClip sheathSwordSound;
    [SerializeField] private float drawSheathVolume = 0.7f;

    [Header("Swing Sounds (Wind Swoosh)")]
    [Tooltip("Array of wind/swoosh sounds for sword swings")]
    [SerializeField] private AudioClip[] swingSounds;
    [SerializeField] private float swingVolume = 0.2f; // Changed from 0.5f to 0.2f (20% volume)
    [SerializeField] private float swingPitchMin = 0.9f;
    [SerializeField] private float swingPitchMax = 1.1f;

    [Header("Hit Impact Sounds")]
    [Tooltip("Array of impact sounds when hitting enemies")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private float hitVolume = 0.8f;
    [SerializeField] private float hitPitchMin = 0.95f;
    [SerializeField] private float hitPitchMax = 1.05f;

    [Header("Hard Attack Sound")]
    [Tooltip("Special sound for hard attacks (optional)")]
    [SerializeField] private AudioClip hardAttackSwingSound;
    [SerializeField] private float hardAttackVolume = 0.9f;

    [Header("Audio Settings")]
    [SerializeField] private bool use3DSound = true;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 15f;

    private void Start()
    {
        InitializeAudioSource();
    }

    private void InitializeAudioSource()
    {
        Debug.Log("🔊 Initializing SwordAudioManager...");

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("✅ Created AudioSource component automatically");
            }
        }

        // Configure audio source
        audioSource.enabled = true; // CRITICAL: Enable the AudioSource!
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = use3DSound ? 1f : 0f;

        if (use3DSound)
        {
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        Debug.Log($"✅ Sword Audio Manager initialized on {gameObject.name}");
        Debug.Log($"   - Draw Sound: {(drawSwordSound != null ? drawSwordSound.name : "NOT ASSIGNED")}");
        Debug.Log($"   - Sheath Sound: {(sheathSwordSound != null ? sheathSwordSound.name : "NOT ASSIGNED")}");
        Debug.Log($"   - Swing Sounds: {(swingSounds != null ? swingSounds.Length.ToString() : "NOT ASSIGNED")}");
        Debug.Log($"   - Hit Sounds: {(hitSounds != null ? hitSounds.Length.ToString() : "NOT ASSIGNED")}");
    }

    /// <summary>
    /// Play draw sword sound
    /// </summary>
    public void PlayDrawSound()
    {
        Debug.Log("🔊 PlayDrawSound() called!");

        if (audioSource == null)
        {
            Debug.LogError("❌ AudioSource is NULL! Cannot play draw sound.");
            return;
        }

        if (drawSwordSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(drawSwordSound, drawSheathVolume);
            Debug.Log($"✅ Playing draw sword sound: {drawSwordSound.name} at volume {drawSheathVolume}");
        }
        else
        {
            Debug.LogWarning("⚠️ Draw sword sound not assigned in Inspector!");
        }
    }

    /// <summary>
    /// Play sheath sword sound
    /// </summary>
    public void PlaySheathSound()
    {
        if (sheathSwordSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(sheathSwordSound, drawSheathVolume);
            Debug.Log("🔊 Playing sheath sword sound");
        }
        else
        {
            Debug.LogWarning("⚠️ Sheath sword sound not assigned!");
        }
    }

    /// <summary>
    /// Play random swing sound (wind swoosh)
    /// </summary>
    public void PlaySwingSound(bool isHardAttack = false)
    {
        // Use special hard attack sound if available
        if (isHardAttack && hardAttackSwingSound != null)
        {
            audioSource.pitch = Random.Range(swingPitchMin, swingPitchMax);
            audioSource.PlayOneShot(hardAttackSwingSound, hardAttackVolume);
            Debug.Log("🔊 Playing HARD attack swing sound");
            return;
        }

        // Otherwise play random swing sound
        if (swingSounds != null && swingSounds.Length > 0)
        {
            AudioClip randomSwing = GetRandomClip(swingSounds);
            if (randomSwing != null)
            {
                audioSource.pitch = Random.Range(swingPitchMin, swingPitchMax);
                audioSource.PlayOneShot(randomSwing, swingVolume);
                Debug.Log("🔊 Playing swing sound");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No swing sounds assigned!");
        }
    }

    /// <summary>
    /// Play random hit impact sound
    /// </summary>
    public void PlayHitSound()
    {
        if (hitSounds != null && hitSounds.Length > 0)
        {
            AudioClip randomHit = GetRandomClip(hitSounds);
            if (randomHit != null)
            {
                audioSource.pitch = Random.Range(hitPitchMin, hitPitchMax);
                audioSource.PlayOneShot(randomHit, hitVolume);
                Debug.Log("🔊 Playing hit impact sound");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No hit sounds assigned!");
        }
    }

    /// <summary>
    /// Play a specific sound with custom volume and pitch
    /// </summary>
    public void PlayCustomSound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Get a random clip from an array, avoiding repeats if possible
    /// </summary>
    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;

        // Simple random selection
        int randomIndex = Random.Range(0, clips.Length);
        return clips[randomIndex];
    }

    /// <summary>
    /// Stop all currently playing sounds
    /// </summary>
    public void StopAllSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // Public getters for debugging
    public bool HasDrawSound => drawSwordSound != null;
    public bool HasSheathSound => sheathSwordSound != null;
    public bool HasSwingSounds => swingSounds != null && swingSounds.Length > 0;
    public bool HasHitSounds => hitSounds != null && hitSounds.Length > 0;
}