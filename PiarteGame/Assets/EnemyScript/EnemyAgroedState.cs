using UnityEngine;

public class EnemyAgroedState : EnemyBaseState
{
    private EnemyBaseState _currentSubState;

    private readonly EnemyChasingState _chasingState = new EnemyChasingState();
    private readonly EnemyAttackingState _attackingState = new EnemyAttackingState();

    private float _stateChangeTimer = 0f;
    private float _attackCooldownTimer = 0f;

    public override void EnterState(EnemyController enemy)
    {
        Debug.Log("Agroed: Engaging Player");
        _attackCooldownTimer = 0f;
        SetSubState(enemy, _chasingState);
    }

    public override void UpdateState(EnemyController enemy)
    {
        if (enemy.playerTarget == null) return;

        _stateChangeTimer += Time.deltaTime;
        _attackCooldownTimer -= Time.deltaTime;

        // --- ATTACK LOCK ---
        if (_currentSubState == _attackingState)
        {
            bool isLocked = false;

            if (_stateChangeTimer < 0.2f)
            {
                isLocked = true;
            }
            else if (enemy.animator != null)
            {
                AnimatorStateInfo info = enemy.animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsTag("Attack") && info.normalizedTime < 1f)
                {
                    isLocked = true;
                }
            }

            if (isLocked)
            {
                _currentSubState.UpdateState(enemy);
                return;
            }
        }

        // --- FLATTEN DISTANCE CHECK ---
        Vector3 enemyPos = enemy.transform.position;
        Vector3 playerPos = enemy.playerTarget.position;
        enemyPos.y = playerPos.y = 0f;

        float distanceToPlayer = Vector3.Distance(enemyPos, playerPos);

        // --- STATE SELECTION ---
        if (distanceToPlayer <= enemy.attackSensorRange)
        {
            if (_currentSubState != _attackingState && _attackCooldownTimer <= 0f)
            {
                _attackCooldownTimer = enemy.attackCooldown;
                SetSubState(enemy, _attackingState);
            }
        }
        else if (distanceToPlayer <= enemy.viewRadius)
        {
            if (_currentSubState != _chasingState)
            {
                SetSubState(enemy, _chasingState);
            }
        }
        else
        {
            if (enemy.initialType == EnemyType.Spawning)
                enemy.initialType = EnemyType.Guarding;

            enemy.TransitionToState(enemy.StartState);
            return;
        }

        _currentSubState.UpdateState(enemy);
    }

    public override void ExitState(EnemyController enemy)
    {
        _currentSubState?.ExitState(enemy);
    }

    private void SetSubState(EnemyController enemy, EnemyBaseState subState)
    {
        _stateChangeTimer = 0f;
        _currentSubState?.ExitState(enemy);
        _currentSubState = subState;
        _currentSubState.EnterState(enemy);
    }
}
