using UnityEngine;
using System;

public class EnemyCounter : MonoBehaviour
{
    private int enemyDeathCount = 0;

    // 🔔 Fired whenever the enemy kill count changes
    public static event Action<int> OnEnemyCountUpdated;

    private void OnEnable()
    {
        EnemyHealthController.EnemyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealthController.EnemyDied -= OnEnemyDied;
    }

    private void OnEnemyDied(EnemyHealthController enemy)
    {
        enemyDeathCount++;

        Debug.Log($"[EnemyCounter] Enemy killed! Total enemies defeated: {enemyDeathCount}");

        // 🔥 Notify listeners (mission manager, UI, etc.)
        OnEnemyCountUpdated?.Invoke(enemyDeathCount);
    }

    public int GetEnemyDeathCount()
    {
        return enemyDeathCount;
    }

    public void ResetCounter()
    {
        enemyDeathCount = 0;
        OnEnemyCountUpdated?.Invoke(enemyDeathCount);
    }
}
