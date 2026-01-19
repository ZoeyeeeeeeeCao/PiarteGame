using UnityEngine;
using UnityEngine.SceneManagement;
public class TransitionToRunningScene : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag the Boss GameObject here")]
    [SerializeField] private EnemyHealthController bossHealthController;

    private void OnEnable()
    {
        // Subscribe to the static death event
        EnemyHealthController.EnemyDied += CheckIfBossDied;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and errors when switching scenes
        EnemyHealthController.EnemyDied -= CheckIfBossDied;
    }

    private void CheckIfBossDied(EnemyHealthController deadEnemy)
    {
        // Check if the enemy that just died is the one we are tracking as the boss
        if (deadEnemy == bossHealthController)
        {
            HandleBossDeath();
        }
    }

    private void HandleBossDeath()
    {
        Debug.Log("BOSS DEAD");

        SceneManager.LoadScene("Running");
    }
}