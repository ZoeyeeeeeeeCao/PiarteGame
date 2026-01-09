using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Attach this script to your SWORD prefab (not the player).
/// Handles all collision detection, damage dealing, and VFX spawning.
/// NOW USES ENEMY TAG INSTEAD OF LAYER!
/// </summary>
public class SwordCollisionHandler : MonoBehaviour
{
    [Header("Collision Setup")]
    [SerializeField] private Collider swordCollider;
    [Tooltip("Tag used to identify enemies (e.g., 'Enemy')")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private bool debugCollisions = true;

    [Header("Sword Blade Points")]
    [Tooltip("Top point of the blade (for VFX positioning)")]
    [SerializeField] private Transform swordTipTransform;
    [Tooltip("Bottom point of the blade (for VFX positioning)")]
    [SerializeField] private Transform swordBottomTransform;

    [Header("Hard Attack AOE Settings")]
    [Tooltip("Radius around player for hard attack AOE damage")]
    [SerializeField] private float hardAttackAOERadius = 2f;
    [Tooltip("Damage dealt to enemies hit directly by the sword")]
    [SerializeField] private int hardAttackDirectDamage = 40;
    [Tooltip("Damage dealt to enemies in AOE range but not directly hit")]
    [SerializeField] private int hardAttackAOEDamage = 20;
    [SerializeField] private bool showAOEDebugGizmos = true;

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
    [SerializeField] private float hardAttackShakeDuration = 0.3f;
    [SerializeField] private float hardAttackShakeMagnitude = 0.2f;

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

    [Header("Player Pull Settings")]
    [SerializeField] private bool enablePlayerPull = true;
    [SerializeField] private float normalPullDistance = 0.8f;
    [SerializeField] private float hardPullDistance = 1.5f;
    [SerializeField] private float pullSpeed = 15f;
    [SerializeField] private float minDistanceToEnemy = 2f;
    [SerializeField] private bool pullOnEveryHit = true;
    [Tooltip("If true, pulls player slightly at the start of each attack towards nearest enemy")]
    [SerializeField] private bool autoPullOnAttackStart = true;
    [SerializeField] private float autoPullDistance = 0.5f;
    [SerializeField] private float autoPullMaxRange = 5f;

    [Header("Swing VFX (When Enemy In Range)")]
    [Tooltip("VFX that spawns when swinging near an enemy")]
    [SerializeField] private GameObject[] swingVFXPrefabs;
    [SerializeField] private Transform swingVFXSpawnPoint;
    [SerializeField] private float swingVFXLifetime = 1f;
    [SerializeField] private float swingVFXDetectionRadius = 3f;
    [SerializeField] private bool spawnSwingVFXOnAllSwings = false;

    // State tracking
    private bool isCollisionActive = false;
    private int currentAttackDamage = 0;
    private bool isHardAttack = false;
    private HashSet<Collider> hitTargetsThisAttack = new HashSet<Collider>();
    private HashSet<Collider> aoeHitTargets = new HashSet<Collider>();
    private CharacterController characterController;
    private Collider lastTargetedEnemy = null;

    private void Start()
    {
        InitializeCollider();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                characterController = player.GetComponent<CharacterController>();
            }
        }
        else
        {
            characterController = playerTransform.GetComponent<CharacterController>();
        }

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
        if (debugCollisions && isCollisionActive)
        {
            string attackType = isHardAttack ? "HARD ATTACK (AOE)" : "Normal";
            Debug.Log($"⚔️ COLLISION ACTIVE - Waiting for hits... (Damage: {currentAttackDamage}, Type: {attackType})");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showAOEDebugGizmos || playerTransform == null) return;

