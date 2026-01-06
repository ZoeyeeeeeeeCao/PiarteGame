using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public override void EnterState(EnemyController enemy)
    {
        
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.guardPoint == null) return;
        if (enemy.agent == null) return;

        // Check distance to the guard point
        // using agent.remainingDistance is often more accurate than Vector3.Distance for NavMesh paths,
        // but Vector3.Distance is safer if the path isn't calculated yet.
        float distanceToPoint = Vector3.Distance(enemy.transform.position, enemy.guardPoint.position);

        // 1. Walk to the point if not there
        if (distanceToPoint > 1.0f)
        {
            enemy.agent.isStopped = false;
            enemy.agent.speed = enemy.walkingSpeed;
            enemy.agent.destination = enemy.guardPoint.position;

            if (enemy.animator != null)
                enemy.animator.SetTrigger("Walk");
                enemy.animator.SetFloat("WalkBlend", 0.5f);
        }
        // 2. If at the point, Idle and Face Direction
        else
        {
            Debug.Log("Enemy Stop");
            // Stop the agent so it doesn't push around
            enemy.agent.isStopped = true;

            if (enemy.animator != null)
                enemy.animator.SetTrigger("Walk");
                enemy.animator.SetFloat("WalkBlend", 0f);

            // Face the direction of the empty object (Match rotation)
            // Slerp provides a smooth turn instead of a snap
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                enemy.guardPoint.rotation,
                Time.deltaTime * 5f
            );
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        // Ensure agent is free to move when leaving this state (e.g., getting agroed)
        if (enemy.agent != null)
            enemy.agent.isStopped = false;
    }
}