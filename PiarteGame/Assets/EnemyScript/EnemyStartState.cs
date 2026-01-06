using UnityEngine;

public class EnemyStartState : EnemyBaseState
{
    private EnemyBaseState _currentSubState;

    // Sub-state instances
    private readonly EnemyPatrolState _patrolState = new EnemyPatrolState();
    private readonly EnemyIdleState _idleState = new EnemyIdleState();
    private readonly EnemySpawnState _spawnState = new EnemySpawnState();

    public override void EnterState(EnemyController enemy)
    {
        Debug.Log("Entering Start SuperState");

        // Decide sub-state based on Inspector Enum
        switch (enemy.initialType)
        {
            case EnemyType.Patrolling:
                SetSubState(enemy, _patrolState);
                break;
            case EnemyType.Guarding:
                SetSubState(enemy, _idleState);
                break;
            case EnemyType.Spawning:
                SetSubState(enemy, _spawnState);
                break;
        }
    }

    public override void UpdateState(EnemyController enemy)
    {
        // Run the logic of the active sub-state
        _currentSubState?.UpdateState(enemy);

        // -- Global Transition Logic for this SuperState --

        // Example: Transition to Agro if player is close
        // logic: if (Vector3.Distance(enemy.transform.position, player.position) < visionRange)
        // {
        //     enemy.TransitionToState(enemy.AgroedState);
        // }

        // Example: Transition to Death if health is low
        // logic: if (health <= 0) enemy.TransitionToState(enemy.DeathState);
    }

    public override void ExitState(EnemyController enemy)
    {
        _currentSubState?.ExitState(enemy);
    }

    private void SetSubState(EnemyController enemy, EnemyBaseState subState)
    {
        _currentSubState?.ExitState(enemy);
        _currentSubState = subState;
        _currentSubState.EnterState(enemy);
    }
}