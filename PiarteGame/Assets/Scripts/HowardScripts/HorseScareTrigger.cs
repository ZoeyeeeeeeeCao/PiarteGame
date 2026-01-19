using UnityEngine;
using System.Collections.Generic; // Needed for Lists

public class HorseScareTrigger : MonoBehaviour
{
    [Header("Setup")]
    public CartMover horseScript;
    public string playerTag = "Player";

    [Header("Escape Path")]
    [Tooltip("Drag the Empty GameObjects for the escape route here in order")]
    public List<Transform> escapePath; // Now a list!

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag(playerTag))
        {
            hasTriggered = true;

            if (horseScript != null && escapePath.Count > 0)
            {
                Debug.Log("Horse scared! Running escape path...");
                horseScript.TriggerRunAway(escapePath);
            }
        }
    }
}