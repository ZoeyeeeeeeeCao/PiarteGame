using UnityEngine;

public class EnemySpawnState : EnemyBaseState
{
    // Flag to ensure we don't finish before the animation has even started
    private bool _hasStartedSpawnAnimation = false;

    public override void EnterState(EnemyController enemy)
    {
        Debug.Log("Enemy Spawning...");

        enemy.isSpawning = true;

        enemy.ToggleNavMesh(false);

        _hasStartedSpawnAnimation = false;

        if (enemy.animator != null)
        {
            enemy.animator.SetTrigger("Spawn");
        }
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.animator == null)
        {
            // If no animator, just skip immediately to Agro
            enemy.TransitionToState(enemy.AgroedState);
            return;
        }

        // Get the current state info of layer 0
        AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);

        // 1. Wait for the animator to actually switch to the "Spawn" state.
        if (stateInfo.IsName("Spawn"))
        {
            _hasStartedSpawnAnimation = true;
        }

        // 2. Check for completion
        if (_hasStartedSpawnAnimation)
        {
            // We transition if:
            // A) The Spawn animation has finished playing (normalizedTime >= 1)
            // OR
            // B) The Animator has already transitioned to a different state (like Idle) 
            //    because the Spawn clip finished. (!IsName("Spawn"))
            if (stateInfo.normalizedTime >= 1.0f || !stateInfo.IsName("Spawn"))
            {
                // Transition to the combat state (Agroed contains Chasing/Attacking)
                enemy.TransitionToState(enemy.AgroedState);
            }
        }
    }

    public override void ExitState(EnemyController enemy)
    {
        enemy.isSpawning = false;
    }
}