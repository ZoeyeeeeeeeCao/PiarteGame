using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls full-screen damage and healing effects by managing URP Renderer Features.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EffectsController : MonoBehaviour
{
    public static EffectsController instance;

    [Header("URP Renderer Data")]
    [Tooltip("Assign your main URP Renderer asset here (e.g., ForwardRenderer).")]
    public ScriptableRendererData rendererData;

    [Header("Effect Materials")]
    public Material damageMaterial;
    public Material healingMaterial;

    [Header("Damage Overlays")]
    [Tooltip("Assign multiple blood/scratch textures here.")]
    public Texture2D[] damageOverlayTextures;

    [Header("Healing Overlays")]
    [Tooltip("Assign multiple healing textures (runes, hex patterns, etc.).")]
    public Texture2D[] healingOverlayTextures;

    [Header("Audio Settings")]
    [Tooltip("Sound that plays when healing.")]
    public AudioClip healingSound;
    [Tooltip("A list of sounds that can play randomly when the player takes damage.")]
    public List<AudioClip> damageSounds = new List<AudioClip>();

    private AudioSource effectsAudioSource;
    private ScriptableRendererFeature damageFeature;
    private ScriptableRendererFeature healingFeature;
    private Coroutine damageCoroutine;
    private Coroutine healingCoroutine;

    // Shader Property IDs for Shader Graph
    private static readonly int VignetteRadiusID = Shader.PropertyToID("_VignetteRadius");
    private static readonly int VignetteSmoothnessID = Shader.PropertyToID("_VignetteSmoothness");
    private static readonly int VignetteDarkeningID = Shader.PropertyToID("_VignetteDarkening");
    private static readonly int DamageColorID = Shader.PropertyToID("_DamageColor");
    private static readonly int OverlayOpacityID = Shader.PropertyToID("_OverlayOpacity");
    private static readonly int DamageOverlayTexID = Shader.PropertyToID("_MainTex");
    private static readonly int ScratchesIntensityID = Shader.PropertyToID("_ScratchesIntensity");

    // Healing shader properties
    private static readonly int HealOpacityID = Shader.PropertyToID("_HealOpacity");
    private static readonly int HealTextureID = Shader.PropertyToID("_HealTexture");
    private static readonly int HealIntensityID = Shader.PropertyToID("_Intensity");
    private static readonly int HealColorID = Shader.PropertyToID("_HealColor");

    // Effect Strength Values for Shader Graph
    private const float DAMAGE_RADIUS_NONE = 0.0f;
    private const float DAMAGE_RADIUS_MAX = 0.8f;
    private const float DAMAGE_SMOOTHNESS = 0.3f;
    private const float DAMAGE_DARKENING_MAX = 0.7f;

    // FIXED: Swapped healing radius values - now goes from small to large
    private const float HEALING_RADIUS_NONE = 0.0f;   // No effect (was 15f)
    private const float HEALING_RADIUS_MAX = 0.8f;    // Full effect (was 3f)
    private const float HEALING_SMOOTHNESS = 3f;    // Add smoothness control

    [Header("Tester Settings")]
    [Range(0f, 1f)]
    public float testDamageIntensity = 0.8f;
    [Range(0f, 1f)]
    public float testHealingIntensity = 1.0f;

    [Header("Debug")]
    public bool debugMode = true;

    public void Awake()
    {
        instance = this;
        effectsAudioSource = GetComponent<AudioSource>();

        // Validate setup
        ValidateSetup();

        // Find renderer features
        damageFeature = FindRendererFeature("FullScreenPassDamage");
        healingFeature = FindRendererFeature("FullScreenPassHealing");

        // Initialize materials to clean state
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        if (damageMaterial != null)
        {
            damageMaterial.SetFloat(VignetteRadiusID, DAMAGE_RADIUS_NONE);
            damageMaterial.SetFloat(VignetteSmoothnessID, DAMAGE_SMOOTHNESS);
            damageMaterial.SetFloat(VignetteDarkeningID, 0f);
            damageMaterial.SetFloat(OverlayOpacityID, 0f);
            damageMaterial.SetFloat(ScratchesIntensityID, 0f);
        }

        if (healingMaterial != null)
        {
            healingMaterial.SetFloat(VignetteRadiusID, HEALING_RADIUS_NONE);
            healingMaterial.SetFloat(VignetteSmoothnessID, HEALING_SMOOTHNESS);
            healingMaterial.SetFloat(HealOpacityID, 0f);
            healingMaterial.SetFloat(HealIntensityID, 0f);
            // Set a default healing color (green/cyan glow)
            healingMaterial.SetColor(HealColorID, new Color(0.2f, 1f, 0.5f, 1f));
        }
    }

    private void ValidateSetup()
    {
        Debug.Log("=== EffectsController Setup Validation ===");

        if (rendererData == null)
        {
            Debug.LogError("❌ Renderer Data is NOT assigned! Please assign your ForwardRenderer asset.");
        }
        else
        {
            Debug.Log($"✅ Renderer Data assigned: {rendererData.name}");

            // List all features
            if (rendererData.rendererFeatures != null && rendererData.rendererFeatures.Count > 0)
            {
                Debug.Log($"📋 Found {rendererData.rendererFeatures.Count} Renderer Features:");
                foreach (var feature in rendererData.rendererFeatures)
                {
                    Debug.Log($"   - {feature.name} (Active: {feature.isActive})");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No Renderer Features found! You need to add them to your Renderer.");
            }
        }

        if (damageMaterial == null)
            Debug.LogWarning("⚠️ Damage Material is NOT assigned!");
        else
            Debug.Log($"✅ Damage Material assigned: {damageMaterial.name}");

        if (healingMaterial == null)
            Debug.LogWarning("⚠️ Healing Material is NOT assigned!");
        else
            Debug.Log($"✅ Healing Material assigned: {healingMaterial.name}");

        if (damageOverlayTextures == null || damageOverlayTextures.Length == 0)
            Debug.LogWarning("⚠️ No Damage Overlay Textures assigned!");
        else
            Debug.Log($"✅ {damageOverlayTextures.Length} Damage Overlay Textures assigned");

        if (healingOverlayTextures == null || healingOverlayTextures.Length == 0)
            Debug.LogWarning("⚠️ No Healing Overlay Textures assigned!");
        else
            Debug.Log($"✅ {healingOverlayTextures.Length} Healing Overlay Textures assigned");

        Debug.Log("==========================================");
    }

    private ScriptableRendererFeature FindRendererFeature(string featureName)
    {
        if (rendererData == null)
        {
            Debug.LogError($"❌ Cannot find '{featureName}' - Renderer Data is null!");
            return null;
        }

        if (rendererData.rendererFeatures == null || rendererData.rendererFeatures.Count == 0)
        {
            Debug.LogError($"❌ Cannot find '{featureName}' - No Renderer Features exist!");
            Debug.LogError("   Please add 'Full Screen Pass Renderer Feature' in your Renderer asset.");
            return null;
        }

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature.name == featureName)
            {
                Debug.Log($"✅ Found Renderer Feature: {featureName}");
                return feature;
            }
        }

        Debug.LogError($"❌ Could not find Renderer Feature named '{featureName}'.");
        Debug.LogError($"   Available features are:");
        foreach (var feature in rendererData.rendererFeatures)
        {
            Debug.LogError($"   - '{feature.name}'");
        }

        return null;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("🔴 Testing DAMAGE effect...");
            TriggerDamage(testDamageIntensity);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("💚 Testing HEALING effect...");
            TriggerHealing(testHealingIntensity);
        }
