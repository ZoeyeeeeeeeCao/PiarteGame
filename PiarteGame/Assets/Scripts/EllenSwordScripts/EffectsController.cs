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

    // Shader Property IDs
    private static readonly int VignetteRadiusID = Shader.PropertyToID("_VignetteRadius");
    private static readonly int OverlayOpacityID = Shader.PropertyToID("_OverlayOpacity");
    private static readonly int DamageOverlayTexID = Shader.PropertyToID("_MainTex");
    private static readonly int HealOpacityID = Shader.PropertyToID("_HealOpacity");
    private static readonly int HealTextureID = Shader.PropertyToID("_HealTexture");
    private static readonly int HealIntensityID = Shader.PropertyToID("_Intensity");

    // Effect Strength Values
    private const float DAMAGE_RADIUS_NONE = 1.0f;
    private const float DAMAGE_RADIUS_MAX = 0.3f;
    private const float HEALING_RADIUS_NONE = 15f;
    private const float HEALING_RADIUS_MAX = 3f;

    [Header("Tester Settings")]
    [Range(0f, 1f)]
    public float testDamageIntensity = 0.8f;
    [Range(0f, 1f)]
    public float testHealingIntensity = 1.0f;

    public void Awake()
    {
        instance = this;
        effectsAudioSource = GetComponent<AudioSource>();

        // Validate setup
        ValidateSetup();

        // Find renderer features
        damageFeature = FindRendererFeature("FullScreenPassDamage");
        healingFeature = FindRendererFeature("FullScreenPassHealing");
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
            damageMaterial.SetFloat(OverlayOpacityID, 0f);
        }

        if (healingMaterial != null)
        {
            healingMaterial.SetFloat(VignetteRadiusID, HEALING_RADIUS_NONE);
            healingMaterial.SetFloat(HealOpacityID, 0f);
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

    // === DAMAGE EFFECT ===
    private void StartDamageEffect(float intensity)
    {
        if (damageFeature == null)
        {
            Debug.LogWarning("⚠️ Cannot trigger damage effect - damageFeature is null!");
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
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingFeature.SetActive(false);
            healingCoroutine = null;
        }

        // Stop previous damage effect
        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        // Set random overlay texture
        if (damageOverlayTextures != null && damageOverlayTextures.Length > 0 && damageMaterial != null)
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
        float targetOverlayOpacity = intensity;

        float timer = 0;
        float startRadius = damageMaterial.GetFloat(VignetteRadiusID);
        float startOpacity = damageMaterial.GetFloat(OverlayOpacityID);

        // Fade in
        while (timer < fadeInTime)
        {
            float step = timer / fadeInTime;
            damageMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(startRadius, targetRadius, step));
            damageMaterial.SetFloat(OverlayOpacityID, Mathf.Lerp(startOpacity, targetOverlayOpacity, step));
            timer += Time.deltaTime;
            yield return null;
        }

        damageMaterial.SetFloat(VignetteRadiusID, targetRadius);
        damageMaterial.SetFloat(OverlayOpacityID, targetOverlayOpacity);

        yield return new WaitForSeconds(holdTime);

        // Fade out
        timer = 0;
        while (timer < fadeOutTime)
        {
            float step = timer / fadeOutTime;
            damageMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(targetRadius, DAMAGE_RADIUS_NONE, step));
            damageMaterial.SetFloat(OverlayOpacityID, Mathf.Lerp(targetOverlayOpacity, 0f, step));
            timer += Time.deltaTime;
            yield return null;
        }

        damageFeature.SetActive(false);
        damageMaterial.SetFloat(VignetteRadiusID, DAMAGE_RADIUS_NONE);
        damageMaterial.SetFloat(OverlayOpacityID, 0f);
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

        // Play healing sound
        if (healingSound != null)
        {
            effectsAudioSource.PlayOneShot(healingSound);
        }

        // Stop damage effect if active
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageFeature.SetActive(false);
            damageCoroutine = null;
        }

        // Stop previous healing effect
        if (healingCoroutine != null)
            StopCoroutine(healingCoroutine);

        // Set random overlay texture
        if (healingOverlayTextures != null && healingOverlayTextures.Length > 0 && healingMaterial != null)
        {
            int randomIndex = Random.Range(0, healingOverlayTextures.Length);
            healingMaterial.SetTexture(HealTextureID, healingOverlayTextures[randomIndex]);
        }

        // Start effect
        healingFeature.SetActive(true);
        healingCoroutine = StartCoroutine(HealingVignette(intensity));
    }

    private IEnumerator HealingVignette(float intensity)
    {
        float fadeInTime = 0.1f;
        float holdTime = 0.5f;
        float fadeOutTime = 0.9f;

        float targetRadius = Mathf.Lerp(HEALING_RADIUS_NONE, HEALING_RADIUS_MAX, intensity);
        float targetHealOpacity = intensity;

        float timer = 0;
        float startRadius = healingMaterial.GetFloat(VignetteRadiusID);
        float startHealOpacity = healingMaterial.GetFloat(HealOpacityID);

        // Fade in
        while (timer < fadeInTime)
        {
            float step = timer / fadeInTime;
            healingMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(startRadius, targetRadius, step));
            healingMaterial.SetFloat(HealOpacityID, Mathf.Lerp(startHealOpacity, targetHealOpacity, step));
            timer += Time.deltaTime;
            yield return null;
        }

        healingMaterial.SetFloat(VignetteRadiusID, targetRadius);
        healingMaterial.SetFloat(HealOpacityID, targetHealOpacity);

        yield return new WaitForSeconds(holdTime);

        // Fade out
        timer = 0;
        while (timer < fadeOutTime)
        {
            float step = timer / fadeOutTime;
            healingMaterial.SetFloat(VignetteRadiusID, Mathf.Lerp(targetRadius, HEALING_RADIUS_NONE, step));
            healingMaterial.SetFloat(HealOpacityID, Mathf.Lerp(targetHealOpacity, 0f, step));
            timer += Time.deltaTime;
            yield return null;
        }

        healingFeature.SetActive(false);
        healingMaterial.SetFloat(VignetteRadiusID, HEALING_RADIUS_NONE);
        healingMaterial.SetFloat(HealOpacityID, 0f);
        healingCoroutine = null;
    }
}