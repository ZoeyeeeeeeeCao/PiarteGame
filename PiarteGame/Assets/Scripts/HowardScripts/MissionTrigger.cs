using UnityEngine;

public class MissionTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Destroy this trigger object after the player hits it?")]
    public bool destroyAfterUse = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check for the Player tag
        if (other.CompareTag("Player") && !hasTriggered)
        {
            if (MissionManager.Instance != null)
            {
                // Logic: Manager decides if it should show the 1st mission or move to the 2nd/3rd
                MissionManager.Instance.StartNextMission();

                hasTriggered = true;

                if (destroyAfterUse)
                {
                    Destroy(gameObject, 0.1f);
                }
            }
            else
            {
                Debug.LogWarning("MissionManager Instance not found in scene!");
            }
        }
    }
}