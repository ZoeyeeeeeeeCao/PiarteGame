using UnityEngine;

public class EnemyTutorialTarget : MonoBehaviour
{
    public int health = 100;
    private CombatTutorialSystem manager;

    void Start() { manager = FindObjectOfType<CombatTutorialSystem>(); }

    // Call this function when weapon hits enemy
    public void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0) Die();
    }

    // Helper for testing in Inspector
    [ContextMenu("Kill Enemy")]
    void Die()
    {
   //     if (manager != null) manager.OnEnemyKilled();
        Destroy(gameObject);
    }
}