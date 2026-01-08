using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles directional slash VFX that spawn when enemies are in range
/// Attach this to the SWORD prefab alongside SwordCollisionHandler
/// </summary>
public class SwordSlashVFXSystem : MonoBehaviour
{
    [Header("Slash VFX Settings")]
    [Tooltip("Slash effect prefabs (should face forward Z-axis)")]
    [SerializeField] private GameObject[] slashVFXPrefabs;
    [SerializeField] private float slashVFXLifetime = 0.6f;
    [SerializeField] private float slashVFXScale = 1f;
    [SerializeField] private Vector3 slashSpawnOffset = Vector3.zero;

    [Header("Spawn Location")]
    [SerializeField] private SlashSpawnLocation spawnLocation = SlashSpawnLocation.Middle;

    public enum SlashSpawnLocation
    {
        Tip,      // Spawn at sword tip
        Base,     // Spawn at sword base
        Middle,   // Spawn at middle of blade
        Multiple  // Spawn at multiple points along blade
    }

    [Header("Timing")]
    [Tooltip("Delay before spawning slash VFX (to sync with sword movement)")]
    [SerializeField] private float slashSpawnDelay = 0.15f;
    [Tooltip("How many frames to track for direction calculation")]
    [SerializeField] private int directionTrackingFrames = 5;

    [Header("Enemy Detection")]
    [SerializeField] private float detectionRadius = 3f;
    [Tooltip("Only spawn slash if enemy is in front of player")]
    [SerializeField] private bool onlySpawnWhenEnemyInFront = true;
    [Tooltip("Angle in front of player to consider 'likely to hit'")]
    [SerializeField] private float likelyHitAngle = 90f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Transform swordTipTransform;
    [SerializeField] private Transform swordBaseTransform;
    [SerializeField] private Transform playerTransform;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showDetectionGizmos = true;

    // Position tracking
    private Queue<Vector3> tipPositionHistory = new Queue<Vector3>();
    private Queue<Vector3> basePositionHistory = new Queue<Vector3>();

    // State
    private Coroutine slashSpawnCoroutine = null;

    private void Start()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (slashVFXPrefabs == null || slashVFXPrefabs.Length == 0)
        {
            Debug.LogWarning("⚠️ No slash VFX prefabs assigned to SwordSlashVFXSystem!");
        }

        if (swordTipTransform == null || swordBaseTransform == null)
        {
            Debug.LogError("❌ Sword tip/base transforms not assigned to SwordSlashVFXSystem!");
        }

