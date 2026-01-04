using UnityEngine;

public class EnemySpawnState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager Enemy)
    {
        Debug.Log("Spawning");


    }

    public override void UpdateState(EnemyStateManager Enemy)
    {

    }

    public override void OnCollisionEnter(EnemyStateManager Enemy) { }
}