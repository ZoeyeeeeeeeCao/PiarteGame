using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A modular health controller for enemies.
/// This version uses UnityEvents exclusively for damage and death,
/// allowing you to trigger animations, state changes, and sounds via the Inspector.
/// </summary>
public class EnemyHealthController : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Detection Settings")]
    [SerializeField] private string damageTag = "EnemyDamage";
    [SerializeField] private float damagePerHit = 10f;

    [Header("Testing")]
    [Tooltip("If enabled, pressing E will deal damage to this enemy for testing purposes.")]
    [SerializeField] private bool enableDebugKeys = true;

    [Header("Events")]
    [Tooltip("Triggered when health is reduced but remains above 0.")]
    public UnityEvent OnTakeDamage;

    [Tooltip("Triggered when health reaches 0.")]
    public UnityEvent OnDeath;

    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // Debug testing: Press E to damage the enemy
        if (enableDebugKeys && !isDead && Input.GetKeyDown(KeyCode.T))
        {
            ApplyDamage(damagePerHit);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag(damageTag))
        {
            ApplyDamage(damagePerHit);
        }
    }

    public void ApplyDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            // Transition to Death State via Event
            OnDeath?.Invoke();
        }
        else
        {
            // Transition to Damage State via Event
            OnTakeDamage?.Invoke();
        }
    }

    // Public getters for UI or other scripts
    public float GetHealth() => currentHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
}