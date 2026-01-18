using UnityEngine;
using UnityEngine.Playables;
using TMPro;

public class SubtitleBehaviour : PlayableBehaviour
{
    public string subtitleText;
    public Color textColor = Color.white;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        TextMeshProUGUI textElement = playerData as TextMeshProUGUI;

        if (textElement != null)
        {
            textElement.text = subtitleText;
            textElement.color = textColor;

            // Set alpha based on the weight of the track (allows for fading)
            float alpha = info.weight;
            textElement.canvasRenderer.SetAlpha(alpha);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // Clear text when the clip ends or Timeline stops
        TextMeshProUGUI textElement = info.output.GetUserData() as TextMeshProUGUI;
        if (textElement != null)
        {
            textElement.text = "";
        }
    }
}