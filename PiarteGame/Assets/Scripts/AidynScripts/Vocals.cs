using UnityEngine;

public class Vocals : MonoBehaviour
{
    private AudioSource source;
    public static Vocals instance;

    private void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
    }

    public void Say(AudioObject clip)
    {
        if (source.isPlaying)
            source.Stop();

        source.PlayOneShot(clip.clip);

        SubtitlesUI.instance.SetSubtitles(
            clip.subtitleLines,
            clip.clip.length,
            clip.useCustomTimings ? clip.customDurations : null
        );
    }

}
