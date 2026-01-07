using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrolState : EnemyBaseState
{
    private int _currentPointIndex = 0;
    private float _waitTimer = 0f;

    [Header("Patrol Timing")]
    public float waitTimeAtPoint = 1.5f; // seconds to wait at each patrol point

    public override void EnterState(EnemyController enemy)
    {
        // Enable NavMesh for Patrol
        enemy.ToggleNavMesh(true);

        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned in EnemyController!");
            return;
        }

        if (enemy.agent != null && enemy.agent.enabled)
        {
            enemy.agent.speed = enemy.walkingSpeed;
            enemy.agent.isStopped = false;

            // IMPORTANT: allow NavMeshAgent to rotate the enemy
            enemy.agent.updateRotation = true;
        }

        if (enemy.animator != null)
        {
            enemy.animator.SetTrigger("Walk");
            enemy.animator.SetFloat("WalkBlend", 0.5f);
        }

        SetDestinationToCurrentPoint(enemy);
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0) return;
        if (enemy.agent == null || !enemy.agent.enabled) return;

        // Smoothly rotate toward movement direction (fixes sliding / moonwalk)
        if (enemy.agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = enemy.agent.velocity.normalized;
            direction.y = 0f;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                targetRotation,
                Time.deltaTime * 8f
            );
        }

        // If reached patrol point, start waiting
        if (!enemy.agent.pathPending && enemy.agent.remainingDistance <= 1.0f)
        {
            _waitTimer += Time.deltaTime;

            if (_waitTimer >= waitTimeAtPoint)
            {
                _waitTimer = 0f;
                _currentPointIndex = (_currentPointIndex + 1) % enemy.patrolPoints.Length;
                SetDestinationToCurrentPoint(enemy);
            }
        }
        else
        {
            // Reset timer while moving
            _waitTimer = 0f;
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        // Leaving Patrol state
    }

    private void SetDestinationToCurrentPoint(EnemyController enemy)
    {
        if (enemy.agent != null && enemy.agent.enabled)
        {
            enemy.agent.destination = enemy.patrolPoints[_currentPointIndex].position;
        }
    }
}
