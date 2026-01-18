using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad;
    public string playerTag = "Player";

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            // --- THE FIX IS HERE ---
            // Change "SceneLoader" to "SceneLoaderHoward"
            if (SceneLoaderHoward.Instance != null)
            {
                hasTriggered = true;
                SceneLoaderHoward.Instance.LoadLevel(sceneToLoad);
            }
            else
            {
                Debug.LogError("SceneLoaderHoward Instance not found! Check your GameManager.");
            }
        }
    }
}