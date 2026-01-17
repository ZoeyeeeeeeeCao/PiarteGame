using UnityEngine;

public class RelicStonePickup : MonoBehaviour
{
    [SerializeField] private Level3MissionController missionController;

    private bool _pickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (_pickedUp) return;
        if (!other.CompareTag("Player")) return;

        _pickedUp = true;

        if (missionController != null)
            missionController.OnStoneCollected();
    }
}
