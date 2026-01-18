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
        // Debug to confirm script started
        Debug.Log($"{gameObject.name} Health Controller is waking up!");

        // Ensure the script is actually enabled at runtime
        this.enabled = true;

        // Fix: Ensure currentHealth is not 0 or negative at start
        currentHealth = maxHealth;

        // Safety check for AudioSource to prevent NullReferenceException
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

        // Debug check for tag issues
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

        // Instead of disabling the script, we just stop movement or logic here
        // If you want the object to vanish, uncomment the line below:
        // Destroy(gameObject, 1.5f);
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