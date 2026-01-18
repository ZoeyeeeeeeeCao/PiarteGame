using UnityEngine;

public class TriggerAudioText : MonoBehaviour
{
    public AudioObject clipToPlay;
    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasPlayed && other.CompareTag("Player"))
        {
            hasPlayed = true;
            Vocals.instance.Say(clipToPlay);
        }
    }
}
