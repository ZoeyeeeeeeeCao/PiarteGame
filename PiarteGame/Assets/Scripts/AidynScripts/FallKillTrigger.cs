using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FallKillTrigger : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Player tag to detect.")]
    public string playerTag = "Player";

    [Header("Audio")]
    public AudioClip screamClip;
    [Range(0f, 1f)] public float screamVolume = 1f;

    [Tooltip("Seconds to wait after scream starts before killing player.")]
    public float killDelay = 0.6f;

    [Header("Behavior")]
    [Tooltip("If true, prevent multiple triggers.")]
    public bool triggerOncePerEntry = true;

    [Tooltip("If true, disable player movement immediately (optional).")]
    public bool disableMovementImmediately = true;

    [Tooltip("If true, force player velocity to 0 (helps if Rigidbody).")]
    public bool zeroVelocity = true;

    bool _triggered;

    private void Reset()
    {
        // Ensure collider is trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered && triggerOncePerEntry) return;
        if (!other.CompareTag(playerTag)) return;

        _triggered = true;
        StartCoroutine(ScreamThenKill(other.gameObject));
    }

    private IEnumerator ScreamThenKill(GameObject player)
    {
        // Optional: stop movement ASAP so they don't keep falling forever
        if (disableMovementImmediately)
        {
            // CharacterController-based player
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Rigidbody-based player
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (zeroVelocity) rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // If you have your own controller script, also disable it here:
            // var controller = player.GetComponent<PlayerController>();
            // if (controller != null) controller.enabled = false;
        }

        // Play scream (2D sound not tied to position)
        if (screamClip != null)
            AudioSource.PlayClipAtPoint(screamClip, Camera.main ? Camera.main.transform.position : player.transform.position, screamVolume);

        // Wait, then kill
        yield return new WaitForSeconds(killDelay);

        // --- Call your health/death system here ---
        // If you're using your PlayerHealthController singleton from earlier:
        if (PlayerHealthController.Instance != null)
        {
            PlayerHealthController.Instance.Damage(999999f);
        }
        else
        {
            // Fallback: destroy player (not recommended, but prevents softlock)
            Debug.LogWarning("PlayerHealthController.Instance not found. Destroying player as fallback.");
            Destroy(player);
        }
    }
}
