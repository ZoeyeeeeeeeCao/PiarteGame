using UnityEngine;

// --- SUPER STATE: AGRO ---
public class EnemyAgroedState : EnemyBaseState
{
    private EnemyBaseState _currentSubState;
    
    // Sub-states
    private readonly EnemyChasingState _chasingState = new EnemyChasingState();
    private readonly EnemyAttackingState _attackingState = new EnemyAttackingState();

    private float _loseAgroTimer = 0f;

    public override void EnterState(EnemyController enemy)
    {
        Debug.Log("Agroed: Engaging Player");
        _loseAgroTimer = 0f;
        // Default to chasing when first agroed
        SetSubState(enemy, _chasingState);
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.playerTarget == null)
        {
            Debug.LogWarning("Enemy is Agroed but has no Player Target assigned in Inspector!");
            return;
        }

        bool isLockedInAttack = false;
        if (_currentSubState == _attackingState && enemy.animator != null)
        {
            AnimatorStateInfo currentInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo nextInfo = enemy.animator.GetNextAnimatorStateInfo(0);

            // Check if currently attacking OR transitioning into attack
            if (currentInfo.IsName("Attack") || nextInfo.IsName("Attack"))
            {
                isLockedInAttack = true;
            }
        }

        // If locked, just update the attack state and return early (Skip all distance checks)
        if (isLockedInAttack)
        {
            _currentSubState?.UpdateState(enemy);
            return;
        }


        // 2. Normal Distance Logic
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.playerTarget.position);

        // -- LOSE AGRO LOGIC --
        // If player is too far (20f), wait 5 seconds then go back to Start State
        if (distanceToPlayer > 20f)
        {
            if (enemy.agent != null) 
            {
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
            }

            if (enemy.animator != null)
                enemy.animator.SetFloat("WalkBlend", 0f, 0.2f, Time.deltaTime);

            _loseAgroTimer += Time.deltaTime;
            if (_loseAgroTimer >= 5f)
            {
                ReturnToStart(enemy);
            }
            return;
        }
        else
        {
            _loseAgroTimer = 0f;
        }

        // -- COMBAT LOGIC --
        _currentSubState?.UpdateState(enemy);

        if (distanceToPlayer <= enemy.attackRange)
        {
            if (_currentSubState != _attackingState)
            {
                SetSubState(enemy, _attackingState);
            }
        }
        else
        {
            if (_currentSubState != _chasingState)
            {
                SetSubState(enemy, _chasingState);
            }
        }
    }

    private void ReturnToStart(EnemyController enemy)
    {
        if (enemy.initialType == EnemyType.Spawning)
        {
            enemy.initialType = EnemyType.Guarding;
        }
        enemy.TransitionToState(enemy.StartState);
    }

    public override void ExitState(EnemyController enemy)
    {
        _currentSubState?.ExitState(enemy);
    }

    private void SetSubState(EnemyController enemy, EnemyBaseState subState)
    {
        _currentSubState?.ExitState(enemy);
        _currentSubState = subState;
        _currentSubState.EnterState(enemy);
    }
}