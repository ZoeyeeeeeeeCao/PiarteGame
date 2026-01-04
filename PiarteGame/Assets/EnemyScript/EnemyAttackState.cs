using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager Enemy)
    {
        Debug.Log("Entering Attack State");

    }

    public override void UpdateState(EnemyStateManager Enemy)
    {

    }

    public override void OnCollisionEnter(EnemyStateManager Enemy) { }

}