using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealthController : MonoBehaviour
{
    public static PlayerHealthController Instance;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float lowHealthThreshold = 30f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsFullHealth => Mathf.Approximately(CurrentHealth, maxHealth);
    public bool IsLowHealth => CurrentHealth <= lowHealthThreshold;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private bool deathFired = false;

    // =========================
    // VFX / Feedback
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

    [Header("Low Health Pulse Effect")]
    [SerializeField] private float lowHealthPulseIntensity = 0.5f;
    [Tooltip("How fast the low health effect pulses")]
    [SerializeField] private float lowHealthPulseSpeed = 1.5f;
    [Tooltip("Time to pause low health pulse when damage/heal occurs")]
    [SerializeField] private float effectInterruptDuration = 1.0f;

    private Coroutine lowHealthPulseCoroutine;
    private float lastEffectTime = -999f; // Track when last damage/heal occurred
    private GameObject activeHealParticleInstance; // Track active heal particles

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

        CurrentHealth = maxHealth;
        Notify();
    }

    private void Start()
    {
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
        if (Input.GetKeyDown(KeyCode.J))
        {
            Damage(15f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Damage(testDamageAmount);
            Debug.Log($"🎮 TEST: Pressed [1] - Damage {testDamageAmount}");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CurrentHealth = 25f;
            Notify();
            CheckLowHealthState();
            Debug.Log($"🎮 TEST: Pressed [2] - Set health to {CurrentHealth}");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Heal(20f);
            Debug.Log($"🎮 TEST: Pressed [K] - Heal 20");
        }
#endif
    }

    public void Damage(float amount)
    {
        if (deathFired) return;
        if (amount <= 0f) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        Notify();

        // Record time of effect to pause low health pulse
        lastEffectTime = Time.time;

        // IMPORTANT: Stop any active heal particles immediately
        StopHealParticles();

        // IMPORTANT: Stop any ongoing healing effect first
        EffectsController.StopHealing();

        // Trigger shader-based damage effect
        float damageIntensity = Mathf.Clamp01(amount / maxHealth);
        EffectsController.TriggerDamage(damageIntensity);

        // Camera shake
        TriggerDamageShake(amount);

        // Check if low health state changed
        CheckLowHealthState();

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (deathFired) return;
        if (amount <= 0f) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        Notify();

        // Record time of effect to pause low health pulse
        lastEffectTime = Time.time;

        CheckLowHealthState();

        // IMPORTANT: Stop any ongoing damage effect first
        EffectsController.StopDamage();

        // Trigger shader-based healing effect
        float healIntensity = Mathf.Clamp01(amount / maxHealth);
        EffectsController.TriggerHealing(healIntensity);

        PlayHealParticles();
    }

    public void ResetHealth()
    {
        deathFired = false;
        CurrentHealth = maxHealth;
        Notify();
        CheckLowHealthState();
    }

    private void Die()
    {
        if (deathFired) return;
        deathFired = true;

        // Stop low health pulse
        if (lowHealthPulseCoroutine != null)
        {
            StopCoroutine(lowHealthPulseCoroutine);
            lowHealthPulseCoroutine = null;
        }

        Debug.Log("💀 Player died");
        OnDeath?.Invoke();
    }

    private void Notify()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void CheckLowHealthState()
    {
        if (IsLowHealth && !IsDead)
        {
            // Start pulsing low health effect using shader
            if (lowHealthPulseCoroutine == null)
            {
                lowHealthPulseCoroutine = StartCoroutine(LowHealthPulseCoroutine());
                Debug.Log("⚠️ Low health pulse started!");
            }
        }
        else
        {
            // Stop pulsing
            if (lowHealthPulseCoroutine != null)
            {
                StopCoroutine(lowHealthPulseCoroutine);
                lowHealthPulseCoroutine = null;
                Debug.Log("✅ Low health pulse stopped!");
            }
        }
    }

    private IEnumerator LowHealthPulseCoroutine()
    {
        while (IsLowHealth && !IsDead)
        {
            // Check if we should pause pulsing due to recent damage/heal effect
            float timeSinceLastEffect = Time.time - lastEffectTime;

            if (timeSinceLastEffect >= effectInterruptDuration)
            {
                // Calculate pulsing intensity
                float pingPong = Mathf.PingPong(Time.time * lowHealthPulseSpeed, 1f);
                float intensity = Mathf.Lerp(0.3f, lowHealthPulseIntensity, pingPong);

                // Trigger damage effect continuously for pulse
                EffectsController.TriggerDamage(intensity);
            }
            // else: Skip triggering pulse effect, let damage/heal effect play out

            // Wait for next frame
            yield return new WaitForSeconds(0.1f);
        }

        lowHealthPulseCoroutine = null;
    }

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

        // Stop any existing heal particles first
        StopHealParticles();

        Transform spawnParent = playerTransform != null ? playerTransform : transform;

        activeHealParticleInstance = Instantiate(healParticlePrefab, spawnParent);
        activeHealParticleInstance.transform.localPosition = particleOffset;
        activeHealParticleInstance.transform.localRotation = Quaternion.identity;

        ParticleSystem[] particleSystems = activeHealParticleInstance.GetComponentsInChildren<ParticleSystem>();

        if (particleSystems.Length > 0)
        {
            foreach (ParticleSystem ps in particleSystems)
            {
                var mainModule = ps.main;
                mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
                ps.Play();
            }

            float maxDuration = 0f;
            foreach (ParticleSystem ps in particleSystems)
            {
                float psDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (psDuration > maxDuration)
                    maxDuration = psDuration;
            }

            Destroy(activeHealParticleInstance, maxDuration);
        }
        else
        {
            Debug.LogWarning("⚠️ No ParticleSystem found in the prefab!");
            Destroy(activeHealParticleInstance, healParticleDuration);
        }
    }

    private void StopHealParticles()
    {
        if (activeHealParticleInstance != null)
        {
            // Stop all particle systems immediately
            ParticleSystem[] particleSystems = activeHealParticleInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // Destroy the game object
            Destroy(activeHealParticleInstance);
            activeHealParticleInstance = null;
        }
    }
}