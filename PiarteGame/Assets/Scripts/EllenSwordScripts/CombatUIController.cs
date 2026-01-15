using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SwordCombatController combatController;

    [Header("UI Container")]
    [SerializeField] private GameObject combatUIContainer;

    [Header("Button Images")]
    [SerializeField] private Image leftMouseButtonImage;
    [SerializeField] private Image rightMouseButtonImage;
    [SerializeField] private Image rButtonImage;

    [Header("R Button Fill (Hard Attack Indicator)")]
    [SerializeField] private Image rButtonFillImage;
    [Tooltip("The hard attack requirement from SwordCombatController")]
    [SerializeField] private int hardAttackRequirement = 10;

    [Header("Opacity Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float idleOpacity = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float activeOpacity = 1f;
    [SerializeField] private float opacityTransitionSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private bool useScalePulse = true;
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseSpeed = 0.1f;

    [Header("Hard Attack Ready Animation")]
    [SerializeField] private Color hardAttackReadyColor = Color.yellow;
    [SerializeField] private float readyPulseScale = 1.15f;
    [SerializeField] private float readyPulseSpeed = 0.5f;

    // State tracking
    private bool isUIVisible = false;
    private bool isHardAttackReady = false;
    private int lastAttackCount = 0;

    // Target opacities for each button
    private float leftMouseTargetOpacity;
    private float rightMouseTargetOpacity;
    private float rButtonTargetOpacity;

    // Coroutines for pulse effects
    private Coroutine leftMousePulseCoroutine;
    private Coroutine rightMousePulseCoroutine;
    private Coroutine rButtonPulseCoroutine;
    private Coroutine hardAttackReadyPulseCoroutine;

    // Store original scales and colors
    private Vector3 leftMouseOriginalScale;
    private Vector3 rightMouseOriginalScale;
    private Vector3 rButtonOriginalScale;
    private Color rButtonOriginalColor;

    private void Start()
    {
        // Find combat controller if not assigned
        if (combatController == null)
        {
            combatController = FindObjectOfType<SwordCombatController>();
            if (combatController == null)
            {
                Debug.LogError("❌ SwordCombatController not found! Please assign it.");
                return;
            }
        }

        // Store original scales at start
        if (leftMouseButtonImage != null)
            leftMouseOriginalScale = leftMouseButtonImage.transform.localScale;
        if (rightMouseButtonImage != null)
            rightMouseOriginalScale = rightMouseButtonImage.transform.localScale;
        if (rButtonImage != null)
        {
            rButtonOriginalScale = rButtonImage.transform.localScale;
            rButtonOriginalColor = rButtonImage.color;
        }

        // Force initialize UI as hidden
        if (combatUIContainer != null)
        {
            combatUIContainer.SetActive(false);
            isUIVisible = false;
            Debug.Log("🔒 Combat UI forcefully hidden on start");
        }
        else
        {
            Debug.LogError("❌ Combat UI Container not assigned! Please assign the GameObject containing the button images.");
        }

        // Set initial button opacities
        leftMouseTargetOpacity = idleOpacity;
        rightMouseTargetOpacity = idleOpacity;
        rButtonTargetOpacity = idleOpacity;

        SetImageOpacity(leftMouseButtonImage, idleOpacity);
        SetImageOpacity(rightMouseButtonImage, idleOpacity);
        SetImageOpacity(rButtonImage, idleOpacity);

        // Initialize fill to 0
        if (rButtonFillImage != null)
        {
            rButtonFillImage.fillAmount = 0f;
        }

        Debug.Log("✅ CombatUIController initialized");
    }

    private void Update()
    {
        // Check if sword is drawn
        if (combatController != null)
        {
            bool shouldShowUI = combatController.IsSwordDrawn;

            if (shouldShowUI && !isUIVisible)
            {
                ShowCombatUI();
            }
            else if (!shouldShowUI && isUIVisible)
            {
                HideCombatUI();
            }
        }

        // Only update button states if UI is visible
        if (isUIVisible)
        {
            UpdateButtonStates();
            UpdateButtonOpacities();
            UpdateHardAttackReadyState();
        }
    }

    private void ShowCombatUI()
    {
        isUIVisible = true;

        if (combatUIContainer != null)
        {
            combatUIContainer.SetActive(true);
        }

        Debug.Log("🎮 Combat UI shown");
    }

    private void HideCombatUI()
    {
        isUIVisible = false;

        if (combatUIContainer != null)
        {
            combatUIContainer.SetActive(false);
        }

        // Stop ready pulse if active
        StopHardAttackReadyPulse();

        // Reset fill
        if (rButtonFillImage != null)
        {
            rButtonFillImage.fillAmount = 0f;
        }

        Debug.Log("🎮 Combat UI hidden");
    }

    private void UpdateButtonStates()
    {
        // Left Mouse Button (Easy Attack)
        if (Input.GetMouseButtonDown(0))
        {
            leftMouseTargetOpacity = activeOpacity;
            if (useScalePulse) TriggerPulse(leftMouseButtonImage, leftMouseOriginalScale, ref leftMousePulseCoroutine);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            leftMouseTargetOpacity = idleOpacity;
        }

        // Right Mouse Button (Normal Attack)
        if (Input.GetMouseButtonDown(1))
        {
            rightMouseTargetOpacity = activeOpacity;
            if (useScalePulse) TriggerPulse(rightMouseButtonImage, rightMouseOriginalScale, ref rightMousePulseCoroutine);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            rightMouseTargetOpacity = idleOpacity;
        }

        // R Button (Hard Attack) - Check actual combat controller count
        int currentAttackCount = combatController.AttackCounter;
        bool canUseHardAttack = currentAttackCount >= hardAttackRequirement;

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (canUseHardAttack)
            {
                rButtonTargetOpacity = activeOpacity;
                if (useScalePulse) TriggerPulse(rButtonImage, rButtonOriginalScale, ref rButtonPulseCoroutine);
            }
        }
        else if (Input.GetKeyUp(KeyCode.R))
        {
            rButtonTargetOpacity = idleOpacity;
        }

        // Update R button fill based on attack count
        UpdateHardAttackFill();
    }

    private void UpdateHardAttackReadyState()
    {
        if (combatController == null) return;

        int currentAttackCount = combatController.AttackCounter;
        bool shouldBeReady = currentAttackCount >= hardAttackRequirement;

        // Check if hard attack was just used (count dropped below requirement)
        if (isHardAttackReady && currentAttackCount < lastAttackCount)
        {
            // Hard attack was used - reset to normal state
            StopHardAttackReadyPulse();
            ResetRButtonToNormal();
            isHardAttackReady = false;
            Debug.Log("⚔️ Hard attack used - resetting R button");
        }
        // Check if hard attack just became ready
        else if (!isHardAttackReady && shouldBeReady)
        {
            // Start the ready pulse animation
            StartHardAttackReadyPulse();
            isHardAttackReady = true;
            Debug.Log("✨ Hard attack ready - starting pulse");
        }

        lastAttackCount = currentAttackCount;
    }

    private void StartHardAttackReadyPulse()
    {
        if (rButtonImage == null) return;

        // Stop any existing pulse
        StopHardAttackReadyPulse();

        // Change color to yellow
        Color targetColor = hardAttackReadyColor;
        targetColor.a = rButtonImage.color.a; // Preserve current alpha
        rButtonImage.color = targetColor;

        // Start continuous pulse
        hardAttackReadyPulseCoroutine = StartCoroutine(ContinuousPulse(rButtonImage.transform, rButtonOriginalScale));
    }

    private void StopHardAttackReadyPulse()
    {
        if (hardAttackReadyPulseCoroutine != null)
        {
            StopCoroutine(hardAttackReadyPulseCoroutine);
            hardAttackReadyPulseCoroutine = null;
        }
    }

    private void ResetRButtonToNormal()
    {
        if (rButtonImage == null) return;

        // Reset scale
        rButtonImage.transform.localScale = rButtonOriginalScale;

        // Reset color (preserve alpha)
        Color resetColor = rButtonOriginalColor;
        resetColor.a = rButtonImage.color.a;
        rButtonImage.color = resetColor;
    }

    private IEnumerator ContinuousPulse(Transform target, Vector3 originalScale)
    {
        while (true)
        {
            Vector3 targetScale = originalScale * readyPulseScale;

            // Scale up
            float elapsed = 0f;
            while (elapsed < readyPulseSpeed)
            {
                target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / readyPulseSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < readyPulseSpeed)
            {
                target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / readyPulseSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Brief pause at original scale
            target.localScale = originalScale;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void UpdateButtonOpacities()
    {
        // Smoothly transition button opacities
        if (leftMouseButtonImage != null)
        {
            float currentOpacity = leftMouseButtonImage.color.a;
            float newOpacity = Mathf.Lerp(currentOpacity, leftMouseTargetOpacity, Time.deltaTime * opacityTransitionSpeed);
            SetImageOpacity(leftMouseButtonImage, newOpacity);
        }

        if (rightMouseButtonImage != null)
        {
            float currentOpacity = rightMouseButtonImage.color.a;
            float newOpacity = Mathf.Lerp(currentOpacity, rightMouseTargetOpacity, Time.deltaTime * opacityTransitionSpeed);
            SetImageOpacity(rightMouseButtonImage, newOpacity);
        }

        if (rButtonImage != null)
        {
            float currentOpacity = rButtonImage.color.a;
            float newOpacity = Mathf.Lerp(currentOpacity, rButtonTargetOpacity, Time.deltaTime * opacityTransitionSpeed);
            SetImageOpacity(rButtonImage, newOpacity);
        }
    }

    private void UpdateHardAttackFill()
    {
        if (rButtonFillImage == null || combatController == null) return;

        // Get actual attack count from combat controller
        int currentAttackCount = combatController.AttackCounter;

        float targetFill = Mathf.Clamp01((float)currentAttackCount / hardAttackRequirement);
        rButtonFillImage.fillAmount = Mathf.Lerp(rButtonFillImage.fillAmount, targetFill, Time.deltaTime * 10f);

        // Change fill color when ready
        if (currentAttackCount >= hardAttackRequirement)
        {
            rButtonFillImage.color = Color.Lerp(rButtonFillImage.color, hardAttackReadyColor, Time.deltaTime * 5f);
        }
        else
        {
            rButtonFillImage.color = Color.Lerp(rButtonFillImage.color, Color.white, Time.deltaTime * 5f);
        }
    }

    private void SetImageOpacity(Image image, float opacity)
    {
        if (image == null) return;

        Color color = image.color;
        color.a = opacity;
        image.color = color;
    }

    private void TriggerPulse(Image image, Vector3 originalScale, ref Coroutine pulseCoroutine)
    {
        if (image == null) return;

        // Stop previous coroutine and force reset scale
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            image.transform.localScale = originalScale;
        }

        pulseCoroutine = StartCoroutine(PulseScale(image.transform, originalScale));
    }

    private IEnumerator PulseScale(Transform target, Vector3 originalScale)
    {
        // Always start from the original scale
        target.localScale = originalScale;

        Vector3 targetScale = originalScale * pulseScale;

        // Scale up
        float elapsed = 0f;
        while (elapsed < pulseSpeed)
        {
            target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / pulseSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < pulseSpeed)
        {
            target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / pulseSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we end exactly at original scale
        target.localScale = originalScale;
    }

    // Public method to force reset
    public void ForceReset()
    {
        if (rButtonFillImage != null)
        {
            rButtonFillImage.fillAmount = 0f;
        }

        // Stop ready pulse
        StopHardAttackReadyPulse();

        // Reset R button to normal
        ResetRButtonToNormal();
        isHardAttackReady = false;

        // Reset scales for all buttons
        if (leftMouseButtonImage != null)
            leftMouseButtonImage.transform.localScale = leftMouseOriginalScale;
        if (rightMouseButtonImage != null)
            rightMouseButtonImage.transform.localScale = rightMouseOriginalScale;
        if (rButtonImage != null)
            rButtonImage.transform.localScale = rButtonOriginalScale;
    }
}