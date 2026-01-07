using UnityEngine;

/// <summary>
/// State triggered when the enemy takes damage.
/// Plays a hit animation and applies a knockback effect.
/// </summary>
public class EnemyDamageState : EnemyBaseState
{
    private float _timer = 0f;
    private const float _knockbackDuration = 0.2f;
    private const float _stateDuration = 0.5f; // Total time spent in this state
    private Vector3 _knockbackDirection;
    private float _knockbackForce = 10f;

    public override void EnterState(EnemyController enemy)
    {
        _timer = 0f;

        // logic: Play Damage Animation
        if (enemy.animator != null)
        {
            enemy.animator.SetTrigger("TakeDamage");
        }

        // logic: Calculate knockback direction (away from player)
        if (enemy.playerTarget != null)
        {
            _knockbackDirection = (enemy.transform.position - enemy.playerTarget.position).normalized;
            _knockbackDirection.y = 0; // Keep knockback horizontal
        }
        else
        {
            _knockbackDirection = -enemy.transform.forward;
        }

        // logic: Briefly disable NavMesh pathfinding to allow manual movement
        enemy.ToggleNavMesh(false);
    }

    public override void UpdateState(EnemyController enemy)
    {
        _timer += Time.deltaTime;

        // logic: Apply knockback force over the first part of the state
        if (_timer < _knockbackDuration)
        {
            // Use agent.Move to stay on NavMesh while being knocked back
            if (enemy.agent != null && enemy.agent.isOnNavMesh)
            {
                enemy.agent.Move(_knockbackDirection * _knockbackForce * Time.deltaTime);
            }
        }

        // logic: Return to Agroed state once animation/stun is over
        if (_timer >= _stateDuration)
        {
            enemy.TransitionToState(enemy.AgroedState);
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        // Re-enable NavMesh for chasing/patrolling
        enemy.ToggleNavMesh(true);
    }
}