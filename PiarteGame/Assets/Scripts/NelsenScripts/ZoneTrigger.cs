using UnityEngine;
using UnityEngine.Events;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string targetTag = "Player";
    public KeyCode interactionKey = KeyCode.E; // Added for easy changing in Inspector
    public bool triggerOnce = true;

    [Header("Events")]
    public UnityEvent onPlayerEnter;

    private bool _hasTriggered = false;

    // We use OnTriggerStay because Enter only triggers for one frame
    private void OnTriggerStay(Collider other)
    {
        // 1. Check if the object is the player
        if (other.CompareTag(targetTag))
        {
            // 2. Check if the key is pressed
            if (Input.GetKeyDown(interactionKey))
            {
                // 3. Check if we are only allowed to trigger once
                if (triggerOnce && _hasTriggered) return;

                ExecuteTrigger(other.name);
            }
        }
    }

    private void ExecuteTrigger(string activatorName)
    {
        Debug.Log("Activated!");
        onPlayerEnter.Invoke();
        _hasTriggered = true;
        Debug.Log($"Trigger activated by: {activatorName}");
    }
}