using UnityEngine;

public class EnemyDeathState : EnemyBaseState
{
    private float deathTimer = 0f;
    private const float destroyDelay = 2.0f;

    public override void EnterState(EnemyController enemy)
    {
        Debug.Log("Enemy Entered Death State");

        // logic: Stop Movement
        enemy.ToggleNavMesh(false);

        // logic: Disable Collision so player doesn't trip over a corpse
        Collider col = enemy.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // logic: Play death animation
        if (enemy.animator != null)
        {
            enemy.animator.SetTrigger("Die");
        }

        // logic: Drop loot (e.g., instantiate a prefab)

        deathTimer = 0f;
    }

    public override void UpdateState(EnemyController enemy)
    {
        // logic: Wait for timer, then destroy the game object
        deathTimer += Time.deltaTime;

        if (deathTimer >= destroyDelay)
        {
            Object.Destroy(enemy.gameObject);
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        // Usually never exited because the object is destroyed
    }
}