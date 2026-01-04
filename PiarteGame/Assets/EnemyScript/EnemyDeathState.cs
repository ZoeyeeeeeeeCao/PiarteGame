using UnityEngine;

public class EnemyDeathState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager Enemy)
    {
        Debug.Log("Enemy Died");

    }

    public override void UpdateState(EnemyStateManager Enemy)
    {

    }

    public override void OnCollisionEnter(EnemyStateManager Enemy)
    {

    }
}