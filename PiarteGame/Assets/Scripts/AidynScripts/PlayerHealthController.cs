using System;
using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    public static PlayerHealthController Instance;

    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsFullHealth => Mathf.Approximately(CurrentHealth, maxHealth);

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private bool deathFired = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If you want health to persist, keep this.
        // If you want "new run" health per fresh play session, reset elsewhere.
        CurrentHealth = maxHealth;
        Notify();
    }

    private void Update()
    {
#if UNITY_EDITOR
        // DEBUG: test damage
        if (Input.GetKeyDown(KeyCode.J))
        {
            Damage(15f);
        }
#endif
    }

    public void Damage(float amount)
    {
        if (deathFired) return;
        if (amount <= 0f) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        Notify();

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (deathFired) return;
        if (amount <= 0f) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        Notify();
    }

    /// <summary>
    /// Call this when restarting the level so you don't reload with 0 HP
    /// (since this controller persists with DontDestroyOnLoad).
    /// </summary>
    public void ResetHealth()
    {
        deathFired = false;
        CurrentHealth = maxHealth;
        Notify();
    }

    private void Die()
    {
        if (deathFired) return;
        deathFired = true;

        Debug.Log("Player died");
        OnDeath?.Invoke();
    }

    private void Notify()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
