using UnityEngine;

public class EnemySpawnTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag the enemy objects from the hierarchy here.")]
    public GameObject[] enemiesToActivate;

    [Tooltip("If true, the script will automatically hide the enemies when the game starts.")]
    public bool hideEnemiesOnStart = true;

    [Tooltip("If true, the trigger destroys itself after use (happens only once).")]
    public bool triggerOnce = true;

    private void Start()
    {
        // Automatically hide the enemies at the start of the game
        if (hideEnemiesOnStart)
        {
            foreach (GameObject enemy in enemiesToActivate)
            {
                if (enemy != null)
                {
                    enemy.SetActive(false);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the Player
        // Make sure your Player object has the tag "Player"
        if (other.CompareTag("Player"))
        {
            ActivateEnemies();
        }
    }

    private void ActivateEnemies()
    {
        foreach (GameObject enemy in enemiesToActivate)
        {
            if (enemy != null)
            {
                enemy.SetActive(true);
            }
        }

        // Optional: Destroy the trigger object so it doesn't fire again
        if (triggerOnce)
        {
            Destroy(gameObject);
        }
    }
}