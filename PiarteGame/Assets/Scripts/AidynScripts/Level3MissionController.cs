using System.Collections;
using TMPro;
using UnityEngine;

public class Level3MissionController : MonoBehaviour
{

    [Header("UI - Separate Groups")]
    [SerializeField] private CanvasGroup panelGroup;   // panel/background group (fade in once)
    [SerializeField] private CanvasGroup textGroup;    // text-only group (fade on change)
    [SerializeField] private TextMeshProUGUI missionText;

    [Header("Mission Text")]
    [TextArea][SerializeField] private string mission1 = "Find the last relic stone";
    [TextArea][SerializeField] private string mission2 = "Climb up to the skull cave";

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip missionAppearClip;
    [SerializeField] private AudioClip missionCompleteClip;

    [Header("Timing")]
    [SerializeField] private float panelFadeInTime = 0.4f;
    [SerializeField] private float textFadeOutTime = 0.25f;
    [SerializeField] private float textFadeInTime = 0.25f;
    [SerializeField] private float swapDelay = 0.05f;

    private bool _stoneCollected;
    private Coroutine _routine;

    private void Awake()
    {
        if (panelGroup != null) panelGroup.alpha = 0f; // panel hidden initially
        if (textGroup != null) textGroup.alpha = 1f;   // text visible once panel shows
    }

    private void Start()
    {
        // Set mission 1 immediately
        if (missionText != null) missionText.text = mission1;

        // Panel fades in ONCE, then stays forever
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PanelIntroRoutine());
    }

    private IEnumerator PanelIntroRoutine()
    {
        // Fade in the panel (background + children stay)
        yield return Fade(panelGroup, 0f, 1f, panelFadeInTime);

        // When panel is fully visible, play appear sound once for mission 1
        PlayOneShot(missionAppearClip);
    }

    /// <summary>Call when relic stone is collected.</summary>
    public void OnStoneCollected()
    {
        if (_stoneCollected) return;
        _stoneCollected = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ChangeMissionTextRoutine(mission2));
    }

    private IEnumerator ChangeMissionTextRoutine(string newText)
    {
        // 1) Play complete sound for mission 1
        PlayOneShot(missionCompleteClip);

        // 2) Fade OUT only the text
        yield return Fade(textGroup, 1f, 0f, textFadeOutTime);

        // 3) Small delay (optional, feels cleaner)
        if (swapDelay > 0f) yield return new WaitForSeconds(swapDelay);

        // 4) Swap the text while invisible
        if (missionText != null) missionText.text = newText;

        // 5) Play appear sound for mission 2
        PlayOneShot(missionAppearClip);

        // 6) Fade IN only the text
        yield return Fade(textGroup, 0f, 1f, textFadeInTime);
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        group.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        group.alpha = to;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