#endif
    }

    private void OnDisable()
    {
        if (damageFeature != null) damageFeature.SetActive(false);
        if (healingFeature != null) healingFeature.SetActive(false);

        if (damageMaterial != null)
        {
            damageMaterial.SetFloat(VignetteRadiusID, DAMAGE_RADIUS_NONE);
            damageMaterial.SetFloat(VignetteSmoothnessID, DAMAGE_SMOOTHNESS);
            damageMaterial.SetFloat(VignetteDarkeningID, 0f);
            damageMaterial.SetFloat(OverlayOpacityID, 0f);
            damageMaterial.SetFloat(ScratchesIntensityID, 0f);
        }

        if (healingMaterial != null)
        {
            healingMaterial.SetFloat(VignetteRadiusID, HEALING_RADIUS_NONE);
            healingMaterial.SetFloat(VignetteSmoothnessID, HEALING_SMOOTHNESS);
            healingMaterial.SetFloat(HealOpacityID, 0f);
            healingMaterial.SetFloat(HealIntensityID, 0f);
        }
    }

    // === PUBLIC API ===
    public static void TriggerDamage(float intensity)
    {
        if (instance != null)
            instance.StartDamageEffect(intensity);
        else
            Debug.LogError("❌ EffectsController.instance is null! Make sure EffectsController exists in scene.");
    }

    public static void TriggerHealing(float intensity)
    {
        if (instance != null)
            instance.StartHealingEffect(intensity);
        else
            Debug.LogError("❌ EffectsController.instance is null! Make sure EffectsController exists in scene.");
    }

    // === NEW: STOP METHODS ===
    public static void StopHealing()
    {
        if (instance != null)
            instance.ForceStopHealingEffect();
    }

    public static void StopDamage()
    {
        if (instance != null)
            instance.ForceStopDamageEffect();
    }

    private void ForceStopHealingEffect()
    {
        // Stop coroutine if running
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
        }

        // Disable feature
        if (healingFeature != null)
            healingFeature.SetActive(false);

        // Reset material properties immediately
        if (healingMaterial != null)
        {
            healingMaterial.SetFloat(VignetteRadiusID, HEALING_RADIUS_NONE);
            healingMaterial.SetFloat(HealOpacityID, 0f);
            healingMaterial.SetFloat(HealIntensityID, 0f);
        }

        if (debugMode)
            Debug.Log("🛑 Healing effect forcefully stopped");
    }

    private void ForceStopDamageEffect()
    {
        // Stop coroutine if running
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        // Disable feature
        if (damageFeature != null)
            damageFeature.SetActive(false);

        // Reset material properties immediately
        if (damageMaterial != null)
        {
            damageMaterial.SetFloat(VignetteRadiusID, DAMAGE_RADIUS_NONE);
            damageMaterial.SetFloat(VignetteDarkeningID, 0f);
            damageMaterial.SetFloat(OverlayOpacityID, 0f);
            damageMaterial.SetFloat(ScratchesIntensityID, 0f);
        }

        if (debugMode)
            Debug.Log("🛑 Damage effect forcefully stopped");
    }

    // === DAMAGE EFFECT ===
    private void StartDamageEffect(float intensity)
    {
        if (damageFeature == null)
        {
            Debug.LogWarning("⚠️ Cannot trigger damage effect - damageFeature is null!");
            return;
        }

        if (damageMaterial == null)
        {
            Debug.LogWarning("⚠️ Cannot trigger damage effect - damageMaterial is null!");
            return;
        }

        // Play damage sound
        if (damageSounds != null && damageSounds.Count > 0)
        {
            AudioClip clipToPlay = damageSounds[Random.Range(0, damageSounds.Count)];
            if (clipToPlay != null)
            {
                effectsAudioSource.PlayOneShot(clipToPlay);
            }
        }

        // Stop healing effect if active
        ForceStopHealingEffect();

        // Stop previous damage effect
        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        // Set random overlay texture
        if (damageOverlayTextures != null && damageOverlayTextures.Length > 0)
        {
            int randomIndex = Random.Range(0, damageOverlayTextures.Length);
            damageMaterial.SetTexture(DamageOverlayTexID, damageOverlayTextures[randomIndex]);
        }

        // Start effect
        damageFeature.SetActive(true);
        damageCoroutine = StartCoroutine(DamageVignette(intensity));
    }

    private IEnumerator DamageVignette(float intensity)
    {
        float fadeInTime = 0.1f;
        float holdTime = 0.5f;
        float fadeOutTime = 0.9f;

        float targetRadius = Mathf.Lerp(DAMAGE_RADIUS_NONE, DAMAGE_RADIUS_MAX, intensity);
        float targetDarkening = Mathf.Lerp(0f, DAMAGE_DARKENING_MAX, intensity);
        float targetOverlayOpacity = intensity;
        float targetScratches = intensity * 0.8f;

        float timer = 0;
        float startRadius = damageMaterial.GetFloat(VignetteRadiusID);
        float startDarkening = damageMaterial.GetFloat(VignetteDarkeningID);
        float startOpacity = damageMaterial.GetFloat(OverlayOpacityID);
        float startScratches = damageMaterial.GetFloat(ScratchesIntensityID);

        // Set smoothness
        damageMaterial.SetFloat(VignetteSmoothnessID, DAMAGE_SMOOTHNESS);

        // Fade in
        while (timer < fadeInTime)
        {
            float step = timer / fadeInTime;
            damageMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(startRadius, targetRadius, step));
            damageMaterial.SetFloat(VignetteDarkeningID, Mathf.Lerp(startDarkening, targetDarkening, step));
            damageMaterial.SetFloat(OverlayOpacityID, Mathf.Lerp(startOpacity, targetOverlayOpacity, step));
            damageMaterial.SetFloat(ScratchesIntensityID, Mathf.Lerp(startScratches, targetScratches, step));
            timer += Time.deltaTime;
            yield return null;
        }

        damageMaterial.SetFloat(VignetteRadiusID, targetRadius);
        damageMaterial.SetFloat(VignetteDarkeningID, targetDarkening);
        damageMaterial.SetFloat(OverlayOpacityID, targetOverlayOpacity);
        damageMaterial.SetFloat(ScratchesIntensityID, targetScratches);

        yield return new WaitForSeconds(holdTime);

        // Fade out
        timer = 0;
        while (timer < fadeOutTime)
        {
            float step = timer / fadeOutTime;
            damageMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(targetRadius, DAMAGE_RADIUS_NONE, step));
            damageMaterial.SetFloat(VignetteDarkeningID, Mathf.Lerp(targetDarkening, 0f, step));
            damageMaterial.SetFloat(OverlayOpacityID, Mathf.Lerp(targetOverlayOpacity, 0f, step));
            damageMaterial.SetFloat(ScratchesIntensityID, Mathf.Lerp(targetScratches, 0f, step));
            timer += Time.deltaTime;
            yield return null;
        }

        damageFeature.SetActive(false);
        damageMaterial.SetFloat(VignetteRadiusID, DAMAGE_RADIUS_NONE);
        damageMaterial.SetFloat(VignetteDarkeningID, 0f);
        damageMaterial.SetFloat(OverlayOpacityID, 0f);
        damageMaterial.SetFloat(ScratchesIntensityID, 0f);
        damageCoroutine = null;
    }

    // === HEALING EFFECT ===
    private void StartHealingEffect(float intensity)
    {
        if (healingFeature == null)
        {
            Debug.LogWarning("⚠️ Cannot trigger healing effect - healingFeature is null!");
            return;
        }

        if (healingMaterial == null)
        {
            Debug.LogWarning("⚠️ Cannot trigger healing effect - healingMaterial is null!");
            return;
        }

        if (debugMode)
        {
            Debug.Log($"💚 Starting healing effect with intensity: {intensity}");
        }

        // Play healing sound
        if (healingSound != null)
        {
            effectsAudioSource.PlayOneShot(healingSound);
        }

        // Stop damage effect if active
        ForceStopDamageEffect();

        // Stop previous healing effect
        if (healingCoroutine != null)
            StopCoroutine(healingCoroutine);

        // Set random overlay texture
        if (healingOverlayTextures != null && healingOverlayTextures.Length > 0)
        {
            int randomIndex = Random.Range(0, healingOverlayTextures.Length);
            healingMaterial.SetTexture(HealTextureID, healingOverlayTextures[randomIndex]);

            if (debugMode)
            {
                Debug.Log($"✅ Set healing texture: {healingOverlayTextures[randomIndex].name}");
            }
        }

        // Start effect
        healingFeature.SetActive(true);

        if (debugMode)
        {
            Debug.Log($"✅ Healing feature activated");
        }

        healingCoroutine = StartCoroutine(HealingVignette(intensity));
    }

    private IEnumerator HealingVignette(float intensity)
    {
        float fadeInTime = 0.15f;   // Quick punch-in
        float holdTime = 0.8f;      // Hold longer to appreciate the effect
        float fadeOutTime = 2.0f;   // Slow, smooth fade out

        float targetRadius = Mathf.Lerp(HEALING_RADIUS_NONE, HEALING_RADIUS_MAX, intensity);
        float targetHealOpacity = intensity * 0.6f;  // Reduced intensity (was intensity)
        float targetHealIntensity = intensity * 0.6f;

        float timer = 0;
        float startRadius = healingMaterial.GetFloat(VignetteRadiusID);
        float startHealOpacity = healingMaterial.GetFloat(HealOpacityID);
        float startHealIntensity = healingMaterial.GetFloat(HealIntensityID);

        // Set smoothness
        healingMaterial.SetFloat(VignetteSmoothnessID, HEALING_SMOOTHNESS);

        if (debugMode)
        {
            Debug.Log($"🔍 Healing values - Radius: {startRadius} → {targetRadius}, Opacity: {startHealOpacity} → {targetHealOpacity}");
        }

        // Fade in with slight ease
        while (timer < fadeInTime)
        {
            float step = timer / fadeInTime;
            // Ease-in for smooth start
            float eased = step * step;

            healingMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(startRadius, targetRadius, eased));
            healingMaterial.SetFloat(HealOpacityID, Mathf.Lerp(startHealOpacity, targetHealOpacity, eased));
            healingMaterial.SetFloat(HealIntensityID, Mathf.Lerp(startHealIntensity, targetHealIntensity, eased));
            timer += Time.deltaTime;
            yield return null;
        }

        healingMaterial.SetFloat(VignetteRadiusID, targetRadius);
        healingMaterial.SetFloat(HealOpacityID, targetHealOpacity);
        healingMaterial.SetFloat(HealIntensityID, targetHealIntensity);

        if (debugMode)
        {
            Debug.Log($"✅ Healing fade-in complete - Radius: {targetRadius}, Opacity: {targetHealOpacity}");
        }

        yield return new WaitForSeconds(holdTime);

        // Fade out with smooth easing (like the damage effect)
        timer = 0;
        while (timer < fadeOutTime)
        {
            float step = timer / fadeOutTime;
            // Cubic ease-out for beautiful smooth fade
            float eased = 1f - Mathf.Pow(1f - step, 3f);

            healingMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(targetRadius, HEALING_RADIUS_NONE, eased));
            healingMaterial.SetFloat(HealOpacityID, Mathf.Lerp(targetHealOpacity, 0f, eased));
            healingMaterial.SetFloat(HealIntensityID, Mathf.Lerp(targetHealIntensity, 0f, eased));
            timer += Time.deltaTime;
            yield return null;
        }

        healingFeature.SetActive(false);
        healingMaterial.SetFloat(VignetteRadiusID, HEALING_RADIUS_NONE);
        healingMaterial.SetFloat(HealOpacityID, 0f);
        healingMaterial.SetFloat(HealIntensityID, 0f);

        if (debugMode)
        {
            Debug.Log($"✅ Healing effect complete and disabled");
        }

        healingCoroutine = null;
    }
}