        if (debugMode)
        {
            Debug.Log("✅ Slash VFX System initialized");
        }
    }

    private void Update()
    {
        // Continuously track sword position for accurate direction calculation
        if (swordTipTransform != null && swordBaseTransform != null)
        {
            tipPositionHistory.Enqueue(swordTipTransform.position);
            basePositionHistory.Enqueue(swordBaseTransform.position);

            // Keep only the last N frames
            while (tipPositionHistory.Count > directionTrackingFrames)
            {
                tipPositionHistory.Dequeue();
                basePositionHistory.Dequeue();
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDetectionGizmos || swordTipTransform == null || swordBaseTransform == null)
            return;

        // Draw detection radius
        Vector3 center = (swordTipTransform.position + swordBaseTransform.position) / 2f;
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(center, detectionRadius);
    }

    /// <summary>
    /// Call this when an attack starts to trigger slash VFX
    /// </summary>
    public void TriggerSlashCheck()
    {
        if (slashVFXPrefabs == null || slashVFXPrefabs.Length == 0)
            return;

        if (slashSpawnCoroutine != null)
        {
            StopCoroutine(slashSpawnCoroutine);
        }

        slashSpawnCoroutine = StartCoroutine(DelayedSlashSpawn());
    }

    /// <summary>
    /// Cancels any pending slash spawns
    /// </summary>
    public void CancelSlashSpawn()
    {
        if (slashSpawnCoroutine != null)
        {
            StopCoroutine(slashSpawnCoroutine);
            slashSpawnCoroutine = null;
        }
    }

    private IEnumerator DelayedSlashSpawn()
    {
        // Wait for the sword to start moving
        yield return new WaitForSeconds(slashSpawnDelay);

        // Check if enemies are in range and spawn slash
        if (CheckForEnemiesInRange())
        {
            SpawnSlashVFX();
        }

        slashSpawnCoroutine = null;
    }

    private bool CheckForEnemiesInRange()
    {
        if (swordTipTransform == null || swordBaseTransform == null)
            return false;

        Vector3 swordCenter = (swordTipTransform.position + swordBaseTransform.position) / 2f;

        // Find enemies in range
        Collider[] nearbyEnemies = Physics.OverlapSphere(swordCenter, detectionRadius, enemyLayer);

        if (nearbyEnemies.Length == 0)
        {
            if (debugMode)
                Debug.Log("❌ No enemies in slash detection range");
            return false;
        }

        // Filter for enemies that are likely to be hit (in front of player)
        if (onlySpawnWhenEnemyInFront && playerTransform != null)
        {
            int validTargets = 0;
            Vector3 playerForward = playerTransform.forward;

            foreach (Collider enemy in nearbyEnemies)
            {
                if (enemy == null || enemy.gameObject == null) continue;

                // Check if enemy is alive
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null && health.IsDead) continue;

                Vector3 toEnemy = (enemy.transform.position - playerTransform.position).normalized;
                float angle = Vector3.Angle(playerForward, toEnemy);

                if (angle <= likelyHitAngle / 2f)
                {
                    validTargets++;
                }
            }

            if (validTargets == 0)
            {
                if (debugMode)
                    Debug.Log($"❌ {nearbyEnemies.Length} enemies nearby but none in front");
                return false;
            }

            if (debugMode)
                Debug.Log($"✅ {validTargets} enemies likely to be hit - spawning slash!");

            return true;
        }

        // If we're not checking front direction, any enemy in range is valid
        if (debugMode)
            Debug.Log($"✅ {nearbyEnemies.Length} enemies in range - spawning slash!");

        return true;
    }

    private void SpawnSlashVFX()
    {
        if (slashVFXPrefabs == null || slashVFXPrefabs.Length == 0) return;
        if (swordTipTransform == null || swordBaseTransform == null) return;

        Vector3 currentTipPos = swordTipTransform.position;
        Vector3 currentBasePos = swordBaseTransform.position;
        Vector3 bladeDirection = (currentTipPos - currentBasePos).normalized;

        // Calculate swing direction from position history
        Vector3 swingDirection = CalculateSwingDirection();

        // Ensure valid directions
        if (bladeDirection.magnitude < 0.01f)
        {
            bladeDirection = Vector3.up;
        }

        if (swingDirection.magnitude < 0.01f)
        {
            // Fallback to perpendicular to blade if no movement detected
            swingDirection = Vector3.Cross(bladeDirection, Vector3.up);
            if (swingDirection.magnitude < 0.01f)
            {
                swingDirection = Vector3.Cross(bladeDirection, Vector3.right);
            }
            swingDirection.Normalize();
        }

        if (debugMode)
        {
            Debug.Log($"⚡ Slash Direction: {swingDirection}, Blade Direction: {bladeDirection}");
            Debug.DrawRay(currentTipPos, swingDirection * 2f, Color.red, 2f);
            Debug.DrawRay(currentTipPos, bladeDirection * 1f, Color.blue, 2f);
        }

        // Select random slash VFX
        int randomIndex = Random.Range(0, slashVFXPrefabs.Length);
        GameObject slashPrefab = slashVFXPrefabs[randomIndex];

        if (slashPrefab == null) return;

        // Determine spawn position(s) based on location setting
        List<Vector3> spawnPositions = new List<Vector3>();

        switch (spawnLocation)
        {
            case SlashSpawnLocation.Tip:
                spawnPositions.Add(currentTipPos + slashSpawnOffset);
                break;
            case SlashSpawnLocation.Base:
                spawnPositions.Add(currentBasePos + slashSpawnOffset);
                break;
            case SlashSpawnLocation.Middle:
                spawnPositions.Add((currentTipPos + currentBasePos) / 2f + slashSpawnOffset);
                break;
            case SlashSpawnLocation.Multiple:
                spawnPositions.Add(currentBasePos + slashSpawnOffset);
                spawnPositions.Add((currentTipPos + currentBasePos) / 2f + slashSpawnOffset);
                spawnPositions.Add(currentTipPos + slashSpawnOffset);
                break;
        }

        // Spawn slash VFX at each position
        foreach (Vector3 spawnPos in spawnPositions)
        {
            // Create rotation: slash faces the swing direction, oriented along blade
            Quaternion slashRotation = Quaternion.LookRotation(swingDirection, bladeDirection);

            GameObject slash = Instantiate(slashPrefab, spawnPos, slashRotation);
            slash.transform.localScale = Vector3.one * slashVFXScale;

            Destroy(slash, slashVFXLifetime);

            if (debugMode)
                Debug.Log($"⚡ Spawned slash VFX at {spawnLocation}");
        }
    }

    private Vector3 CalculateSwingDirection()
    {
        if (tipPositionHistory.Count < 2 || basePositionHistory.Count < 2)
        {
            return Vector3.zero;
        }

        // Get oldest and newest positions
        Vector3[] tipArray = tipPositionHistory.ToArray();
        Vector3[] baseArray = basePositionHistory.ToArray();

        Vector3 oldTipPos = tipArray[0];
        Vector3 newTipPos = tipArray[tipArray.Length - 1];
        Vector3 oldBasePos = baseArray[0];
        Vector3 newBasePos = baseArray[baseArray.Length - 1];

        // Calculate movement vectors
        Vector3 tipMovement = newTipPos - oldTipPos;
        Vector3 baseMovement = newBasePos - oldBasePos;

        // Average the movements for more stable direction
        Vector3 averageMovement = (tipMovement + baseMovement) / 2f;

        // Project onto horizontal plane (XZ) for more consistent slashes
        averageMovement.y *= 0.3f; // Reduce vertical component

        return averageMovement.normalized;
    }

    // Public properties
    public bool IsSpawningSlash => slashSpawnCoroutine != null;
}