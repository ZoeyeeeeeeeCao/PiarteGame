using UnityEngine;
using System.Collections.Generic;

public class WeaponTrailEffect : MonoBehaviour
{
    [Header("Setup - General")]
    [SerializeField] private bool enableGizmos = true;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private string trailName = "Trail 1";

    [Header("Trail Transform Settings")]
    [SerializeField] private Transform lineTipTransform;
    [SerializeField] private Transform lineBottomTransform;

    [Header("Particle Prefab Settings")]
    [SerializeField] private GameObject particleEffectPrefab;
    [SerializeField] private int particleSpawnCount = 3; // Number of particle systems along the blade
    [SerializeField] private float particleScale = 1f; // Overall scale multiplier

    [Header("Trail Settings")]
    [SerializeField] private bool enableTrail = false;

    // Trail state
    private bool isTrailActive = false;
    private List<GameObject> activeParticleEffects = new List<GameObject>();
    private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

    private void Start()
    {
        if (particleEffectPrefab == null)
        {
            Debug.LogWarning("Particle Effect Prefab is not assigned! Please assign a particle system prefab.");
        }
    }

    private void Update()
    {
        if (enableTrail && !isTrailActive)
        {
            StartTrail();
        }
        else if (!enableTrail && isTrailActive)
        {
            StopTrail();
        }

        if (isTrailActive && activeParticleEffects.Count > 0)
        {
            UpdateTrailPositions();
        }
    }

    public void StartTrail()
    {
        if (particleEffectPrefab == null)
        {
            Debug.LogWarning("Cannot start trail - Particle Effect Prefab is not assigned!");
            return;
        }

        if (lineTipTransform == null || lineBottomTransform == null)
        {
            Debug.LogWarning("Trail transforms not assigned!");
            return;
        }

        isTrailActive = true;

        // Spawn multiple particle effects along the blade
        if (activeParticleEffects.Count == 0)
        {
            for (int i = 0; i < particleSpawnCount; i++)
            {
                // Calculate position along the blade (0 = bottom, 1 = tip)
                float t = particleSpawnCount > 1 ? (float)i / (particleSpawnCount - 1) : 0.5f;
                Vector3 spawnPos = Vector3.Lerp(lineBottomTransform.position, lineTipTransform.position, t);

                GameObject particleEffect = Instantiate(particleEffectPrefab, spawnPos, Quaternion.identity, transform);
                particleEffect.name = $"{trailName} Effect {i + 1}";
                particleEffect.transform.localScale = Vector3.one * particleScale;

                ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    particleSystems.Add(ps);
                }

                activeParticleEffects.Add(particleEffect);
            }
        }

        if (debugMode)
        {
            Debug.Log($"Trail started - {particleSpawnCount} particle effects spawned");
        }
    }

    public void StopTrail()
    {
        isTrailActive = false;

        // Stop all particle systems
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // Destroy all effects after particles fade out
        if (particleSystems.Count > 0 && particleSystems[0] != null)
        {
            float lifetime = particleSystems[0].main.startLifetime.constantMax + particleSystems[0].main.duration;
            foreach (var effect in activeParticleEffects)
            {
                if (effect != null)
                {
                    Destroy(effect, lifetime);
                }
            }
        }
        else
        {
            // Fallback if we can't get lifetime
            foreach (var effect in activeParticleEffects)
            {
                if (effect != null)
                {
                    Destroy(effect, 2f);
                }
            }
        }

        activeParticleEffects.Clear();
        particleSystems.Clear();

        if (debugMode)
        {
            Debug.Log("Trail stopped");
        }
    }

    private void UpdateTrailPositions()
    {
        if (lineTipTransform == null || lineBottomTransform == null)
            return;

        Vector3 tipPos = lineTipTransform.position;
        Vector3 bottomPos = lineBottomTransform.position;
        Vector3 direction = tipPos - bottomPos;

        // Update each particle effect position along the blade
        for (int i = 0; i < activeParticleEffects.Count; i++)
        {
            if (activeParticleEffects[i] != null)
            {
                float t = particleSpawnCount > 1 ? (float)i / (particleSpawnCount - 1) : 0.5f;
                Vector3 targetPos = Vector3.Lerp(bottomPos, tipPos, t);

                activeParticleEffects[i].transform.position = targetPos;

                // Orient particle effect along the blade direction
                if (direction.magnitude > 0.001f)
                {
                    activeParticleEffects[i].transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        if (debugMode)
        {
            Debug.DrawLine(bottomPos, tipPos, Color.cyan);
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableGizmos)
            return;

        // Draw tip transform
        if (lineTipTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lineTipTransform.position, 0.02f);
            Gizmos.DrawLine(lineTipTransform.position, lineTipTransform.position + lineTipTransform.right * 0.1f);
        }

        // Draw bottom transform
        if (lineBottomTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lineBottomTransform.position, 0.02f);
            Gizmos.DrawLine(lineBottomTransform.position, lineBottomTransform.position + lineBottomTransform.right * 0.1f);
        }

        // Draw connection line
        if (lineTipTransform != null && lineBottomTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lineTipTransform.position, lineBottomTransform.position);

            // Draw spawn points preview
            for (int i = 0; i < particleSpawnCount; i++)
            {
                float t = particleSpawnCount > 1 ? (float)i / (particleSpawnCount - 1) : 0.5f;
                Vector3 spawnPos = Vector3.Lerp(lineBottomTransform.position, lineTipTransform.position, t);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPos, 0.03f);
            }
        }
    }

    // Public methods for manual control
    public void SetTrailEnabled(bool enabled)
    {
        enableTrail = enabled;
    }

    public void SetParticleCount(int count)
    {
        if (count > 0)
        {
            particleSpawnCount = count;
            if (isTrailActive)
            {
                StopTrail();
                StartTrail();
            }
        }
    }

    public void SetParticleScale(float scale)
    {
        particleScale = scale;
        foreach (var effect in activeParticleEffects)
        {
            if (effect != null)
            {
                effect.transform.localScale = Vector3.one * particleScale;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var effect in activeParticleEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeParticleEffects.Clear();
        particleSystems.Clear();
    }
}