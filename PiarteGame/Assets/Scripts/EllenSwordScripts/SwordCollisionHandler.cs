using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Attach this script to your SWORD prefab (not the player).
/// Handles all collision detection, damage dealing, and VFX spawning.
/// </summary>
public class SwordCollisionHandler : MonoBehaviour
{
    [Header("Collision Setup")]
    [SerializeField] private Collider swordCollider;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool debugCollisions = true;

    [Header("Sword Blade Points")]
    [Tooltip("Top point of the blade (for VFX positioning)")]
    [SerializeField] private Transform swordTipTransform;
    [Tooltip("Bottom point of the blade (for VFX positioning)")]
    [SerializeField] private Transform swordBottomTransform;

    [Header("Contact VFX (Along Blade)")]
    [SerializeField] private GameObject[] enemyContactVFXPrefabs;
    [SerializeField] private GameObject[] generalContactVFXPrefabs;
    [SerializeField] private int vfxSpawnCount = 3;
    [SerializeField] private float contactVFXLifetime = 2f;
    [SerializeField] private bool scaleVFXToSwordLength = true;
    [SerializeField] private float vfxSizeMultiplier = 1f;

    [Header("Impact Wave VFX (Around Player)")]
    [SerializeField] private GameObject[] enemyImpactWaveVFXPrefabs;
    [SerializeField] private GameObject[] generalImpactWaveVFXPrefabs;
    [SerializeField] private float impactWaveVFXLifetime = 3f;
    [SerializeField] private Vector3 impactWaveOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] private Transform playerTransform;

    [Header("Audio")]
    [SerializeField] private SwordAudioManager audioManager;

    [Header("Camera Shake")]
    [SerializeField] private bool useFS_CameraShakeBridgeOnHit = true;
    [SerializeField] private float hitShakeDuration = 0.15f;
    [SerializeField] private float hitShakeMagnitude = 0.08f;
    [SerializeField] private float hitShakeRotation = 1.5f;

    [Header("Hit VFX on All Collisions")]
    [Tooltip("Spawn VFX when hitting any object with a collider")]
    [SerializeField] private bool spawnVFXOnAllCollisions = true;
    [SerializeField] private GameObject[] genericHitVFXPrefabs;
    [SerializeField] private float genericHitVFXLifetime = 1.5f;

    [Header("Knockback Settings")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private float normalKnockbackForce = 3f;
    [SerializeField] private float hardKnockbackForce = 10f;
    [SerializeField] private float hardAttackSpreadAngle = 15f;

    // State tracking
    private bool isCollisionActive = false;
    private int currentAttackDamage = 0;
    private bool isHardAttack = false;
    private HashSet<Collider> hitTargetsThisAttack = new HashSet<Collider>();

    private void Start()
    {
        InitializeCollider();

        if (playerTransform == null)
        {
            // Try to find player transform automatically
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // Try to find audio manager if not assigned
        if (audioManager == null)
        {
            audioManager = GetComponent<SwordAudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("⚠️ SwordAudioManager not found. Audio will not play.");
            }
        }
    }

    private void Update()
    {
        // Debug: Show when collision is active
        if (debugCollisions && isCollisionActive)
        {
            string attackType = isHardAttack ? "HARD ATTACK" : "Normal";
            Debug.Log($"⚔️ COLLISION ACTIVE - Waiting for hits... (Damage: {currentAttackDamage}, Type: {attackType})");
        }
    }

    private void InitializeCollider()
    {
        if (swordCollider == null)
        {
            swordCollider = GetComponent<Collider>();
        }

        if (swordCollider == null)
        {
            Debug.LogError("❌ SWORD COLLIDER NOT FOUND! Add a Capsule/Box Collider to the sword blade and assign it.");
            return;
        }

        if (!swordCollider.isTrigger)
        {
            swordCollider.isTrigger = true;
            Debug.LogWarning("⚠️ Sword collider was not set as trigger. Fixed automatically.");
        }

        swordCollider.enabled = false;
        Debug.Log("✅ Sword collision handler initialized");
    }

    public void EnableCollision(int damage, bool hardAttack = false)
    {
        if (swordCollider == null)
        {
            Debug.LogError("❌ Cannot enable collision - collider not found!");
            return;
        }

        isCollisionActive = true;
        currentAttackDamage = damage;
        isHardAttack = hardAttack;
        hitTargetsThisAttack.Clear();
        swordCollider.enabled = true;

        string attackType = hardAttack ? "HARD ATTACK" : "Normal Attack";
        Debug.Log($"⚔️ COLLISION ENABLED - Damage: {damage} ({attackType})");
    }

    public void DisableCollision()
    {
        if (swordCollider == null) return;

        isCollisionActive = false;
        currentAttackDamage = 0;
        isHardAttack = false;
        swordCollider.enabled = false;

        if (hitTargetsThisAttack.Count > 0)
        {
            Debug.Log($"🛡️ COLLISION DISABLED - Hit {hitTargetsThisAttack.Count} targets this attack");
        }
        else
        {
            Debug.Log($"🛡️ COLLISION DISABLED - No hits detected");
        }

        hitTargetsThisAttack.Clear();
    }

    private void HandleEnemyHit(Collider enemyCollider, Vector3 hitPosition, Vector3 hitDirection)
    {
        string attackType = isHardAttack ? "HARD ATTACK" : "normal attack";
        Debug.Log($"⚔️ SWORD HIT ENEMY: {enemyCollider.gameObject.name} with {attackType}!");

        // Deal damage
        EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(currentAttackDamage);
            Debug.Log($"💔 Dealt {currentAttackDamage} damage to {enemyCollider.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ {enemyCollider.gameObject.name} is on enemy layer but has no EnemyHealth component!");
        }

        // Apply knockback
        if (enableKnockback && playerTransform != null)
        {
            EnemyKnockback knockback = enemyCollider.GetComponent<EnemyKnockback>();
            if (knockback != null)
            {
                if (isHardAttack)
                {
                    // Hard attack: push enemies back hard with spread
                    knockback.ApplyHardKnockback(playerTransform.position, playerTransform.forward);
                    Debug.Log($"💥💥 Applied HARD knockback to {enemyCollider.gameObject.name}");
                }
                else
                {
                    // Normal attack: light knockback
                    knockback.ApplyNormalKnockback(playerTransform.position);
                    Debug.Log($"💥 Applied normal knockback to {enemyCollider.gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ {enemyCollider.gameObject.name} has no EnemyKnockback component!");
            }
        }

        // Camera shake
        if (useFS_CameraShakeBridgeOnHit && FS_FS_CameraShakeBridgeBridge.Instance != null)
        {
            FS_FS_CameraShakeBridgeBridge.Instance.Shake(hitShakeDuration, hitShakeMagnitude, hitShakeRotation);
        }

        // Play hit sound
        if (audioManager != null)
        {
            audioManager.PlayHitSound();
        }

        // Spawn VFX
        SpawnContactVFXAlongBlade(enemyContactVFXPrefabs, hitPosition, hitDirection);
        SpawnImpactWaveVFX(enemyImpactWaveVFXPrefabs);
    }

    private void HandleGeneralCollision(Collider otherCollider, Vector3 hitPosition, Vector3 hitDirection)
    {
        if (debugCollisions)
            Debug.Log($"💥 SWORD HIT OBJECT: {otherCollider.gameObject.name}");

        // Spawn general VFX
        if (spawnVFXOnAllCollisions)
        {
            SpawnContactVFXAlongBlade(generalContactVFXPrefabs, hitPosition, hitDirection);
            SpawnImpactWaveVFX(generalImpactWaveVFXPrefabs);

            // Spawn generic hit VFX at collision point
            if (genericHitVFXPrefabs != null && genericHitVFXPrefabs.Length > 0)
            {
                SpawnGenericHitVFX(hitPosition, hitDirection);
            }
        }
    }

    private void SpawnContactVFXAlongBlade(GameObject[] vfxArray, Vector3 hitPosition, Vector3 hitDirection)
    {
        if (vfxArray == null || vfxArray.Length == 0) return;

        if (swordTipTransform == null || swordBottomTransform == null)
        {
            // Fallback: spawn at hit point only
            SpawnSingleVFX(vfxArray, hitPosition, hitDirection, vfxSizeMultiplier, contactVFXLifetime);
            return;
        }

        Vector3 tipPos = swordTipTransform.position;
        Vector3 bottomPos = swordBottomTransform.position;
        Vector3 bladeDirection = (tipPos - bottomPos).normalized;
        float bladeLength = Vector3.Distance(tipPos, bottomPos);

        // Ensure blade direction is valid
        if (bladeDirection.magnitude < 0.01f)
        {
            bladeDirection = Vector3.up;
        }

        float vfxScale = scaleVFXToSwordLength ? (bladeLength / vfxSpawnCount) * vfxSizeMultiplier : vfxSizeMultiplier;

        int randomIndex = Random.Range(0, vfxArray.Length);
        GameObject vfxPrefab = vfxArray[randomIndex];

        if (vfxPrefab == null) return;

        // Spawn VFX along the blade
        for (int i = 0; i < vfxSpawnCount; i++)
        {
            float t = vfxSpawnCount > 1 ? (float)i / (vfxSpawnCount - 1) : 0.5f;
            Vector3 spawnPosition = Vector3.Lerp(bottomPos, tipPos, t);

            // Ensure hit direction is valid
            Vector3 safeHitDirection = hitDirection.magnitude > 0.01f ? hitDirection : Vector3.forward;
            Quaternion vfxRotation = Quaternion.LookRotation(-safeHitDirection, bladeDirection);

            GameObject vfx = Instantiate(vfxPrefab, spawnPosition, vfxRotation);
            vfx.transform.localScale = Vector3.one * vfxScale;
            Destroy(vfx, contactVFXLifetime);
        }

        if (debugCollisions)
            Debug.Log($"💥 Spawned {vfxSpawnCount} contact VFX along blade");
    }

    private void SpawnSingleVFX(GameObject[] vfxArray, Vector3 position, Vector3 direction, float scale, float lifetime)
    {
        if (vfxArray == null || vfxArray.Length == 0) return;

        int randomIndex = Random.Range(0, vfxArray.Length);
        GameObject vfxPrefab = vfxArray[randomIndex];

        if (vfxPrefab != null)
        {
            // Ensure direction is valid
            Vector3 safeDirection = direction.magnitude > 0.01f ? direction : Vector3.forward;
            Quaternion vfxRotation = Quaternion.LookRotation(-safeDirection);
            GameObject vfx = Instantiate(vfxPrefab, position, vfxRotation);
            vfx.transform.localScale = Vector3.one * scale;
            Destroy(vfx, lifetime);
        }
    }

    public void CheckForHitAtEvent(int damage)
    {
        if (swordTipTransform == null || swordBottomTransform == null) return;

        // 1. Define the sword's space as a Capsule (more accurate for a blade)
        Vector3 point1 = swordTipTransform.position;
        Vector3 point2 = swordBottomTransform.position;
        float radius = 0.5f; // Adjust this for how "thick" your hit detection should be

        // 2. Look for any colliders inside that capsule RIGHT NOW
        Collider[] hitColliders = Physics.OverlapCapsule(point1, point2, radius, enemyLayer);

        foreach (Collider col in hitColliders)
        {
            // Don't hit the same enemy twice in one swing
            if (hitTargetsThisAttack.Contains(col)) continue;

            hitTargetsThisAttack.Add(col);

            // 3. Logic for the HIT VFX and Damage
            Vector3 hitPoint = col.ClosestPoint(point1);
            Vector3 hitDirection = (hitPoint - transform.position).normalized;

            Debug.Log($"💥 HIT DETECTED VIA EVENT on {col.name}! Spawning Hit VFX.");

            // Deal Damage
            EnemyHealth health = col.GetComponent<EnemyHealth>();
            if (health != null) health.TakeDamage(damage);

            // Apply knockback
            if (enableKnockback && playerTransform != null)
            {
                EnemyKnockback knockback = col.GetComponent<EnemyKnockback>();
                if (knockback != null)
                {
                    if (isHardAttack)
                    {
                        knockback.ApplyHardKnockback(playerTransform.position, playerTransform.forward);
                    }
                    else
                    {
                        knockback.ApplyNormalKnockback(playerTransform.position);
                    }
                }
            }

            // Play the Camera Shake
            if (useFS_CameraShakeBridgeOnHit && CameraNewShake.Instance != null)
            {
                CameraNewShake.Instance.Shake(hitShakeDuration, hitShakeMagnitude, hitShakeRotation);
            }

            // Play hit sound
            if (audioManager != null)
            {
                audioManager.PlayHitSound();
            }

            // Spawn the ACTUAL HIT EFFECTS (Sparks/Impacts)
            SpawnContactVFXAlongBlade(enemyContactVFXPrefabs, hitPoint, hitDirection);
            SpawnImpactWaveVFX(enemyImpactWaveVFXPrefabs);
        }
    }

    public void ForceInstantHitCheck(int damage)
    {
        // 1. Create a "hit zone" around the sword tip and base
        Vector3 center = (swordTipTransform.position + swordBottomTransform.position) / 2f;
        float radius = Vector3.Distance(swordTipTransform.position, swordBottomTransform.position) / 2f;

        // 2. Look for enemies in that zone RIGHT NOW
        Collider[] hitEnemies = Physics.OverlapSphere(center, radius + 0.5f, enemyLayer);

        foreach (Collider col in hitEnemies)
        {
            // Avoid hitting the same enemy twice in one swing
            if (hitTargetsThisAttack.Contains(col)) continue;

            hitTargetsThisAttack.Add(col);

            // 3. THIS IS THE HIT VFX LOGIC
            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 direction = (hitPoint - center).normalized;

            Debug.Log($"🎯 EVENT HIT: Found enemy {col.name}. Triggering HIT VFX!");

            // This calls your existing HandleEnemyHit logic (Damage, Camera Shake, and HIT VFX)
            HandleEnemyHit(col, hitPoint, direction);
        }
    }

    private void SpawnImpactWaveVFX(GameObject[] vfxArray)
    {
        if (vfxArray == null || vfxArray.Length == 0) return;
        if (playerTransform == null)
        {
            Debug.LogWarning("⚠️ Player transform not assigned - cannot spawn impact wave VFX");
            return;
        }

        int randomIndex = Random.Range(0, vfxArray.Length);
        GameObject vfxPrefab = vfxArray[randomIndex];

        if (vfxPrefab != null)
        {
            Vector3 spawnPosition = playerTransform.position + impactWaveOffset;
            Quaternion spawnRotation = Quaternion.Euler(-90, 0, 0);

            GameObject impactVFX = Instantiate(vfxPrefab, spawnPosition, spawnRotation);
            Destroy(impactVFX, impactWaveVFXLifetime);

            if (debugCollisions)
                Debug.Log($"🌊 Spawned impact wave VFX: {vfxPrefab.name}");
        }
    }

    private void SpawnGenericHitVFX(Vector3 hitPosition, Vector3 hitDirection)
    {
        int randomIndex = Random.Range(0, genericHitVFXPrefabs.Length);
        GameObject vfxPrefab = genericHitVFXPrefabs[randomIndex];

        if (vfxPrefab != null)
        {
            // Ensure direction is valid
            Vector3 safeDirection = hitDirection.magnitude > 0.01f ? hitDirection : Vector3.forward;
            Quaternion vfxRotation = Quaternion.LookRotation(-safeDirection);
            GameObject vfx = Instantiate(vfxPrefab, hitPosition, vfxRotation);
            Destroy(vfx, genericHitVFXLifetime);

            if (debugCollisions)
                Debug.Log($"✨ Spawned generic hit VFX at collision point");
        }
    }

    // Public getters
    public bool IsCollisionActive => isCollisionActive;
    public int CurrentDamage => currentAttackDamage;
    public int HitCountThisAttack => hitTargetsThisAttack.Count;
}