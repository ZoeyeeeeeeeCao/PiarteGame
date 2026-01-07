using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

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

    [Header("Testing Settings")]
    [SerializeField] private float testDamageAmount = 10f;
    [SerializeField] private float testHealAmount = 10f;

    void Start()
    {
        currentHealth = maxHealth;

        if (playerTransform == null)
        {
            playerTransform = transform;
            Debug.Log("🔧 Player Transform auto-assigned to this GameObject");
        }

        Debug.Log($"🔧 PlayerHealth is on: {gameObject.name}");
        Debug.Log($"🔧 playerTransform is set to: {playerTransform.name}");
        Debug.Log($"🔧 Particles will parent to: {playerTransform.name}");

        if (healParticlePrefab == null)
        {
            Debug.LogWarning("⚠️ Heal Particle Prefab not assigned in Inspector!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TakeDamage(testDamageAmount);
            Debug.Log($"🎮 TEST: Pressed [1] - Damage {testDamageAmount}");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Heal(testHealAmount);
            Debug.Log($"🎮 TEST: Pressed [2] - Heal {testHealAmount}");
        }
    }

    public void TakeDamage(float amount)
    {
        float previousHealth = currentHealth;
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"💥 DAMAGE: {amount} | Health: {previousHealth:F1} → {currentHealth:F1} ({(currentHealth / maxHealth) * 100:F0}%)");

        TriggerDamageShake(amount);

        if (currentHealth <= 0)
        {
            Die();
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
        float shakeMagnitude;

        if (damagePercentage >= 0.2f)
        {
            shakeMagnitude = heavyDamageShakeMagnitude;
            Debug.Log($"📹 HEAVY DAMAGE SHAKE: Magnitude={shakeMagnitude}");
        }
        else
        {
            shakeMagnitude = lightDamageShakeMagnitude;
            Debug.Log($"📹 Light damage shake: Magnitude={shakeMagnitude}");
        }

        CameraNewShake.Instance.Shake(duration: damageShakeDuration, magnitude: shakeMagnitude);
    }

    public void Heal(float amount)
    {
        float previousHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"💚 HEAL: +{amount} | Health: {previousHealth:F1} → {currentHealth:F1} ({(currentHealth / maxHealth) * 100:F0}%)");

        PlayHealParticles();
    }

    private void PlayHealParticles()
    {
        if (healParticlePrefab == null)
        {
            Debug.LogWarning("⚠️ Heal Particle Prefab not assigned!");
            return;
        }

        Transform spawnParent = playerTransform != null ? playerTransform : transform;

        Debug.Log($"🔍 Player position: {spawnParent.position}");
        Debug.Log($"🔍 Particle offset: {particleOffset}");

        // Instantiate as child of player
        GameObject particleInstance = Instantiate(healParticlePrefab, spawnParent);

        // Set local position relative to parent
        particleInstance.transform.localPosition = particleOffset;
        particleInstance.transform.localRotation = Quaternion.identity;

        Vector3 worldPos = particleInstance.transform.position;

        Debug.Log($"✨ Particle spawned!");
        Debug.Log($"   - Parent: {particleInstance.transform.parent?.name ?? "NONE"}");
        Debug.Log($"   - Local Pos: {particleInstance.transform.localPosition}");
        Debug.Log($"   - World Pos: {worldPos}");
        Debug.Log($"   - Is Parented: {particleInstance.transform.parent != null}");

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
                Debug.Log($"   - Particle System '{ps.name}' set to Local space and playing");
            }

            // Auto-destroy after the longest particle system finishes
            float maxDuration = 0f;
            foreach (ParticleSystem ps in particleSystems)
            {
                float psDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (psDuration > maxDuration)
                {
                    maxDuration = psDuration;
                }
            }

            Destroy(particleInstance, maxDuration);
            Debug.Log($"✨ Particles will auto-destroy in {maxDuration:F2}s");
        }
        else
        {
            Debug.LogWarning("⚠️ No ParticleSystem found in the prefab!");
            Destroy(particleInstance, healParticleDuration);
        }
    }

    private void Die()
    {
        Debug.Log("💀 DEATH: Player has died.");
    }
}