using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private bool showHealthBar = true;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("Damage Feedback")]
    [SerializeField] private float hitStunDuration = 0.2f;
    [SerializeField] private Material flashMaterial;
    private Material originalMaterial;
    private Renderer enemyRenderer;

    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
        }

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(showHealthBar);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        UpdateHealthBar();

        // Damage feedback
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        if (enemyRenderer != null && flashMaterial != null)
        {
            enemyRenderer.material = flashMaterial;
            yield return new WaitForSeconds(0.1f);
            enemyRenderer.material = originalMaterial;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        isDead = true;

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // You can add death animation here
        Destroy(gameObject, 0.5f);
    }
}