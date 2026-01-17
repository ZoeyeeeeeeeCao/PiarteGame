using UnityEngine;
using UnityEngine.Events;

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
    [Tooltip("Random hurt/groan sounds played when enemy takes damage")]
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private float hurtVolume = 1f;
    [Tooltip("First death sound (plays immediately when health reaches 0)")]
    [SerializeField] private AudioClip deathSoundA;
    [Tooltip("Second death sound (plays right after deathSoundA)")]
    [SerializeField] private AudioClip deathSoundB;
    [SerializeField] private float deathVolumeA = 1f;
    [SerializeField] private float deathVolumeB = 1f;
    [SerializeField] private float deathSoundBDelay = 0.1f;

    [Header("Testing")]
    [Tooltip("If enabled, pressing T will deal damage to this enemy for testing purposes.")]
    [SerializeField] private bool enableDebugKeys = true;

    [Header("Events")]
    [Tooltip("Triggered when health is reduced but remains above 0.")]
    public UnityEvent OnTakeDamage;

    [Tooltip("Triggered when health reaches 0.")]
    public UnityEvent OnDeath;

    private bool isDead = false;

    private void Awake()
    {
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

        if (currentHealth <= 0)
        {
            isDead = true;
            PlayDeathSounds();
            OnDeath?.Invoke();
        }
        else
        {
            PlayHurtSound();
            OnTakeDamage?.Invoke();
        }
    }

    private void PlayHurtSound()
    {
        if (hurtSounds == null || hurtSounds.Length == 0) return;

        int randomIndex = Random.Range(0, hurtSounds.Length);
        AudioClip randomHurtSound = hurtSounds[randomIndex];

        if (randomHurtSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(randomHurtSound, hurtVolume);
        }
    }

    private void PlayDeathSounds()
    {
        if (audioSource == null) return;

        if (deathSoundA != null)
        {
            audioSource.PlayOneShot(deathSoundA, deathVolumeA);
        }

        if (deathSoundB != null)
        {
            Invoke(nameof(PlayDeathSoundB), deathSoundBDelay);
        }
    }

    private void PlayDeathSoundB()
    {
        if (deathSoundB != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSoundB, deathVolumeB);
        }
    }

    public float GetHealth() => currentHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
}