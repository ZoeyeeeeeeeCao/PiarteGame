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
        hasActivated = true; // Lock it so it doesn't happen twice

        Debug.Log("Interaction Successful!");

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
        }

        // Hide the "Press E" UI immediately
        if (interactTextUI != null) interactTextUI.SetActive(false);

        // Optional: Play a sound here if you have an AudioSource
        // GetComponent<AudioSource>()?.Play();
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