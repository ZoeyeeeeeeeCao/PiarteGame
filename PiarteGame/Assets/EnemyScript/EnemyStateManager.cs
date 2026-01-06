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
    public float viewRadius = 15f;      // "Sensor in 15f distance" / Chase distance
    [Range(0, 360)]
    public float viewAngle = 60f;       // "FOV of 60"
    public float attackSensorRange = 2f; // "Sensor of 2f"

    public LayerMask obstacleMask;       // To block raycasts (Walls)
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

    private EnemyBaseState _currentState;
    public bool isSpawning = false;

    // State Instances
    public readonly EnemyStartState StartState = new EnemyStartState();
    public readonly EnemyAgroedState AgroedState = new EnemyAgroedState();
    public readonly EnemyDeathState DeathState = new EnemyDeathState();

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
        _currentState?.ExitState(this);
        _currentState = newState;
        _currentState.EnterState(this);
    }

    public void SetTorchLayerWeight(float weight)
    {
        if (animator != null && torchLayerIndex < animator.layerCount)
        {
            animator.SetLayerWeight(torchLayerIndex, weight);
        }
    }

    //// Helper to safely enable/disable NavMeshAgent
    public void ToggleNavMesh(bool enable)
    {
        if (agent == null) return;

        if (enable)
        {
            // Only enable if the agent is on a NavMesh
            if (!agent.enabled && agent.isOnNavMesh)
            {
                agent.enabled = true;
            }

            // Start movement if agent is enabled and on NavMesh
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true;  // Enable NavMesh rotation control
            }
        }
        else
        {
            // Stop movement but keep rotation active
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.velocity = Vector3.zero;    // Kill momentum instantly
                agent.isStopped = true;            // Stop movement
                agent.ResetPath();                 // Clear the current path
                agent.updateRotation = false;      // Disable NavMesh rotation (manual control)
            }

            // Keep agent ENABLED so rotation can still be controlled manually
            // agent.enabled = false; // REMOVED - keep enabled for manual rotation
        }
    }

    // --- DETECTION LOGIC ---
    public bool CanSeePlayer()
    {
        if (playerTarget == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 1. Check Distance (15f Sensor)
        if (distanceToPlayer > viewRadius)
            return false;

        // 2. Check Angle (60 deg FOV)
        Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
        {
            // 3. Raycast (Check for walls)
            // Raycast from slightly up (eye level) to player center
            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstacleMask))
            {
                return true; // Player is seen
            }
        }

        return false;
    }

    // --- VISUALIZATION ---
    private void OnDrawGizmosSelected()
    {
        // 1. Attack Sensor (Red Sphere)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackSensorRange);

        // 2. Chase Sensor / View Radius (Yellow Sphere)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // 3. Field of View (Blue Lines)
        Gizmos.color = Color.blue;
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}