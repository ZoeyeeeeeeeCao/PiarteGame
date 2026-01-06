using UnityEngine;

public class EnemyPatrolState : EnemyBaseState
{
    private int _currentPointIndex = 0;

    public override void EnterState(EnemyController enemy)
    {
        // Check if we have points to patrol
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned in EnemyController!");
            return;
        }

        // Set the speed
        if (enemy.agent != null)
            enemy.agent.speed = enemy.walkingSpeed;

        // Set the Animation
        if (enemy.animator != null)
            enemy.animator.SetTrigger("Walk");
            enemy.animator.SetFloat("WalkBlend", 0.5f);

        // Start moving to the current point immediately
        SetDestinationToCurrentPoint(enemy);
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0) return;
        if (enemy.agent == null) return;

        // Check if we have reached the destination
        // !pathPending ensures we don't return true while the agent is still calculating the path
        if (!enemy.agent.pathPending && enemy.agent.remainingDistance <= enemy.agent.stoppingDistance)
        {
            // Move to the next index, looping back to 0 if we hit the end
            _currentPointIndex = (_currentPointIndex + 1) % enemy.patrolPoints.Length;

            SetDestinationToCurrentPoint(enemy);
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        // Optional: Stop moving when exiting state if you want them to freeze
        // if (enemy.agent != null) enemy.agent.ResetPath();
    }

    private void SetDestinationToCurrentPoint(EnemyController enemy)
    {
        if (enemy.agent != null)
        {
            enemy.agent.destination = enemy.patrolPoints[_currentPointIndex].position;
        }
    }
}