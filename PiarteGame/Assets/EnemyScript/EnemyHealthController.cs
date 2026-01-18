using UnityEngine;
using UnityEngine.Events;
using System;

public class EnemyHealthController : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Detection Settings")]
    [SerializeField] private string damageTag = "EnemyDamage";
    [SerializeField] private float damagePerHit = 10f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private float hurtVolume = 1f;
    [SerializeField] private AudioClip deathSoundA;
    [SerializeField] private AudioClip deathSoundB;
    [SerializeField] private float deathVolumeA = 1f;
    [SerializeField] private float deathVolumeB = 1f;
    [SerializeField] private float deathSoundBDelay = 0.1f;

    [Header("Testing")]
    [SerializeField] private bool enableDebugKeys = true;

    [Header("Events")]
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;

    public static event Action<EnemyHealthController> EnemyDied;
    public static event Action<int> OnEnemyCountUpdated;

    private static int globalDeathCount = 0;
    private bool isDead = false;

    /// <summary>
    /// Resets the counter and immediately notifies listeners (like the UI).
    /// </summary>
    public static void ResetDeathCount()
    {
        globalDeathCount = 0;
        OnEnemyCountUpdated?.Invoke(globalDeathCount);
    }

    /// <summary>
    /// Forces the current count to be sent to all listeners.
    /// Useful for refreshing UI when a script first enables.
    /// </summary>
    public static void BroadcastCurrentCount()
    {
        OnEnemyCountUpdated?.Invoke(globalDeathCount);
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        if (enableDebugKeys && !isDead && Input.GetKeyDown(KeyCode.T))
            ApplyDamage(damagePerHit);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (other.CompareTag(damageTag)) ApplyDamage(damagePerHit);
    }

    public void ApplyDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
            HandleDeath();
        else
        {
            PlayHurtSound();
            OnTakeDamage?.Invoke();
        }
    }

    private void HandleDeath()
    {
        isDead = true;
        globalDeathCount++; // Increment count FIRST

        PlayDeathSounds();
        OnDeath?.Invoke();

        // Notify Listeners
        EnemyDied?.Invoke(this);
        OnEnemyCountUpdated?.Invoke(globalDeathCount); // Trigger Update SECOND

        Debug.Log($"[EnemyHealthController] Died. Global Count: {globalDeathCount}");
    }

    private void PlayHurtSound()
    {
        if (hurtSounds == null || hurtSounds.Length == 0) return;
        AudioClip clip = hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)];
        audioSource.PlayOneShot(clip, hurtVolume);
    }

    private void PlayDeathSounds()
    {
        if (deathSoundA != null) audioSource.PlayOneShot(deathSoundA, deathVolumeA);
        if (deathSoundB != null) Invoke(nameof(PlayDeathSoundB), deathSoundBDelay);
    }

    private void PlayDeathSoundB() => audioSource.PlayOneShot(deathSoundB, deathVolumeB);

    public float GetHealth() => currentHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
}