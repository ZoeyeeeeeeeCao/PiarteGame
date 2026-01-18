using System.Collections;
using TMPro;
using UnityEngine;

public class SubtitlesUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI subtitleText = default;
    public static SubtitlesUI instance;

    private void Awake()
    {
        instance = this;
        ClearSubtitle();
    }

    public void SetSubtitles(string[] lines, float totalDuration, float[] customDurations = null)
    {
        StopAllCoroutines(); // In case a subtitle is still running
        StartCoroutine(ShowSubtitlesRoutine(lines, totalDuration, customDurations));
    }

    private IEnumerator ShowSubtitlesRoutine(string[] lines, float totalDuration, float[] customDurations)
    {
        float timePerLine = totalDuration / lines.Length;

        for (int i = 0; i < lines.Length; i++)
        {
            subtitleText.text = lines[i];

            float waitTime = (customDurations != null && i < customDurations.Length) ? customDurations[i] : timePerLine;
            yield return new WaitForSeconds(waitTime);
        }

        ClearSubtitle();
    }

    public void ClearSubtitle()
    {
        subtitleText.text = "";
    }
}
