using UnityEngine;

public class DisappearPlatform : MonoBehaviour
{
    public float delay = 2f;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            Debug.Log("Palyer detected");
            Destroy(gameObject, delay);
        }
    }
}
