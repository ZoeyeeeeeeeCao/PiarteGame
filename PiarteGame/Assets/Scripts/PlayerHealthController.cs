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

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentHealth = maxHealth;
        Notify();
    }

    private void Update()
    {
        // DEBUG: test damage
        if (Input.GetKeyDown(KeyCode.J))
        {
            Damage(15f);
        }
    }

    public void Damage(float amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        Notify();

        if (CurrentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);
        Notify();
    }

    private void Die()
    {
        Debug.Log("Player died");
        OnDeath?.Invoke();
    }

    private void Notify()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
