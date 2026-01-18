using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Object", menuName = "Assets/New Audio Object")]
public class AudioObject : ScriptableObject
{
    public AudioClip clip;

    [TextArea(2, 5)]
    public string[] subtitleLines; // Multiple subtitle lines

    // Optional: override default timing?
    public bool useCustomTimings = false;
    public float[] customDurations; // Must match subtitleLines.Length if used
}
