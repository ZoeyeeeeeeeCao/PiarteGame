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

    [Header("Damage Post-Processing")]
    [SerializeField] private float damageEffectDuration = 0.5f;
    [SerializeField] private float maxVignetteIntensity = 0.45f;
    [SerializeField] private Color damageColor = new Color(0.8f, 0f, 0f, 1f);

    [Header("Low Health Pulse Effect")]
    [SerializeField] private float lowHealthPulseSpeed = 2f;
    [SerializeField] private float lowHealthMinIntensity = 0.25f;
    [SerializeField] private float lowHealthMaxIntensity = 0.5f;
    [SerializeField] private Color lowHealthColor = new Color(0.6f, 0f, 0f, 1f);

    private Volume globalVolume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Coroutine damageEffectCoroutine;
    private Coroutine lowHealthPulseCoroutine;

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

        SetupPostProcessing();
    }

    private void SetupPostProcessing()
    {
        Volume[] volumes = FindObjectsOfType<Volume>();

        foreach (Volume vol in volumes)
        {
            if (vol.isGlobal)
            {
                globalVolume = vol;
                Debug.Log($"✅ Found existing Global Volume: {vol.gameObject.name}");
                break;
            }
        }

        if (globalVolume == null && volumes.Length > 0)
        {
            globalVolume = volumes[0];
            Debug.Log($"✅ Using existing Volume: {globalVolume.gameObject.name}");
        }

        if (globalVolume == null)
        {
            Debug.LogWarning("⚠️ No Volume found in scene. Creating one automatically...");
            GameObject volumeObj = new GameObject("Global Volume (Auto-Created)");
            globalVolume = volumeObj.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 1;
            globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Debug.Log("✅ Global Volume created automatically!");
        }

        if (globalVolume.profile == null)
        {
            globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Debug.Log("✅ Volume Profile created!");
        }

        if (!globalVolume.profile.TryGet(out vignette))
        {
            vignette = globalVolume.profile.Add<Vignette>(false);
            Debug.Log("✅ Vignette added to Volume Profile");
        }

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.4f;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;

        if (!globalVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = globalVolume.profile.Add<ColorAdjustments>(false);
            Debug.Log("✅ Color Adjustments added to Volume Profile");
        }

        colorAdjustments.active = true;
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = Color.white;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 0f;

        Debug.Log($"✅ Post-Processing setup complete!");
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

        // Call EffectsController for fullscreen damage effect
        float damageIntensity = Mathf.Clamp01(amount / maxHealth);
        EffectsController.TriggerDamage(damageIntensity);

        // Original effects
        TriggerDamageShake(amount);
        TriggerDamagePostProcess(amount);
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
        CheckLowHealthState();

        // Call EffectsController for fullscreen healing effect
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
            if (lowHealthPulseCoroutine == null)
            {
                lowHealthPulseCoroutine = StartCoroutine(LowHealthPulseCoroutine());
                Debug.Log("⚠️ Low health pulse started!");
            }
        }
        else
        {
            if (lowHealthPulseCoroutine != null)
            {
                StopCoroutine(lowHealthPulseCoroutine);
                lowHealthPulseCoroutine = null;

                if (vignette != null) vignette.intensity.value = 0f;
                if (colorAdjustments != null)
                {
                    colorAdjustments.colorFilter.value = Color.white;
                    colorAdjustments.saturation.value = 0f;
                    colorAdjustments.contrast.value = 0f;
                }
                if (filmGrain != null) filmGrain.intensity.value = 0f;
                if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
                if (lensDistortion != null) lensDistortion.intensity.value = 0f;

                Debug.Log("✅ Low health pulse stopped!");
            }
        }
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

    private void TriggerDamagePostProcess(float damageAmount)
    {
        if (globalVolume == null || vignette == null || colorAdjustments == null)
        {
            Debug.LogWarning("⚠️ Post-processing components not ready!");
            return;
        }

        if (damageEffectCoroutine != null)
            StopCoroutine(damageEffectCoroutine);

        float damagePercentage = damageAmount / maxHealth;
        float targetIntensity = Mathf.Clamp(damagePercentage * maxVignetteIntensity * 2f, 0.25f, maxVignetteIntensity);

        damageEffectCoroutine = StartCoroutine(DamageEffectCoroutine(targetIntensity));
    }

    private IEnumerator DamageEffectCoroutine(float targetIntensity)
    {
        float elapsed = 0f;
        float fadeInDuration = 0.08f;

        float targetGrain = targetIntensity * 1.2f;
        float targetChromatic = targetIntensity * 0.8f;
        float targetDistortion = -0.2f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, t);
            colorAdjustments.colorFilter.value = Color.Lerp(Color.white, damageColor, t * 0.5f);
            colorAdjustments.saturation.value = Mathf.Lerp(0f, -20f, t * 0.4f);
            colorAdjustments.contrast.value = Mathf.Lerp(0f, 10f, t * 0.3f);

            if (filmGrain != null) filmGrain.intensity.value = Mathf.Lerp(0f, targetGrain, t);
            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(0f, targetChromatic, t);
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(0f, targetDistortion, t);

            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        float fadeOutDuration = damageEffectDuration;
        float startIntensity = vignette.intensity.value;
        Color startColor = colorAdjustments.colorFilter.value;
        float startSaturation = colorAdjustments.saturation.value;
        float startContrast = colorAdjustments.contrast.value;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            if (!IsLowHealth)
            {
                vignette.intensity.value = Mathf.Lerp(startIntensity, 0f, t);
                colorAdjustments.colorFilter.value = Color.Lerp(startColor, Color.white, t);
                colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, 0f, t);
                colorAdjustments.contrast.value = Mathf.Lerp(startContrast, 0f, t);
            }
            else
            {
                vignette.intensity.value = Mathf.Lerp(startIntensity, lowHealthMinIntensity, t);
                colorAdjustments.colorFilter.value = Color.Lerp(startColor, lowHealthColor, t);
                colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, -15f, t);
                colorAdjustments.contrast.value = Mathf.Lerp(startContrast, 5f, t);
            }

            yield return null;
        }

        if (!IsLowHealth)
        {
            vignette.intensity.value = 0f;
            colorAdjustments.colorFilter.value = Color.white;
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.contrast.value = 0f;
        }

        damageEffectCoroutine = null;
    }

    private IEnumerator LowHealthPulseCoroutine()
    {
        while (IsLowHealth && !IsDead)
        {
            float pingPong = Mathf.PingPong(Time.time * lowHealthPulseSpeed, 1f);

            vignette.intensity.value = Mathf.Lerp(lowHealthMinIntensity, lowHealthMaxIntensity, pingPong);
            colorAdjustments.colorFilter.value = Color.Lerp(lowHealthColor, damageColor, pingPong * 0.5f);
            colorAdjustments.saturation.value = Mathf.Lerp(-10f, -20f, pingPong * 0.5f);

            yield return null;
        }

        vignette.intensity.value = 0f;
        colorAdjustments.colorFilter.value = Color.white;
        colorAdjustments.saturation.value = 0f;
    }

    private void PlayHealParticles()
    {
        if (healParticlePrefab == null)
        {
            Debug.LogWarning("⚠️ Heal Particle Prefab not assigned!");
            return;
        }

        Transform spawnParent = playerTransform != null ? playerTransform : transform;

        GameObject particleInstance = Instantiate(healParticlePrefab, spawnParent);
        particleInstance.transform.localPosition = particleOffset;
        particleInstance.transform.localRotation = Quaternion.identity;

        ParticleSystem[] particleSystems = particleInstance.GetComponentsInChildren<ParticleSystem>();

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

            Destroy(particleInstance, maxDuration);
        }
        else
        {
            Debug.LogWarning("⚠️ No ParticleSystem found in the prefab!");
            Destroy(particleInstance, healParticleDuration);
        }
    }
}