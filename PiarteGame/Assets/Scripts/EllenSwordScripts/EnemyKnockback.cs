using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Handles knockback effects for enemies when hit by player attacks.
/// NavMesh-friendly version that works with NavMeshAgent.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback Settings")]
    [Tooltip("Force applied for normal attacks")]
    [SerializeField] private float normalKnockbackForce = 2f;

    [Tooltip("Force applied for hard attacks")]
    [SerializeField] private float hardKnockbackForce = 5f;

    [Tooltip("How long the knockback lasts")]
    [SerializeField] private float knockbackDuration = 0.2f;

    [Tooltip("Use smooth knockback instead of instant")]
    [SerializeField] private bool useSmoothKnockback = true;

    [Header("NavMesh Settings")]
    [Tooltip("Temporarily disable NavMesh during knockback")]
    [SerializeField] private bool disableNavMeshDuringKnockback = true;

    [Tooltip("Time to wait before re-enabling NavMesh")]
    [SerializeField] private float navMeshReEnableDelay = 0.3f;

    [Header("Limits")]
    [Tooltip("Maximum knockback distance")]
    [SerializeField] private float maxKnockbackDistance = 3f;

    [Tooltip("Prevent knockback if already being knocked back")]
    [SerializeField] private bool preventOverlappingKnockback = true;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // References
    private NavMeshAgent agent;
    private EnemyController enemyController;
    private bool isBeingKnockedBack = false;
    private Coroutine knockbackCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyController = GetComponent<EnemyController>();
    }

    /// <summary>
    /// Apply normal knockback (for regular attacks)
    /// </summary>
    public void ApplyNormalKnockback(Vector3 sourcePosition)
    {
        ApplyKnockback(sourcePosition, normalKnockbackForce, Vector3.zero);
    }

    /// <summary>
    /// Apply hard knockback with direction (for hard attacks)
    /// </summary>
    public void ApplyHardKnockback(Vector3 sourcePosition, Vector3 forwardDirection)
    {
        ApplyKnockback(sourcePosition, hardKnockbackForce, forwardDirection);
    }

    /// <summary>
    /// Main knockback logic
    /// </summary>
    private void ApplyKnockback(Vector3 sourcePosition, float force, Vector3 optionalDirection)
    {
        // Prevent overlapping knockbacks
        if (preventOverlappingKnockback && isBeingKnockedBack)
        {
            if (debugMode)
                Debug.Log($"⚠️ {gameObject.name}: Already being knocked back, ignoring new knockback");
            return;
        }

        // Stop any existing knockback
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        // Calculate knockback direction
        Vector3 knockbackDirection;
        if (optionalDirection != Vector3.zero)
        {
            // Use provided direction (for hard attacks)
            knockbackDirection = optionalDirection.normalized;
        }
        else
        {
            // Calculate from source position (for normal attacks)
            knockbackDirection = (transform.position - sourcePosition).normalized;
        }

        // Keep knockback horizontal (no flying enemies!)
        knockbackDirection.y = 0;

        if (debugMode)
        {
            Debug.Log($"💥 {gameObject.name}: Applying knockback! Force: {force}, Direction: {knockbackDirection}");
        }

        // Start knockback coroutine
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(knockbackDirection, force));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        isBeingKnockedBack = true;

        // Disable NavMeshAgent if configured
        bool wasNavMeshEnabled = agent.enabled;
        if (disableNavMeshDuringKnockback && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();

            if (debugMode)
                Debug.Log($"🚫 {gameObject.name}: NavMesh disabled for knockback");
        }

        // Disable enemy AI during knockback
        if (enemyController != null)
        {
            enemyController.ToggleNavMesh(false);
        }

        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (direction * force);

        // Clamp to max distance
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (distance > maxKnockbackDistance)
        {
            targetPosition = startPosition + (direction * maxKnockbackDistance);
        }

        // Check if target position is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, maxKnockbackDistance, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"⚠️ {gameObject.name}: Knockback target is off NavMesh, using original position");
            targetPosition = startPosition;
        }

        if (useSmoothKnockback)
        {
            // Smooth knockback
            while (elapsed < knockbackDuration)
            {
                float t = elapsed / knockbackDuration;
                // Use ease-out curve for natural deceleration
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Instant knockback
            transform.position = targetPosition;
            yield return new WaitForSeconds(knockbackDuration);
        }

        // Ensure final position
        transform.position = targetPosition;

        if (debugMode)
        {
            Debug.Log($"✅ {gameObject.name}: Knockback complete. Moved {Vector3.Distance(startPosition, transform.position):F2}m");
        }

        // Wait a bit before re-enabling NavMesh
        yield return new WaitForSeconds(navMeshReEnableDelay);

        // Re-enable NavMeshAgent
        if (disableNavMeshDuringKnockback && wasNavMeshEnabled)
        {
            // Make sure we're on the NavMesh
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;

                if (debugMode)
                    Debug.Log($"✅ {gameObject.name}: NavMesh re-enabled");
            }
            else
            {
                Debug.LogError($"❌ {gameObject.name}: Not on NavMesh after knockback!");
            }
        }

        // Re-enable enemy AI
        if (enemyController != null)
        {
            enemyController.ToggleNavMesh(true);
        }

        isBeingKnockedBack = false;
        knockbackCoroutine = null;
    }

    /// <summary>
    /// Stop any ongoing knockback (useful for death, etc.)
    /// </summary>
    public void CancelKnockback()
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        isBeingKnockedBack = false;

        // Re-enable NavMesh
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (enemyController != null)
        {
            enemyController.ToggleNavMesh(true);
        }
    }

    private void OnDrawGizmos()
    {
        if (isBeingKnockedBack)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    public bool IsBeingKnockedBack => isBeingKnockedBack;
}