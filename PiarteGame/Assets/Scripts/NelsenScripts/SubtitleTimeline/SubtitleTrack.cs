using UnityEngine;
using UnityEngine.Timeline;
using TMPro;

[TrackColor(0.1f, 0.8f, 0.4f)]
[TrackClipType(typeof(SubtitleAsset))]
[TrackBindingType(typeof(TextMeshProUGUI))]
public class SubtitleTrack : TrackAsset
{
}