        // Draw AOE radius for hard attacks
        if (isHardAttack && isCollisionActive)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, hardAttackAOERadius);
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
        Debug.Log($"✅ Sword collision handler initialized with TAG system (Enemy Tag: '{enemyTag}')");
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
        aoeHitTargets.Clear();
        swordCollider.enabled = true;

        string attackType = hardAttack ? "HARD ATTACK (AOE)" : "Normal Attack";
        Debug.Log($"⚔️ COLLISION ENABLED - Damage: {damage} ({attackType})");

        CheckAndSpawnSwingVFX();
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
            Debug.Log($"🛡️ COLLISION DISABLED - Hit {hitTargetsThisAttack.Count} direct targets, {aoeHitTargets.Count} AOE targets");
        }
        else
        {
            Debug.Log($"🛡️ COLLISION DISABLED - No hits detected");
        }

        hitTargetsThisAttack.Clear();
        aoeHitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isCollisionActive) return;
        if (hitTargetsThisAttack.Contains(other)) return;

        // ✅ CHECK TAG INSTEAD OF LAYER
        bool isEnemy = other.CompareTag(enemyTag);

        if (isEnemy)
        {
            hitTargetsThisAttack.Add(other);
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDirection = (hitPoint - transform.position).normalized;
            HandleEnemyHit(other, hitPoint, hitDirection);
        }
        else if (spawnVFXOnAllCollisions)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDirection = (hitPoint - transform.position).normalized;
            HandleGeneralCollision(other, hitPoint, hitDirection);
        }
    }

    private void ApplyHardAttackAOE()
    {
        if (!isHardAttack || playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;

        // ✅ FIND ALL ENEMIES BY TAG
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag(enemyTag);

        Debug.Log($"💥 HARD ATTACK AOE: Found {allEnemies.Length} enemies with tag '{enemyTag}'");

        foreach (GameObject enemyObj in allEnemies)
        {
            if (enemyObj == null) continue;

            float distance = Vector3.Distance(playerPos, enemyObj.transform.position);

            // Check if within AOE radius
            if (distance > hardAttackAOERadius) continue;

            Collider enemyCol = enemyObj.GetComponent<Collider>();
            if (enemyCol == null) continue;

            EnemyHealthController enemyHealth = enemyObj.GetComponent<EnemyHealthController>();
            if (enemyHealth == null) continue;
            if (enemyHealth.GetHealth() <= 0) continue;

            bool isDirectHit = hitTargetsThisAttack.Contains(enemyCol);

            if (isDirectHit)
            {
                Debug.Log($"⚔️ Direct sword hit on {enemyObj.name} - {hardAttackDirectDamage} damage");
            }
            else
            {
                aoeHitTargets.Add(enemyCol);

                enemyHealth.ApplyDamage(hardAttackAOEDamage);
                Debug.Log($"🌊 AOE damage to {enemyObj.name} - {hardAttackAOEDamage} damage (Distance: {distance:F2}m)");

                if (enableKnockback)
                {
                    EnemyKnockback knockback = enemyObj.GetComponent<EnemyKnockback>();
                    if (knockback != null)
                    {
                        knockback.ApplyHardKnockback(playerPos, playerTransform.forward);
                    }
                }

                Vector3 hitPos = enemyCol.ClosestPoint(playerPos);
                Vector3 hitDir = (hitPos - playerPos).normalized;
                SpawnContactVFXAlongBlade(enemyContactVFXPrefabs, hitPos, hitDir);
            }
        }

        if (useFS_CameraShakeBridgeOnHit && FS_FS_CameraShakeBridgeBridge.Instance != null)
        {
            FS_FS_CameraShakeBridgeBridge.Instance.Shake(hardAttackShakeDuration, hardAttackShakeMagnitude, hitShakeRotation * 2f);
        }

        SpawnImpactWaveVFX(enemyImpactWaveVFXPrefabs);
    }

    private void PullPlayerTowardsEnemy(Vector3 enemyPosition, bool isFromHit)
    {
        if (!enablePlayerPull || playerTransform == null) return;

        float distanceToEnemy = Vector3.Distance(playerTransform.position, enemyPosition);

        if (distanceToEnemy > minDistanceToEnemy)
        {
            float pullDistance = isHardAttack ? hardPullDistance : normalPullDistance;
            Vector3 directionToEnemy = (enemyPosition - playerTransform.position).normalized;

            directionToEnemy.y = 0;
            directionToEnemy.Normalize();

            Vector3 moveVector = directionToEnemy * pullDistance;

            if (characterController != null)
            {
                characterController.Move(moveVector);
                Debug.Log($"🧲 Pulled player {pullDistance}m towards enemy using CharacterController");
            }
            else
            {
                Vector3 targetPosition = playerTransform.position + moveVector;
                playerTransform.position = Vector3.Lerp(playerTransform.position, targetPosition, pullSpeed * Time.deltaTime);
                Debug.Log($"🧲 Pulled player {pullDistance}m towards enemy using Transform");
            }
        }
    }

    private void HandleEnemyHit(Collider enemyCollider, Vector3 hitPosition, Vector3 hitDirection)
    {
        string attackType = isHardAttack ? "HARD ATTACK (Direct)" : "normal attack";
        Debug.Log($"⚔️ SWORD HIT ENEMY: {enemyCollider.gameObject.name} with {attackType}!");

        EnemyHealthController enemyHealth = enemyCollider.GetComponent<EnemyHealthController>();
        bool enemyIsAlive = true;

        if (enemyHealth != null)
        {
            int damageToApply = isHardAttack ? hardAttackDirectDamage : currentAttackDamage;

            enemyHealth.ApplyDamage(damageToApply);
            Debug.Log($"💔 Dealt {damageToApply} damage to {enemyCollider.gameObject.name}");

            enemyIsAlive = enemyHealth.GetHealth() > 0;
        }
        else
        {
            Debug.LogWarning($"⚠️ {enemyCollider.gameObject.name} has tag '{enemyTag}' but no EnemyHealthController component!");
        }

        if (enemyIsAlive && pullOnEveryHit)
        {
            PullPlayerTowardsEnemy(enemyCollider.transform.position, true);
        }
        else if (debugCollisions && !enemyIsAlive)
        {
            Debug.Log("❌ Enemy died - skipping pull");
        }

        if (enableKnockback && playerTransform != null)
        {
            EnemyKnockback knockback = enemyCollider.GetComponent<EnemyKnockback>();
            if (knockback != null)
            {
                if (isHardAttack)
                {
                    knockback.ApplyHardKnockback(playerTransform.position, playerTransform.forward);
                    Debug.Log($"💥💥 Applied HARD knockback to {enemyCollider.gameObject.name}");
                }
                else
                {
                    knockback.ApplyNormalKnockback(playerTransform.position);
                    Debug.Log($"💥 Applied normal knockback to {enemyCollider.gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ {enemyCollider.gameObject.name} has no EnemyKnockback component!");
            }
        }

        if (useFS_CameraShakeBridgeOnHit && FS_FS_CameraShakeBridgeBridge.Instance != null)
        {
            FS_FS_CameraShakeBridgeBridge.Instance.Shake(hitShakeDuration, hitShakeMagnitude, hitShakeRotation);
        }

        if (audioManager != null)
        {
            audioManager.PlayHitSound();
        }

        SpawnContactVFXAlongBlade(enemyContactVFXPrefabs, hitPosition, hitDirection);
        SpawnImpactWaveVFX(enemyImpactWaveVFXPrefabs);
    }

    private void HandleGeneralCollision(Collider otherCollider, Vector3 hitPosition, Vector3 hitDirection)
    {
        if (debugCollisions)
            Debug.Log($"💥 SWORD HIT OBJECT: {otherCollider.gameObject.name}");

        if (spawnVFXOnAllCollisions)
        {
            SpawnContactVFXAlongBlade(generalContactVFXPrefabs, hitPosition, hitDirection);
            SpawnImpactWaveVFX(generalImpactWaveVFXPrefabs);

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
            SpawnSingleVFX(vfxArray, hitPosition, hitDirection, vfxSizeMultiplier, contactVFXLifetime);
            return;
        }

        Vector3 tipPos = swordTipTransform.position;
        Vector3 bottomPos = swordBottomTransform.position;
        Vector3 bladeDirection = (tipPos - bottomPos).normalized;
        float bladeLength = Vector3.Distance(tipPos, bottomPos);

        if (bladeDirection.magnitude < 0.01f)
        {
            bladeDirection = Vector3.up;
        }

        float vfxScale = scaleVFXToSwordLength ? (bladeLength / vfxSpawnCount) * vfxSizeMultiplier : vfxSizeMultiplier;

        int randomIndex = Random.Range(0, vfxArray.Length);
        GameObject vfxPrefab = vfxArray[randomIndex];

        if (vfxPrefab == null) return;

        for (int i = 0; i < vfxSpawnCount; i++)
        {
            float t = vfxSpawnCount > 1 ? (float)i / (vfxSpawnCount - 1) : 0.5f;
            Vector3 spawnPosition = Vector3.Lerp(bottomPos, tipPos, t);

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

        Vector3 point1 = swordTipTransform.position;
        Vector3 point2 = swordBottomTransform.position;
        float radius = 0.5f;

        Collider[] hitColliders = Physics.OverlapCapsule(point1, point2, radius);

        foreach (Collider col in hitColliders)
        {
            if (col == null || col.gameObject == null) continue;

            // ✅ CHECK TAG
            if (!col.CompareTag(enemyTag)) continue;

            if (hitTargetsThisAttack.Contains(col)) continue;

            EnemyHealthController health = col.GetComponent<EnemyHealthController>();
            if (health != null && health.GetHealth() <= 0) continue;

            hitTargetsThisAttack.Add(col);

            Vector3 hitPoint = col.ClosestPoint(point1);
            Vector3 hitDirection = (hitPoint - transform.position).normalized;

            Debug.Log($"💥 HIT DETECTED VIA EVENT on {col.name}! Spawning Hit VFX.");

            bool enemyIsAlive = true;

            if (health != null)
            {
                int damageToApply = isHardAttack ? hardAttackDirectDamage : damage;

                health.ApplyDamage(damageToApply);
                enemyIsAlive = health.GetHealth() > 0;
            }

            if (enemyIsAlive && pullOnEveryHit)
            {
                PullPlayerTowardsEnemy(col.transform.position, true);
            }

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

            if (useFS_CameraShakeBridgeOnHit && CameraNewShake.Instance != null)
            {
                CameraNewShake.Instance.Shake(hitShakeDuration, hitShakeMagnitude, hitShakeRotation);
            }

            if (audioManager != null)
            {
                audioManager.PlayHitSound();
            }

            SpawnContactVFXAlongBlade(enemyContactVFXPrefabs, hitPoint, hitDirection);
            SpawnImpactWaveVFX(enemyImpactWaveVFXPrefabs);
        }

        if (isHardAttack)
        {
            ApplyHardAttackAOE();
        }
    }

    public void ForceInstantHitCheck(int damage)
    {
        Vector3 center = (swordTipTransform.position + swordBottomTransform.position) / 2f;
        float radius = Vector3.Distance(swordTipTransform.position, swordBottomTransform.position) / 2f;

        Collider[] hitEnemies = Physics.OverlapSphere(center, radius + 0.5f);

        foreach (Collider col in hitEnemies)
        {
            // ✅ CHECK TAG
            if (!col.CompareTag(enemyTag)) continue;

            if (hitTargetsThisAttack.Contains(col)) continue;

            hitTargetsThisAttack.Add(col);

            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 direction = (hitPoint - center).normalized;

            Debug.Log($"🎯 EVENT HIT: Found enemy {col.name}. Triggering HIT VFX!");

            HandleEnemyHit(col, hitPoint, direction);
        }

        if (isHardAttack)
        {
            ApplyHardAttackAOE();
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
            Vector3 safeDirection = hitDirection.magnitude > 0.01f ? hitDirection : Vector3.forward;
            Quaternion vfxRotation = Quaternion.LookRotation(-safeDirection);
            GameObject vfx = Instantiate(vfxPrefab, hitPosition, vfxRotation);
            Destroy(vfx, genericHitVFXLifetime);

            if (debugCollisions)
                Debug.Log($"✨ Spawned generic hit VFX at collision point");
        }
    }

    private void CheckAndSpawnSwingVFX()
    {
        if (swingVFXPrefabs == null || swingVFXPrefabs.Length == 0) return;

        bool enemiesNearby = false;

        if (swordTipTransform != null && swordBottomTransform != null)
        {
            Vector3 center = (swordTipTransform.position + swordBottomTransform.position) / 2f;

            // ✅ FIND ENEMIES BY TAG
            GameObject[] allEnemies = GameObject.FindGameObjectsWithTag(enemyTag);

            foreach (GameObject enemy in allEnemies)
            {
                if (enemy != null)
                {
                    float distance = Vector3.Distance(center, enemy.transform.position);
                    if (distance <= swingVFXDetectionRadius)
                    {
                        enemiesNearby = true;
                        break;
                    }
                }
            }

            if (debugCollisions && enemiesNearby)
            {
                Debug.Log($"👁️ Detected enemies in swing range!");
            }
        }

        if (enemiesNearby || spawnSwingVFXOnAllSwings)
        {
            SpawnSwingVFX();
        }
    }

    private void SpawnSwingVFX()
    {
        if (swingVFXPrefabs == null || swingVFXPrefabs.Length == 0) return;

        Transform spawnPoint = swingVFXSpawnPoint != null ? swingVFXSpawnPoint : swordTipTransform;

        if (spawnPoint == null)
        {
            Debug.LogWarning("⚠️ No spawn point for swing VFX. Assign swingVFXSpawnPoint or swordTipTransform.");
            return;
        }

        int randomIndex = Random.Range(0, swingVFXPrefabs.Length);
        GameObject vfxPrefab = swingVFXPrefabs[randomIndex];

        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, spawnPoint.position, spawnPoint.rotation);
            vfx.transform.SetParent(spawnPoint);
            Destroy(vfx, swingVFXLifetime);

            if (debugCollisions)
                Debug.Log($"💨 Spawned swing VFX: {vfxPrefab.name}");
        }
    }

    public void TriggerSwingVFX()
    {
        CheckAndSpawnSwingVFX();
    }

    public bool IsCollisionActive => isCollisionActive;
    public int CurrentDamage => currentAttackDamage;
    public int HitCountThisAttack => hitTargetsThisAttack.Count;
}