using System;
using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    public static PlayerHealthController Instance;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsFullHealth => Mathf.Approximately(CurrentHealth, maxHealth);

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private bool deathFired = false;

    // =========================
    // VFX / Feedback (from PlayerHealth)
    // =========================
    [Header("Heal Particle System")]
    [SerializeField] private GameObject healParticlePrefab;
    [SerializeField] private Transform playerTransform;
    [Tooltip("Offset from player position (Y=0 means at player's feet/ground)")]
    [SerializeField] private Vector3 particleOffset = new Vector3(0, 0f, 0);
    [SerializeField] private float healParticleDuration = 0.5f;

    [Header("Damage Camera Shake")]
    [SerializeField] private float damageShakeDuration = 0.4f;
    [SerializeField] private float lightDamageShakeMagnitude = 0.15f;
    [SerializeField] private float heavyDamageShakeMagnitude = 0.35f;

    // Optional debug (kept as non-invasive, won’t change your core logic)
    [Header("Testing (Optional)")]
    [SerializeField] private float testDamageAmount = 10f;
    

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If you want health to persist, keep this.
        // If you want "new run" health per fresh play session, reset elsewhere.
        CurrentHealth = maxHealth;
        Notify();
    }

    private void Start()
    {
        // Keep the inspector fields from your old PlayerHealth script.
        // Auto-assign playerTransform if not set.
        if (playerTransform == null)
        {
            playerTransform = transform;
            Debug.Log("🔧 Player Transform auto-assigned to this GameObject");
        }

        if (healParticlePrefab == null)
            Debug.LogWarning("⚠️ Heal Particle Prefab not assigned in Inspector!");
    }

    private void Update()
    {
#if UNITY_EDITOR
        // DEBUG: test damage (existing debug)
        if (Input.GetKeyDown(KeyCode.J))
        {
            Damage(15f);
        }

        // Optional VFX testing (doesn't change core logic)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Damage(testDamageAmount);
            Debug.Log($"🎮 TEST: Pressed [1] - Damage {testDamageAmount}");
        }

       
#endif
    }

    public void Damage(float amount)
    {
        if (deathFired) return;
        if (amount <= 0f) return;

        // ---- core logic preserved ----
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        Notify();
        // ---- end core logic ----

        // VFX/feedback (added)
        TriggerDamageShake(amount);

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (deathFired) return;
        if (amount <= 0f) return;

        // ---- core logic preserved ----
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        Notify();
        // ---- end core logic ----

        // VFX/feedback (added)
        PlayHealParticles();
    }

    /// <summary>
    /// Call this when restarting the level so you don't reload with 0 HP
    /// (since this controller persists with DontDestroyOnLoad).
    /// </summary>
    public void ResetHealth()
    {
        deathFired = false;
        CurrentHealth = maxHealth;
        Notify();
    }

    private void Die()
    {
        if (deathFired) return;
        deathFired = true;

        Debug.Log("Player died");
        OnDeath?.Invoke();
    }

    private void Notify()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    // =========================
    // VFX Helpers
    // =========================

    private void TriggerDamageShake(float damageAmount)
    {
        if (CameraNewShake.Instance == null)
        {
            Debug.LogWarning("⚠️ CameraNewShake.Instance not found!");
            return;
        }

        float damagePercentage = damageAmount / maxHealth;
        float shakeMagnitude = (damagePercentage >= 0.2f) ? heavyDamageShakeMagnitude : lightDamageShakeMagnitude;

        CameraNewShake.Instance.Shake(duration: damageShakeDuration, magnitude: shakeMagnitude);
    }

    private void PlayHealParticles()
    {
        if (healParticlePrefab == null)
        {
            Debug.LogWarning("⚠️ Heal Particle Prefab not assigned!");
            return;
        }

        Transform spawnParent = playerTransform != null ? playerTransform : transform;

        // Instantiate as child of player
        GameObject particleInstance = Instantiate(healParticlePrefab, spawnParent);

        // Set local position relative to parent
        particleInstance.transform.localPosition = particleOffset;
        particleInstance.transform.localRotation = Quaternion.identity;

        // Get all particle systems in the prefab (in case there are multiple)
        ParticleSystem[] particleSystems = particleInstance.GetComponentsInChildren<ParticleSystem>();

        if (particleSystems.Length > 0)
        {
            foreach (ParticleSystem ps in particleSystems)
            {
                // CRITICAL: Set simulation space to Local so particles follow the parent
                var mainModule = ps.main;
                mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;

                ps.Play();
            }

            // Auto-destroy after the longest particle system finishes
            float maxDuration = 0f;
            foreach (ParticleSystem ps in particleSystems)
            {
                float psDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (psDuration > maxDuration)
                    maxDuration = psDuration;
            }

            Destroy(particleInstance, maxDuration);
        }
        else
        {
            Debug.LogWarning("⚠️ No ParticleSystem found in the prefab!");
            Destroy(particleInstance, healParticleDuration);
        }
    }
}
