using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUILevel3 : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Texts")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private TMP_Text hintText; // optional

    [Header("Choices (fade in)")]
    [SerializeField] private CanvasGroup choicesGroup; // put this on the choices container
    [SerializeField] private Button optionAButton;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private Button optionBButton;
    [SerializeField] private TMP_Text optionBText;

    private Action _onA;
    private Action _onB;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (choicesGroup != null)
        {
            choicesGroup.alpha = 0f;
            choicesGroup.interactable = false;
            choicesGroup.blocksRaycasts = false;
        }

        optionAButton.onClick.AddListener(() => _onA?.Invoke());
        optionBButton.onClick.AddListener(() => _onB?.Invoke());
    }

    public void ShowPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void HidePanel()
    {
        StopFade();
        _onA = null;
        _onB = null;

        HideChoicesInstant();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void SetHint(string msg)
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        hintText.text = msg ?? "";
    }

    public void SetLine(string speaker, string text)
    {
        if (speakerNameText != null) speakerNameText.text = speaker;
        if (lineText != null) lineText.text = text;
    }

    public void HideChoicesInstant()
    {
        StopFade();

        if (choicesGroup != null)
        {
            choicesGroup.alpha = 0f;
            choicesGroup.interactable = false;
            choicesGroup.blocksRaycasts = false;
        }
    }

    public void ShowChoicesFadeIn(string aLabel, string bLabel, Action onA, Action onB, float fadeDuration)
    {
        _onA = onA;
        _onB = onB;

        if (optionAText != null) optionAText.text = aLabel;
        if (optionBText != null) optionBText.text = bLabel;

        StopFade();
        _fadeRoutine = StartCoroutine(FadeChoicesRoutine(fadeDuration));
    }

    private IEnumerator FadeChoicesRoutine(float duration)
    {
        if (choicesGroup == null)
            yield break;

        choicesGroup.alpha = 0f;
        choicesGroup.interactable = false;
        choicesGroup.blocksRaycasts = false;

        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            choicesGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        choicesGroup.alpha = 1f;
        choicesGroup.interactable = true;
        choicesGroup.blocksRaycasts = true;
        _fadeRoutine = null;
    }

    private void StopFade()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }
}
