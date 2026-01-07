using UnityEngine;

public class WaterHazard : MonoBehaviour
{
    [Header("Settings")]
    public Transform respawnPoint;      // Where the player goes
    public bool resetVelocity = true;   // Stop falling momentum

    [Header("Audio")]
    public AudioClip splashSound;       // Drag your water splash sound here
    [Range(0f, 1f)]
    public float soundVolume = 1.0f;    // Adjust volume (0.0 to 1.0)

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Play Sound
            if (splashSound != null)
            {
                // We play the sound at the Player's position right before they teleport
                AudioSource.PlayClipAtPoint(splashSound, other.transform.position, soundVolume);
            }

            // 2. Respawn
            RespawnPlayer(other.gameObject);
        }
    }

    void RespawnPlayer(GameObject player)
    {
        // Handle CharacterController (prevents glitching during teleport)
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;
            cc.enabled = true;
        }
        // Handle Rigidbody
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
    }
}