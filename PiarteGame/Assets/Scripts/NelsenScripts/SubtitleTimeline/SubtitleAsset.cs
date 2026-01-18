using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class SubtitleAsset : PlayableAsset
{
    public string subtitleText;
    public Color textColor = Color.white;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<SubtitleBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        behaviour.subtitleText = subtitleText;
        behaviour.textColor = textColor;

        return playable;
    }
}