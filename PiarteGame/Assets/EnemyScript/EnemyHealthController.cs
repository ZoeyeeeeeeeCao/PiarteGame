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
    // New event: Passes the normalized health percentage (0.0 to 1.0)
    public UnityEvent<float> OnHealthChanged;

    public static event Action<EnemyHealthController> EnemyDied;
    public static event Action<int> OnEnemyCountUpdated;

    private static int globalDeathCount = 0;
    private bool isDead = false;

    public static void ResetDeathCount()
    {
        globalDeathCount = 0;
        OnEnemyCountUpdated?.Invoke(globalDeathCount);
    }

    public static void BroadcastCurrentCount()
    {
        OnEnemyCountUpdated?.Invoke(globalDeathCount);
    }

    private void Awake()
    {
        Debug.Log($"{gameObject.name} Health Controller is waking up!");
        this.enabled = true;
        currentHealth = maxHealth;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Initial health broadcast
        OnHealthChanged?.Invoke(GetHealthPercentage());
    }

    private void Update()
    {
        if (enableDebugKeys && !isDead && Input.GetKeyDown(KeyCode.T))
        {
            ApplyDamage(damagePerHit);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag(damageTag))
        {
            ApplyDamage(damagePerHit);
        }
    }

    public void ApplyDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Notify any listeners (like the Boss Bar) that health has changed
        OnHealthChanged?.Invoke(GetHealthPercentage());

        if (currentHealth <= 0)
        {
            HandleDeath();
        }
        else
        {
            PlayHurtSound();
            OnTakeDamage?.Invoke();
        }
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        globalDeathCount++;

        PlayDeathSounds();
        OnDeath?.Invoke();

        EnemyDied?.Invoke(this);
        OnEnemyCountUpdated?.Invoke(globalDeathCount);

        Debug.Log($"[EnemyHealthController] {gameObject.name} Died. Global Count: {globalDeathCount}");
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