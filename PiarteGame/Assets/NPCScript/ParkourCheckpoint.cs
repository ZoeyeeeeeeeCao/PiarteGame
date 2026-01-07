using UnityEngine;

public class ParkourCheckpoint : MonoBehaviour
{
    [Header("Settings")]
    // IMPORTANT: Set this to 0 for the first one, 1 for the second, etc.
    public int checkpointIndex = 0;

    [Header("Visuals")]
    public ParticleSystem particleEffect;
    public AudioClip checkpointSound;

    private ParkourTutorialSystem manager;
    private bool hasBeenReached = false;

    void Start()
    {
        manager = FindObjectOfType<ParkourTutorialSystem>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenReached) return;

        if (other.CompareTag("Player"))
        {
            CollectCheckpoint();
        }
    }

    void CollectCheckpoint()
    {
        hasBeenReached = true;
        Debug.Log($"✅ Checkpoint {checkpointIndex} COLLECTED!");

        // Notify Manager
        if (manager != null)
        {
            manager.OnCheckpointReached(checkpointIndex);
        }

        // Play Sound
        if (checkpointSound != null) AudioSource.PlayClipAtPoint(checkpointSound, transform.position);

        // Visual Feedback (Particles)
        if (particleEffect != null)
        {
            // Detach particle from parent so it doesn't get destroyed immediately
            particleEffect.transform.parent = null;
            particleEffect.Play();
            Destroy(particleEffect.gameObject, 2f);
        }

        // Destroy the checkpoint object immediately so it disappears
        Destroy(gameObject);
    }
}