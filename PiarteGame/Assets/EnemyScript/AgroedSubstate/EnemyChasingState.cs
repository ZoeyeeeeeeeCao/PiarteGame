using UnityEngine;

public class EnemyChasingState : EnemyBaseState
{
    public override void EnterState(EnemyController enemy)
    {
        // 1. Setup Movement for Chasing
        if (enemy.agent != null)
        {
            enemy.agent.speed = enemy.runningSpeed; // Set to Run Speed (3f)
            enemy.agent.isStopped = false;          // Ensure we can move

            // Reset stopping distance to 0 so we don't stop early while chasing; 
            // the transition to AttackState handles the actual "stopping range".
            enemy.agent.stoppingDistance = 0f;
        }

        enemy.animator.SetBool("Attack", false);
    }

    public override void UpdateState(EnemyController enemy)
    {
        // Ensure we have references
        if (enemy.agent != null && enemy.playerTarget != null)
        {
            // Continuously update destination to the Player's current position
            enemy.agent.destination = enemy.playerTarget.position;
        }

        // --- SMOOTH ANIMATION TRANSITION ---
        if (enemy.animator != null)
        {
            // Using 0.2f as the dampTime allows the value to blend from 0 (Idle/Spawn) to 1 (Run) 
            // smoothly over roughly 0.2 seconds, instead of snapping.
            enemy.animator.SetFloat("WalkBlend", 1.0f, 0.2f, Time.deltaTime);
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        // No specific cleanup needed here.
    }
}