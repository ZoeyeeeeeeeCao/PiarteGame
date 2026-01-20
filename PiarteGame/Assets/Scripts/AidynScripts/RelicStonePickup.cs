using UnityEngine;

public class RelicStonePickup : MonoBehaviour
{
    [SerializeField] private Level3MissionController missionController;

    [Header("Trigger To Enable After Pickup")]
    [SerializeField] private Collider triggerToEnable; // BoxCollider (isTrigger = true)

    private bool _pickedUp;

    private void Awake()
    {
        // Make sure it's disabled at start
        if (triggerToEnable != null)
            triggerToEnable.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_pickedUp) return;
        if (!other.CompareTag("Player")) return;

        _pickedUp = true;

        // Mission update
        if (missionController != null)
            missionController.OnStoneCollected();

        // Enable the new trigger
        if (triggerToEnable != null)
            triggerToEnable.enabled = true;
    }
}
