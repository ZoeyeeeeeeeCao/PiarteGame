using UnityEngine;

// --- STATE: DEATH ---
public class EnemyDeathState : EnemyBaseState
{
    public override void EnterState(EnemyController enemy)
    {
        // logic: Disable collider
        // logic: Play death animation
        // logic: Drop loot
        Debug.Log("Enemy Died");
    }

    public override void UpdateState(EnemyController enemy)
    {
        // logic: Wait for 2 seconds, then Destroy(enemy.gameObject)
    }

    public override void ExitState(EnemyController enemy)
    {
        // Usually never exited
    }
}