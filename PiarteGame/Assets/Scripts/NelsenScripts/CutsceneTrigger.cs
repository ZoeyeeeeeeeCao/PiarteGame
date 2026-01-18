using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene")]
    public CutsceneController cutsceneController;

    [Header("Settings")]
    public string playerTag = "Player";
    public bool playOnlyOnce = true;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed && playOnlyOnce) return;

        if (other.CompareTag(playerTag))
        {
            if (cutsceneController != null)
            {
                cutsceneController.ActivateSequence();
                hasPlayed = true;
            }
            else
            {
                Debug.LogWarning("CutsceneController not assigned!", this);
            }
        }
    }
}
