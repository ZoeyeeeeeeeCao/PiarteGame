using UnityEngine;

public class EnemyChaseState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager Enemy)
    {
        Debug.Log("ChasingPlayer");


    }

    public override void UpdateState(EnemyStateManager Enemy)
    {

    }

    public override void OnCollisionEnter(EnemyStateManager Enemy) { }
}