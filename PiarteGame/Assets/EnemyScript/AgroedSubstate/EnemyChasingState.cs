using UnityEngine;

public class EnemyChasingState : EnemyBaseState
{
    private const float DESIRED_DISTANCE = 1f;
    private const float STOPPING_THRESHOLD = 0.15f;

    public override void EnterState(EnemyController enemy)
    {
        enemy.ToggleNavMesh(true);

        if (enemy.agent != null && enemy.agent.enabled)
        {
            enemy.agent.speed = enemy.runningSpeed;
            enemy.agent.isStopped = false;
            enemy.agent.stoppingDistance = 0f;
            enemy.agent.updateRotation = false; // We handle rotation manually for smoother look
        }
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.isSpawning) return;
        if (enemy.agent == null || !enemy.agent.enabled || enemy.playerTarget == null) return;

        // 1. Get real positions for the NavMesh (includes Y height)
        Vector3 realEnemyPos = enemy.transform.position;
        Vector3 realPlayerPos = enemy.playerTarget.position;

        // 2. Create flattened versions for distance and rotation logic
        // This prevents the enemy from "tilting" or miscalculating distance on hills
        Vector3 flatEnemyPos = new Vector3(realEnemyPos.x, 0, realEnemyPos.z);
        Vector3 flatPlayerPos = new Vector3(realPlayerPos.x, 0, realPlayerPos.z);

        float distanceToPlayer = Vector3.Distance(flatEnemyPos, flatPlayerPos);

        // --- MAINTAIN DISTANCE ---
        // Calculate the direction on a flat plane
        Vector3 dirFromPlayer = (flatEnemyPos - flatPlayerPos).normalized;

        // Target a position near the player, but keep the player's real Y height
        // so the NavMeshAgent doesn't try to go underground.
        Vector3 desiredPos = realPlayerPos + dirFromPlayer * DESIRED_DISTANCE;

        if (distanceToPlayer > DESIRED_DISTANCE + STOPPING_THRESHOLD ||
            distanceToPlayer < DESIRED_DISTANCE - STOPPING_THRESHOLD)
        {
            enemy.agent.isStopped = false;
            enemy.agent.destination = desiredPos;
        }
        else
        {
            enemy.agent.isStopped = true;
        }

        // --- FACE PLAYER ---
        // We use the flat positions here so the enemy rotates only on the Y axis
        Vector3 lookDir = flatPlayerPos - flatEnemyPos;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                targetRot,
                Time.deltaTime * 8f
            );
        }

        // --- ANIMATION ---
        float currentSpeed = enemy.agent.velocity.magnitude;
        float blend = Mathf.InverseLerp(0f, enemy.runningSpeed, currentSpeed);

        if (enemy.animator != null)
        {
            enemy.animator.SetFloat("WalkBlend", blend, 0.15f, Time.deltaTime);
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        if (enemy.agent != null && enemy.agent.enabled)
        {
            enemy.agent.isStopped = true;
        }
    }
}