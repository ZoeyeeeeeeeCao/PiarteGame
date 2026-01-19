using UnityEngine;
using UnityEngine.AI;

public enum EnemyType
{
    Patrolling,
    Guarding,
    Spawning
}

/// <summary>
/// Main controller for Enemy AI. 
/// Handles State transitions, NavMesh navigation, and Combat toggles.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Settings")]
    public EnemyType initialType;

    [Header("Sensors & Detection")]
    public float viewRadius = 15f;
    [Range(0, 360)]
    public float viewAngle = 60f;
    public float attackSensorRange = 2f;
    public LayerMask obstacleMask;
    public Transform playerTarget;

    [Header("Combat Settings")]
    public float attackCooldown = 2f;
    public bool hasTorch = false;
    public int torchLayerIndex = 1;

    [Header("Movement Settings")]
    public float walkingSpeed = 1.5f;
    public float runningSpeed = 4f;
    public Transform[] patrolPoints;
    public Transform guardPoint;

    [Header("References")]
    public Animator animator;
    public AnimatorOverrideController enemyAnimationOverride;
    public NavMeshAgent agent;
    public EnemyHealthController healthController;

    [Header("Combat References")]
    public TrailRenderer attackTrail;
    public GameObject swordHitboxObject; // The child object with the collider

    private EnemyBaseState _currentState;
    [HideInInspector] public bool isSpawning = false;

    // State Instances
    public readonly EnemyStartState StartState = new EnemyStartState();
    public readonly EnemyAgroedState AgroedState = new EnemyAgroedState();
    public readonly EnemyDeathState DeathState = new EnemyDeathState();
    public readonly EnemyDamageState DamageState = new EnemyDamageState();

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (healthController == null) healthController = GetComponent<EnemyHealthController>();

        // Initial hard-disable of combat visuals
        DisableCombat();
    }

    private void Start()
    {
        // 1. Apply the Animator Override if one is assigned
        if (animator != null && enemyAnimationOverride != null)
        {
            animator.runtimeAnimatorController = enemyAnimationOverride;
        }

        // 2. Setup Animation Layers
        SetTorchLayerWeight(hasTorch ? 1f : 0f);

        // 3. Initialize State
        TransitionToState(StartState);
    }

    private void Update()
    {
        _currentState?.UpdateState(this);

        // Update Animator Speed parameter based on NavMesh velocity
        if (animator != null && agent != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }
    }

    public void TransitionToState(EnemyBaseState newState)
    {
        // Don't allow transitions if we are already dead
        if (_currentState == DeathState) return;

        _currentState?.ExitState(this);
        _currentState = newState;
        _currentState.EnterState(this);
    }

    /// <summary>
    /// Function to be called by Animation Events.
    /// active = 1 (Enable), active = 0 (Disable)
    /// </summary>
    public void SetCombatActive(int active)
    {
        bool isActive = (active != 0);

        if (swordHitboxObject != null)
            swordHitboxObject.SetActive(isActive);

        if (attackTrail != null)
            attackTrail.emitting = isActive;
    }

    public void DisableCombat()
    {
        if (swordHitboxObject != null) swordHitboxObject.SetActive(false);
        if (attackTrail != null) attackTrail.emitting = false;
    }

    public void TakeDamage() => TransitionToState(DamageState);
    public void Die() => TransitionToState(DeathState);

    public void SetTorchLayerWeight(float weight)
    {
        if (animator != null && torchLayerIndex < animator.layerCount)
        {
            animator.SetLayerWeight(torchLayerIndex, weight);
        }
    }

    public void ToggleNavMesh(bool enable)
    {
        if (agent == null) return;

        if (enable)
        {
            if (!agent.enabled && agent.isOnNavMesh)
                agent.enabled = true;

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }
        else
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.ResetPath();
                agent.updateRotation = false;
            }
        }
    }

    public bool CanSeePlayer()
    {
        if (playerTarget == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > viewRadius) return false;

        Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
        {
            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackSensorRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
}