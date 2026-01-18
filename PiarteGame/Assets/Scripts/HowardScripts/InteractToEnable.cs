using UnityEngine;

public class InteractToEnable : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag the object you want to turn ON here (e.g., the Portal or Scene Changer)")]
    public GameObject objectToEnable;

    [Tooltip("Press this key to activate")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Detection")]
    public string playerTag = "Player";
    public GameObject interactTextUI; // Optional: Drag a UI text here that says "Press E"

    private bool playerInRange = false;
    private bool hasActivated = false; // Ensures we only trigger it once

    void Start()
    {
        // Ensure the UI is hidden at start
        if (interactTextUI != null) interactTextUI.SetActive(false);

        // Ensure the target object is hidden (optional, remove if you want to handle this manually)
        if (objectToEnable != null) objectToEnable.SetActive(false);
    }

    void Update()
    {
        // Only check input if player is close and we haven't already done it
        if (playerInRange && !hasActivated && Input.GetKeyDown(interactKey))
        {
            ActivateObject();
        }
    }

    void ActivateObject()
    {
        hasActivated = true;

        Debug.Log("Interaction Successful!");

        // Option A: If you want to ENABLE something (like a portal)
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
        }

        // Option B: If you want to DESTROY this object (the stone itself)
        // Add this line to make the object you clicked on disappear forever
        Destroy(gameObject);

        if (interactTextUI != null) interactTextUI.SetActive(false);
    }

    // --- DETECTION LOGIC ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (interactTextUI != null) interactTextUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactTextUI != null) interactTextUI.SetActive(false);
        }
    }
}