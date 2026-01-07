using UnityEngine;
using System.Collections;

public class RestrictedAreaBlocker : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("The UI Panel or Image that says 'AREA OFF LIMITS'")]
    public GameObject offLimitsUI;
    public float messageDuration = 2.0f;

    private bool isMessageActive = false;

    void Start()
    {
        if (offLimitsUI != null) offLimitsUI.SetActive(false);
    }

    // Use OnCollisionEnter if your wall is solid (not IsTrigger)
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ShowWarning();
        }
    }

    // Use OnTriggerEnter if your wall is a Trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ShowWarning();
        }
    }

    void ShowWarning()
    {
        if (!isMessageActive && offLimitsUI != null)
        {
            StartCoroutine(DisplayMessage());
        }
    }

    IEnumerator DisplayMessage()
    {
        isMessageActive = true;
        offLimitsUI.SetActive(true);

        // Play a sound error if you want (Optional)
        // AudioSource.PlayClipAtPoint(errorClip, transform.position);

        yield return new WaitForSeconds(messageDuration);

        offLimitsUI.SetActive(false);
        isMessageActive = false;
    }
}