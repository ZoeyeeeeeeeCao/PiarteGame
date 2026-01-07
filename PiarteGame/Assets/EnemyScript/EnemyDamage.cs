using UnityEngine;

/// <summary>
/// Attached to a weapon's collider (trigger) to deal damage to the player.
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage dealt to the player on hit.")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("The tag assigned to the Player game object.")]
    [SerializeField] private string playerTag = "Player";

    /// <summary>
    /// Detects collision with the player and applies damage.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object hit has the Player tag
        if (other.CompareTag(playerTag))
        {
            // Try to find the PlayerHealthController on the hit object (or its parent)
            PlayerHealthController playerHealth = other.GetComponent<PlayerHealthController>();

            // If the script is on a different part of the player hierarchy, check parents
            if (playerHealth == null)
            {
                playerHealth = other.GetComponentInParent<PlayerHealthController>();
            }

            // If we found the health controller, apply damage
            if (playerHealth != null)
            {
                playerHealth.Damage(damageAmount);
                Debug.Log($"Sword hit player! Dealt {damageAmount} damage.");
            }
            else
            {
                Debug.LogWarning("Sword hit Player tag, but no PlayerHealthController was found.");
            }
        }
    }
}