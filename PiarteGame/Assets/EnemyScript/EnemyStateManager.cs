using UnityEngine;
using UnityEngine.AI;

public enum EnemyType
{
    Patrolling,
    Guarding,
    Spawning
}

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
    public float walkingSpeed = 1f;
    public float runningSpeed = 3f;
    public Transform[] patrolPoints;
    public Transform guardPoint;

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public EnemyHealthController healthController;

    private EnemyBaseState _currentState;
    public bool isSpawning = false;

    // State Instances
    public readonly EnemyStartState StartState = new EnemyStartState();
    public readonly EnemyAgroedState AgroedState = new EnemyAgroedState();
    public readonly EnemyDeathState DeathState = new EnemyDeathState();
    public readonly EnemyDamageState DamageState = new EnemyDamageState();

    private void Awake()
    {
        if (healthController == null)
            healthController = GetComponent<EnemyHealthController>();
    }

    private void Start()
    {
        SetTorchLayerWeight(hasTorch ? 1f : 0f);
        TransitionToState(StartState);
    }

    private void Update()
    {
        _currentState?.UpdateState(this);
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
    /// Called by the Health Controller when damage is taken but the enemy is still alive.
    /// </summary>
    public void TakeDamage()
    {
        TransitionToState(DamageState);
    }

    /// <summary>
    /// Called by the Health Controller when health reaching zero.
    /// </summary>
    public void Die()
    {
        TransitionToState(DeathState);
    }

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

        Gizmos.color = Color.blue;
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}