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

    [Header("Combat Settings")]
    public Transform playerTarget;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;

    [Header("Equipment")]
    public bool hasTorch = false;
    public int torchLayerIndex = 1; // Assuming Torch Override is on Layer 1

    [Header("Movement Settings")]
    public float walkingSpeed = 1f;
    public float runningSpeed = 3f;
    public Transform[] patrolPoints;
    public Transform guardPoint;

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;

    private EnemyBaseState _currentState;

    // State Instances
    public readonly EnemyStartState StartState = new EnemyStartState();
    public readonly EnemyAgroedState AgroedState = new EnemyAgroedState();
    public readonly EnemyDeathState DeathState = new EnemyDeathState();

    private void Start()
    {
        // Initialize Torch Layer Weight
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

    // Helper to safely set layer weight
    public void SetTorchLayerWeight(float weight)
    {
        if (animator != null && torchLayerIndex < animator.layerCount)
        {
            animator.SetLayerWeight(torchLayerIndex, weight);
        }
    }
}