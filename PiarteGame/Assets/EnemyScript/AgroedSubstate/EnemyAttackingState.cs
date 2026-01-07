using UnityEngine;

public class EnemyAttackingState : EnemyBaseState
{
    private float _timer;
    private float _torchWeightTransitionSpeed = 3f; // Speed of torch layer weight transitions
    private float _rotationSpeed = 8f; // Rotation speed toward player

    public override void EnterState(EnemyController enemy)
    {
        //enemy.ToggleNavMesh(false);
        _timer = enemy.attackCooldown; // Start ready to attack
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

        // Only rotate and manage torch when NOT attacking
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

            // Smoothly restore torch layer weight after a brief delay
            if (enemy.hasTorch && _timer > 0.2f)
            {
                float currentWeight = enemy.animator.GetLayerWeight(enemy.torchLayerIndex);
                float targetWeight = 1f;
                float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * _torchWeightTransitionSpeed);
                enemy.animator.SetLayerWeight(enemy.torchLayerIndex, newWeight);
            }
        }
        else
        {
            // Smoothly lower torch layer weight during attacks if torch attack is active
            if (enemy.hasTorch && enemy.animator.GetBool("TorchAttack"))
            {
                float currentWeight = enemy.animator.GetLayerWeight(enemy.torchLayerIndex);
                float targetWeight = 0f;
                float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * _torchWeightTransitionSpeed);
                enemy.animator.SetLayerWeight(enemy.torchLayerIndex, newWeight);
            }
        }

        // Increment attack cooldown timer
        _timer += Time.deltaTime;

        // Trigger attack when cooldown is ready and not currently attacking
        if (_timer >= enemy.attackCooldown && !isAttacking)
        {
            PerformRandomAttack(enemy);
        }
    }

    private void PerformRandomAttack(EnemyController enemy)
    {
        if (enemy.animator == null) return;

        // CRITICAL: Reset timer FIRST to prevent immediate re-triggering
        _timer = 0f;

        // Determine if using torch attack (50/50 chance if enemy has torch)
        bool useTorchAttack = false;
        if (enemy.hasTorch)
        {
            useTorchAttack = Random.Range(0, 2) == 0;
        }

        Debug.Log("Attack Triggered: " + (useTorchAttack ? "Torch Attack" : "Normal Attack"));

        // Set attack type bools
        enemy.animator.SetBool("TorchAttack", useTorchAttack);
        enemy.animator.SetBool("NormalAttack", !useTorchAttack);

        // Note: Torch layer weight will be smoothly transitioned in UpdateState
        // No instant change here

        // Select random attack variation (0, 1, or 3)
        int rand = Random.Range(0, 3);
        int attackIndex = 0;
        switch (rand)
        {
            case 0: attackIndex = 0; break;
            case 1: attackIndex = 1; break;
            case 2: attackIndex = 3; break;
        }

        enemy.animator.SetInteger("AttackIndex", attackIndex);
    }

    public override void ExitState(EnemyController enemy)
    {
        // Clean up attack parameters
        if (enemy.animator != null)
        {
            Debug.Log("Attacks Disabled - Exiting Attack State");
            enemy.animator.SetBool("TorchAttack", false);
            enemy.animator.SetBool("NormalAttack", false);
        }

        // Smoothly restore torch layer weight based on whether enemy has torch
        if (enemy.hasTorch)
        {
            // Start smooth transition to weight 1
            float currentWeight = enemy.animator.GetLayerWeight(enemy.torchLayerIndex);
            float targetWeight = 1f;
            // Use a faster transition speed on exit for responsiveness
            float newWeight = Mathf.Lerp(currentWeight, targetWeight, 0.5f);
            enemy.animator.SetLayerWeight(enemy.torchLayerIndex, newWeight);
        }
        else
        {
            enemy.animator.SetLayerWeight(enemy.torchLayerIndex, 0f);
        }
    }
}