using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script manages a Boss Health Bar UI. 
/// It listens to the EnemyHealthController and updates a filled Image component.
/// </summary>
public class BossBarController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The EnemyHealthController script attached to your Boss.")]
    [SerializeField] private EnemyHealthController bossHealth;

    [Tooltip("The UI Image used for the health bar fill. MUST have Image Type set to 'Filled'.")]
    [SerializeField] private Image fillImage;

    [Header("Visual Settings")]
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool hideWhenDead = true;
    [SerializeField] private float hideDelay = 1.5f;

    private float targetFillAmount = 1f;

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            // Subscribe to the event we added in EnemyHealthController
            bossHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            bossHealth.OnDeath.AddListener(HandleBossDeath);

            // Set initial state
            targetFillAmount = bossHealth.GetHealthPercentage();
            if (!useSmoothing) fillImage.fillAmount = targetFillAmount;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            // Unsubscribe to prevent memory leaks
            bossHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            bossHealth.OnDeath.RemoveListener(HandleBossDeath);
        }
    }

    private void Update()
    {
        if (!useSmoothing || fillImage == null) return;

        // Smoothly interpolate the fill amount for a 'draining' effect
        if (!Mathf.Approximately(fillImage.fillAmount, targetFillAmount))
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        }
    }

    private void UpdateHealthBar(float healthPercentage)
    {
        targetFillAmount = healthPercentage;

        if (!useSmoothing && fillImage != null)
        {
            fillImage.fillAmount = targetFillAmount;
        }
    }

    private void HandleBossDeath()
    {
        targetFillAmount = 0f;

        if (hideWhenDead)
        {
            Invoke(nameof(DisableUI), hideDelay);
        }
    }

    private void DisableUI()
    {
        gameObject.SetActive(false);
    }
}