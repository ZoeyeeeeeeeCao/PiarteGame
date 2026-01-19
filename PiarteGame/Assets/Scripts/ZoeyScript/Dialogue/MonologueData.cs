using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Monologue Data", fileName = "MonologueData")]
public class MonologueData : ScriptableObject
{
    [Header("Name shown in UI")]
    public string speakerName = "???";

    [Header("Lines (one line per page)")]
    [TextArea(2, 8)]
    public string[] lines;

    [Header("Voice (optional, one clip per line)")]
    public AudioClip[] voiceClips;
}
