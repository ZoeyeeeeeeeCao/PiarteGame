using UnityEngine;

public class EnemyAttackingState : EnemyBaseState
{
    private float _timer;
    private float _torchWeightTransitionSpeed = 3f;

    public override void EnterState(EnemyController enemy)
    {
        // Ensure we start with a fresh cooldown or ready to strike
        _timer = enemy.attackCooldown;
        enemy.SetCombatActive(1);
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.playerTarget == null) return;

        // Smoothly stop movement animation
        if (enemy.animator != null)
        {
            enemy.animator.SetFloat("WalkBlend", 0f, 0.2f, Time.deltaTime);
        }

        // Check if currently playing an attack animation
        bool isAttacking = false;
        if (enemy.animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("Attack"))
            {
                isAttacking = true;
            }
        }

        // Rotate and manage torch ONLY when not in the middle of a swing
        if (!isAttacking)
        {
            // Rotate to face player
            Vector3 direction = (enemy.playerTarget.position - enemy.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            // Restore torch layer weight
            if (enemy.hasTorch && _timer > 0.2f)
            {
                float currentWeight = enemy.animator.GetLayerWeight(enemy.torchLayerIndex);
                float newWeight = Mathf.Lerp(currentWeight, 1f, Time.deltaTime * _torchWeightTransitionSpeed);
                enemy.animator.SetLayerWeight(enemy.torchLayerIndex, newWeight);
            }
        }
        else
        {
            // Lower torch during torch attacks
            if (enemy.hasTorch && enemy.animator.GetBool("TorchAttack"))
            {
                float currentWeight = enemy.animator.GetLayerWeight(enemy.torchLayerIndex);
                float newWeight = Mathf.Lerp(currentWeight, 0f, Time.deltaTime * _torchWeightTransitionSpeed);
                enemy.animator.SetLayerWeight(enemy.torchLayerIndex, newWeight);
            }
        }

        _timer += Time.deltaTime;

        if (_timer >= enemy.attackCooldown && !isAttacking)
        {
            PerformRandomAttack(enemy);
        }
    }

    private void PerformRandomAttack(EnemyController enemy)
    {
        if (enemy.animator == null) return;

        _timer = 0f;

        bool useTorchAttack = false;
        if (enemy.hasTorch)
        {
            useTorchAttack = Random.Range(0, 2) == 0;
        }

        enemy.animator.SetBool("TorchAttack", useTorchAttack);
        enemy.animator.SetBool("NormalAttack", !useTorchAttack);

        // Select random variation
        int rand = Random.Range(0, 3);
        int attackIndex = (rand == 2) ? 3 : rand; // Maps 0,1,2 to 0,1,3

        enemy.animator.SetInteger("AttackIndex", attackIndex);
    }

    public override void ExitState(EnemyController enemy)
    {
        // CRITICAL: Safety turn off for hitbox and trail when leaving state
        enemy.DisableCombat();

        if (enemy.animator != null)
        {
            enemy.animator.SetBool("TorchAttack", false);
            enemy.animator.SetBool("NormalAttack", false);

            // Restore torch layer if they have one
            if (enemy.hasTorch)
                enemy.animator.SetLayerWeight(enemy.torchLayerIndex, 1f);
        }
    }
}