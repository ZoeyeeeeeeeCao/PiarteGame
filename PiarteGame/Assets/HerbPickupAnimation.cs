using UnityEngine;
using System.Collections;

[RequireComponent(typeof(InteractPickup))]
public class HerbPickupAnimation : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string pickupTrigger = "PickHerb";
    [SerializeField] private float animationDuration = 1.0f;

    [Header("Player Control")]
    [Tooltip("Optional: disable player movement during pickup")]
    [SerializeField] private MonoBehaviour[] movementScriptsToDisable;

    private InteractPickup pickup;
    private bool isPickingUp;

    private void Awake()
    {
        pickup = GetComponent<InteractPickup>();

        if (playerAnimator == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerAnimator = player.GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (isPickingUp) return;
        if (!pickup) return;

        // Let InteractPickup handle range detection
        if (Input.GetKeyDown(pickup.interactKey) && pickup.enabled)
        {
            if (pickup.enabled)
            {
                StartCoroutine(PickupRoutine());
            }
        }
    }

    private IEnumerator PickupRoutine()
    {
        isPickingUp = true;

        // Disable movement
        foreach (var m in movementScriptsToDisable)
            if (m) m.enabled = false;

        // Play animation
        if (playerAnimator != null && !string.IsNullOrEmpty(pickupTrigger))
        {
            playerAnimator.SetTrigger(pickupTrigger);
        }

        // Wait for animation
        yield return new WaitForSeconds(animationDuration);

        // Perform actual pickup
        pickup.enabled = true;   // make sure it's enabled
        pickup.TryPickup();
        // force pickup logic once

        // Re-enable movement
        foreach (var m in movementScriptsToDisable)
            if (m) m.enabled = true;
    }
}
