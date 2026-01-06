using UnityEngine;

public class EnemyChasingState : EnemyBaseState
{
    private const float DESIRED_DISTANCE = 1f;

    public override void EnterState(EnemyController enemy)
    {
        enemy.ToggleNavMesh(true);

        if (enemy.agent != null && enemy.agent.enabled)
        {
            enemy.agent.speed = enemy.runningSpeed;
            enemy.agent.isStopped = false;
            enemy.agent.stoppingDistance = 0f; // we control distance manually
            enemy.agent.updateRotation = false; // manual facing
        }
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.isSpawning) return;
        if (enemy.agent == null || !enemy.agent.enabled || enemy.playerTarget == null) return;

        // --- FLATTEN POSITIONS ---
        Vector3 enemyPos = enemy.transform.position;
        Vector3 playerPos = enemy.playerTarget.position;
        enemyPos.y = playerPos.y = 0f;

        float distanceToPlayer = Vector3.Distance(enemyPos, playerPos);

        // --- MAINTAIN DISTANCE ---
        Vector3 dirFromPlayer = (enemyPos - playerPos).normalized;
        Vector3 desiredPos = playerPos + dirFromPlayer * DESIRED_DISTANCE;

        if (distanceToPlayer > DESIRED_DISTANCE + 0.15f || distanceToPlayer < DESIRED_DISTANCE - 0.15f)
        {
            enemy.agent.isStopped = false;
            enemy.agent.destination = desiredPos;
        }
        else
        {
            enemy.agent.isStopped = true;
        }

        // --- FACE PLAYER ---
        Vector3 lookDir = playerPos - enemyPos;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                targetRot,
                Time.deltaTime * 8f
            );
        }

        // --- ANIMATION DRIVEN BY SPEED ---
        float speed = enemy.agent.velocity.magnitude;
        float blend = Mathf.InverseLerp(0f, enemy.runningSpeed, speed);

        if (enemy.animator != null)
        {
            enemy.animator.SetFloat("WalkBlend", blend, 0.15f, Time.deltaTime);
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        // nothing special
    }
}
