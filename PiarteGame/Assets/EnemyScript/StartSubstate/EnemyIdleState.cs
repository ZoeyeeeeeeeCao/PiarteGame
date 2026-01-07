using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public override void EnterState(EnemyController enemy)
    {
        // Enable NavMesh so the enemy can walk back to guard point
        enemy.ToggleNavMesh(true);

        if (enemy.agent != null)
        {
            enemy.agent.isStopped = false;
            enemy.agent.speed = enemy.walkingSpeed;
            enemy.agent.updateRotation = false; // manual rotation
        }

        if (enemy.animator != null)
            enemy.animator.SetFloat("WalkBlend", 0f);
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.guardPoint == null || enemy.agent == null || !enemy.agent.enabled) return;

        Vector3 flatEnemyPos = new Vector3(enemy.transform.position.x, 0f, enemy.transform.position.z);
        Vector3 flatTargetPos = new Vector3(enemy.guardPoint.position.x, 0f, enemy.guardPoint.position.z);
        float distanceToPoint = Vector3.Distance(flatEnemyPos, flatTargetPos);

        // WALK to guard point
        if (distanceToPoint > enemy.agent.stoppingDistance + 0.1f)
        {
            enemy.agent.destination = enemy.guardPoint.position;

            if (enemy.animator != null)
                enemy.animator.SetTrigger("Walk");
                enemy.animator.SetFloat("WalkBlend", 0.5f);

            // Face movement direction
            if (enemy.agent.velocity.sqrMagnitude > 0.05f)
            {
                Vector3 dir = enemy.agent.velocity.normalized;
                dir.y = 0f;

                Quaternion targetRot = Quaternion.LookRotation(dir);
                enemy.transform.rotation = Quaternion.Slerp(
                    enemy.transform.rotation,
                    targetRot,
                    Time.deltaTime * 8f
                );
            }
        }
        // ARRIVED
        else
        {
            enemy.agent.isStopped = true;
            enemy.ToggleNavMesh(false); // truly idle

            if (enemy.animator != null)
                enemy.animator.SetFloat("WalkBlend", 0f);

            // Face guard direction
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                enemy.guardPoint.rotation,
                Time.deltaTime * 5f
            );
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        enemy.ToggleNavMesh(true);
    }
}
