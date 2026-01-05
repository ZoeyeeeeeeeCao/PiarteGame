using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image fillImage; // assign the Fill Image here

    private void OnEnable()
    {
        // Subscribe if instance exists now
        if (PlayerHealthController.Instance != null)
            PlayerHealthController.Instance.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        // If PlayerHealthController was created after this UI enabled, subscribe here too
        if (PlayerHealthController.Instance != null)
        {
            PlayerHealthController.Instance.OnHealthChanged -= HandleHealthChanged; // prevent double-subscribe
            PlayerHealthController.Instance.OnHealthChanged += HandleHealthChanged;

            // Force an initial UI refresh
            HandleHealthChanged(PlayerHealthController.Instance.CurrentHealth,
                               PlayerHealthController.Instance.MaxHealth);
        }
        else
        {
            // UI can start empty; it will update once controller exists
            SetFill(1f);
        }
    }

    private void OnDisable()
    {
        if (PlayerHealthController.Instance != null)
            PlayerHealthController.Instance.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        float percent = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
        SetFill(percent);
    }

    private void SetFill(float percent)
    {
        if (fillImage != null)
            fillImage.fillAmount = percent;
    }
}
