using UnityEngine;

public class EnemyAttackingState : EnemyBaseState
{
    private float _timer;

    public override void EnterState(EnemyController enemy)
    {
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            enemy.agent.ResetPath();
        }

        // Attack immediately on enter
        _timer = enemy.attackCooldown;
    }

    public override void UpdateState(EnemyController enemy)
    {
        // Safety: Aggressively kill movement
        if (enemy.agent != null)
        {
            enemy.agent.velocity = Vector3.zero;
            if (enemy.agent.hasPath) enemy.agent.ResetPath();
        }

        if (enemy.playerTarget == null) return;

        // 1. Smoothly blend movement animation to Idle
        if (enemy.animator != null)
        {
            enemy.animator.SetFloat("WalkBlend", 0f, 0.2f, Time.deltaTime);
        }

        bool isAttacking = false;
        if (enemy.animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Attack"))
            {
                isAttacking = true;
            }
        }

        // 2. Face the Player (Only if NOT currently mid-attack)
        if (!isAttacking)
        {
            Vector3 direction = (enemy.playerTarget.position - enemy.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, lookRotation, Time.deltaTime * 2f);
            }

            // Restore Torch Layer weight to 1 when done attacking (and holding a torch).
            // We use _timer > 0.2f (buffer) to ensure we don't accidentally reset it immediately 
            // after PerformRandomAttack sets it to 0 (before the animation starts).
            if (enemy.hasTorch && _timer > 0.2f)
            {
                enemy.SetTorchLayerWeight(1f);
            }
        }

        // 3. Handle Attack Logic
        _timer += Time.deltaTime;

        if (_timer >= enemy.attackCooldown && !isAttacking)
        {
            PerformRandomAttack(enemy);
            _timer = 0f;
        }
    }

    private void PerformRandomAttack(EnemyController enemy)
    {
        if (enemy.animator == null) return;

        // A. Decide Attack Type (Torch vs Normal)
        bool useTorchAttack = false;

        if (enemy.hasTorch)
        {
            // If holding a torch, randomly choose (50/50) between using it or a normal attack
            useTorchAttack = Random.Range(0, 2) == 0;
        }
        else
        {
            // No torch -> Always Normal Attack
            useTorchAttack = false;
        }

        // B. Set Animator Bools
        enemy.animator.SetBool("TorchAttack", useTorchAttack);
        enemy.animator.SetBool("NormalAttack", !useTorchAttack);

        // C. Manage Torch Layer Weight
        // If we chose a Torch Attack, set weight to 0 so the attack animation controls the arm.
        // If Normal Attack, we leave it (or it stays 1 from Update) to keep holding the torch up.
        if (useTorchAttack)
        {
            enemy.SetTorchLayerWeight(0f);
        }

        // D. Choose Random Attack Index (0, 1, or 3)
        // We pick a random number 0, 1, or 2 to represent the 3 possible attacks
        int rand = Random.Range(0, 3);
        int attackIndex = 0;

        switch (rand)
        {
            case 0: attackIndex = 0; break; // Attack 1
            case 1: attackIndex = 1; break; // Attack 2
            case 2: attackIndex = 3; break; // Attack 3
        }

        // E. Trigger Attack (Using SetInteger for precision)
        enemy.animator.SetInteger("AttackIndex", attackIndex);
        enemy.animator.SetTrigger("Attack");
    }

    public override void ExitState(EnemyController enemy)
    {
        if (enemy.animator != null)
        {
            // Reset Bools on exit to ensure clean state
            enemy.animator.SetBool("TorchAttack", false);
            enemy.animator.SetBool("NormalAttack", false);
        }

        // Important: Restore the Torch Layer when leaving the attack state
        if (enemy.hasTorch)
        {
            enemy.SetTorchLayerWeight(1f);
        }
        else
        {
            enemy.SetTorchLayerWeight(0f);
        }
    }
}