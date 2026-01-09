using UnityEngine;

/// <summary>
/// Attached to a weapon's collider (trigger) to deal damage to the player.
/// IMPORTANT: This GameObject should be on the "EnemyWeapon" layer!
/// Now integrated with PlayerHealthController!
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage dealt to the player on hit.")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("The tag assigned to the Player game object.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Cooldown Settings")]
    [Tooltip("Time in seconds before this weapon can damage the player again")]
    [SerializeField] private float damageCooldown = 1f;

    [Header("Hit Effects")]
    [Tooltip("Spawn VFX at hit point when damaging player")]
    [SerializeField] private GameObject[] hitVFXPrefabs;
    [SerializeField] private float hitVFXLifetime = 2f;

    [Tooltip("Play sound when hitting player")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private float hitSoundVolume = 0.7f;

    [Header("Camera Shake on Hit")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private float lastDamageTime = -999f;
    private AudioSource audioSource;

    private void Start()
    {
        // Verify setup
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("❌ EnemyDamage: No collider found! Add a collider component.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("❌ EnemyDamage: Collider must be set as TRIGGER!");
        }

        // Check if on correct layer
        if (gameObject.layer != LayerMask.NameToLayer("EnemyWeapon"))
        {
            Debug.LogWarning($"⚠️ EnemyDamage: GameObject '{gameObject.name}' should be on 'EnemyWeapon' layer for proper physics!");
        }

        // Setup audio source for hit sounds
        if (hitSounds != null && hitSounds.Length > 0)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.volume = hitSoundVolume;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 15f;
        }

        if (debugMode)
        {
            Debug.Log($"✅ EnemyDamage initialized on {gameObject.name} (Damage: {damageAmount})");
        }
    }

    /// <summary>
    /// Detects collision with the player and applies damage.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Cooldown check - prevent spam damage
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        // Check if the object hit has the Player tag
        if (other.CompareTag(playerTag))
        {
            // Use the singleton instance of PlayerHealthController
            if (PlayerHealthController.Instance != null)
            {
                // Check if player is already dead
                if (PlayerHealthController.Instance.IsDead)
                {
                    if (debugMode)
                        Debug.Log("⚠️ Player is already dead, no damage applied.");
                    return;
                }

                // Apply damage through the controller
                PlayerHealthController.Instance.Damage(damageAmount);
                lastDamageTime = Time.time;

                if (debugMode)
                {
                    Debug.Log($"⚔️ Enemy weapon hit player! Dealt {damageAmount} damage. Player health: {PlayerHealthController.Instance.CurrentHealth}/{PlayerHealthController.Instance.MaxHealth}");
                }

                // Play hit effects
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                PlayHitEffects(hitPoint);

                // Trigger camera shake
                if (enableCameraShake)
                {
                    TriggerCameraShake();
                }
            }
            else
            {
                Debug.LogError("❌ PlayerHealthController.Instance is NULL! Make sure PlayerHealthController is in the scene and has Awake() called.");
            }
        }
    }

    private void PlayHitEffects(Vector3 hitPosition)
    {
        // Spawn VFX
        if (hitVFXPrefabs != null && hitVFXPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, hitVFXPrefabs.Length);
            GameObject vfxPrefab = hitVFXPrefabs[randomIndex];

            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab, hitPosition, Quaternion.identity);
                Destroy(vfx, hitVFXLifetime);

                if (debugMode)
                    Debug.Log($"💥 Spawned hit VFX at {hitPosition}");
            }
        }

        // Play sound
        if (audioSource != null && hitSounds != null && hitSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, hitSounds.Length);
            AudioClip clip = hitSounds[randomIndex];

            if (clip != null)
            {
                audioSource.PlayOneShot(clip, hitSoundVolume);
            }
        }
    }

    private void TriggerCameraShake()
    {
        if (CameraNewShake.Instance != null)
        {
            CameraNewShake.Instance.Shake(shakeDuration, shakeMagnitude);

            if (debugMode)
                Debug.Log($"📹 Triggered camera shake (Duration: {shakeDuration}s, Magnitude: {shakeMagnitude})");
        }
        else if (debugMode)
        {
            Debug.LogWarning("⚠️ CameraNewShake.Instance not found! Camera shake won't play.");
        }
    }

    // Visualize the trigger in the editor
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawSphere(capsule.center, capsule.radius);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw damage range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}