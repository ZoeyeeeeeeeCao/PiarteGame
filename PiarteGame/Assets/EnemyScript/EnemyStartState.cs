using UnityEngine;

public class EnemyStartState : EnemyBaseState
{
    private EnemyBaseState _currentSubState;

    private readonly EnemyPatrolState _patrolState = new EnemyPatrolState();
    private readonly EnemyIdleState _idleState = new EnemyIdleState();
    private readonly EnemySpawnState _spawnState = new EnemySpawnState();

    public override void EnterState(EnemyController enemy)
    {
        Debug.Log("Entering Start SuperState");

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
        _currentSubState?.UpdateState(enemy);

        // --- IDLE / ANIMATION LOCK ---
        // If an animation tagged "Idle" is playing and hasn't finished, prevent transition.
        if (enemy.animator != null)
        {
            AnimatorStateInfo info = enemy.animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsTag("Idle") && info.normalizedTime < 1.0f)
            {
                return;
            }
        }

        // --- DETECTION LOGIC ---
        if (enemy.CanSeePlayer() && !enemy.isSpawning)
        {
            enemy.TransitionToState(enemy.AgroedState);
        }
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