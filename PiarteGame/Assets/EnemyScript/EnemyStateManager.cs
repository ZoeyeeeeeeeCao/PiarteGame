using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyStateManager : MonoBehaviour
{
    // 1. Define the options for the dropdown
    public enum EnemyType
    {
        Patrolling,
        Spawning
    }

    [Header("Initial Settings")]
    public EnemyType startType;

    public EnemyBaseState currentState;

    public EnemySpawnState spawnState = new EnemySpawnState();
    public EnemyPatrolState patrolState = new EnemyPatrolState();
    public EnemyChaseState chaseState = new EnemyChaseState();
    public EnemyAttackState attackState = new EnemyAttackState();
    public EnemyDeathState deathState = new EnemyDeathState();

    void Start()
    {
        switch (startType)
        {
            case EnemyType.Patrolling:
                currentState = patrolState;
                break;
            case EnemyType.Spawning:
                currentState = spawnState;
                break;
        }

        if (currentState != null)
        {
            currentState.EnterState(this);
        }
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.UpdateState(this);
        }
    }

    public void SwitchState(EnemyBaseState state)
    {
        currentState = state;
        state.EnterState(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != null)
        {
           // currentState.OnCollisionEnter(this, collision);
        }
    }
}