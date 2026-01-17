using UnityEngine;

public class WaterHazard : MonoBehaviour
{
    [Header("Settings")]
    public Transform respawnPoint;
    public bool resetVelocity = true;

    [Header("Audio")]
    [Tooltip("If you don't assign an AudioSource, the script will look for one on this object.")]
    public AudioSource audioSource;
    public AudioClip splashSound;
    [Range(0f, 1f)]
    public float soundVolume = 1.0f;

    void Start()
    {
        // Automatically try to find an AudioSource if one wasn't dragged in
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Play Sound
            PlaySplashSound();

            // 2. Respawn
            RespawnPlayer(other.gameObject);
        }
    }

    void PlaySplashSound()
    {
        if (audioSource != null && splashSound != null)
        {
            // PlayOneShot is best because it doesn't cut off if the sound triggers again quickly
            audioSource.PlayOneShot(splashSound, soundVolume);
        }
        else if (splashSound != null)
        {
            // Fallback if no AudioSource is found
            AudioSource.PlayClipAtPoint(splashSound, transform.position, soundVolume);
        }
    }

    void RespawnPlayer(GameObject player)
    {
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null)
        {
            cc.enabled = false;
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;
            cc.enabled = true;
        }
        else
        {
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;

            if (resetVelocity)
            {
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        Debug.Log("Player Respawned!");
    }
}