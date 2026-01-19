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
    private bool playerInRange; // Track if player is in range

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

        // Only allow pickup if player is in range
        if (playerInRange && Input.GetKeyDown(pickup.interactKey))
        {
            StartCoroutine(PickupRoutine());
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
        pickup.TryPickup();

        // Re-enable movement
        foreach (var m in movementScriptsToDisable)
            if (m) m.enabled = true;

        isPickingUp = false;
    }

    // Listen to the InteractPickup's trigger events
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickup.playerTag))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(pickup.playerTag))
        {
            playerInRange = false;
        }
    }
}