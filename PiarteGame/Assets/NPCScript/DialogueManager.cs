using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public enum DialogueMode { Dialogue, Subtitle }
    private DialogueMode currentMode = DialogueMode.Dialogue;

    [Header("UI References")]
    public GameObject dialogueBox;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject spacePromptText;

    [Header("Settings")]
    public float typeSpeed = 0.05f;
    public float subtitleAutoDelay = 1.0f;

    [Header("Animation Settings")]
    public float slideSpeed = 0.3f;
    public float slideDistance = 300f;
    public float closeDelay = 0.2f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip defaultTypingClip;
    public bool stopAudioOnSkip = true;

    [Range(1, 5)]
    public int frequencyLevel = 2;

    [Header("Player Control")]
    public MonoBehaviour playerController;
    public MonoBehaviour locomotionController;

    private Dialogue.DialogueLine[] currentLines;
    private int lineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private bool isAnimating = false;
    private bool shouldAutoClose = true;

    private RectTransform dialogueBoxRect;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (dialogueBox != null)
        {
            dialogueBoxRect = dialogueBox.GetComponent<RectTransform>();
            if (dialogueBoxRect != null)
            {
                visiblePosition = dialogueBoxRect.anchoredPosition;
                hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y - slideDistance);
                dialogueBoxRect.anchoredPosition = hiddenPosition;
            }
            dialogueBox.SetActive(false);
        }
        if (spacePromptText != null) spacePromptText.SetActive(false);
    }

    void Update()
    {
        // Only allow manual "NextLine" via Enter if in Dialogue mode (not Subtitle)
        if (isDialogueActive && !isAnimating && currentMode == DialogueMode.Dialogue && Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping) FinishLineInstantly();
            else NextLine();
        }
    }

    // UPDATED: Now supports autoClose. Set this to FALSE when chaining multiple dialogue assets.
    public void StartDialogue(Dialogue dialogue, DialogueMode mode = DialogueMode.Dialogue, bool animate = true, bool autoClose = true)
    {
        StopAllCoroutines();
        if (dialogueText != null) dialogueText.text = "";

        currentMode = mode;
        isDialogueActive = true;
        shouldAutoClose = autoClose;
        currentLines = dialogue.dialogueLines;
        lineIndex = 0;

        if (npcNameText != null) npcNameText.text = dialogue.npcName;
        if (spacePromptText != null) spacePromptText.SetActive(currentMode == DialogueMode.Dialogue);

        DisablePlayerControls();

        if (animate)
        {
            StartCoroutine(SlideInAndStartDialogue());
        }
        else
        {
            if (dialogueBox != null)
            {
                dialogueBox.SetActive(true);
                // FORCE POSITION: Ensures the box is at visiblePosition immediately
                if (dialogueBoxRect != null) dialogueBoxRect.anchoredPosition = visiblePosition;
            }
            StartCoroutine(TypeLine());
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        Dialogue.DialogueLine currentLineData = currentLines[lineIndex];
        string lineToType = currentLineData.text;
        bool hasSpecificClip = currentLineData.audioClip != null;

        if (hasSpecificClip && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(currentLineData.audioClip);
        }

        int charCount = 0;
        foreach (char c in lineToType.ToCharArray())
        {
            dialogueText.text += c;
            charCount++;

            if (!hasSpecificClip && audioSource != null && defaultTypingClip != null)
            {
                if (charCount % frequencyLevel == 0)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(defaultTypingClip);
                }
            }
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;

        // Auto-advance for Subtitles
        if (currentMode == DialogueMode.Subtitle)
        {
            if (hasSpecificClip)
            {
                while (audioSource.isPlaying) yield return null;
            }
            yield return new WaitForSeconds(subtitleAutoDelay);
            NextLine();
        }
    }

    void NextLine()
    {
        lineIndex++;
        if (lineIndex < currentLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            isDialogueActive = false;
            isTyping = false;

            // ONLY auto-close if the instruction allows it.
            // If shouldAutoClose is false, the box stays visible and static.
            if (shouldAutoClose)
            {
                CloseDialogueBox();
            }
        }
    }

    public void CloseDialogueBox()
    {
        StartCoroutine(CloseDialogueBoxWithAnimation());
    }

    IEnumerator CloseDialogueBoxWithAnimation()
    {
        yield return new WaitForSeconds(closeDelay);
        yield return StartCoroutine(SlideOut());
        if (spacePromptText != null) spacePromptText.SetActive(false);
        EnablePlayerControls();
    }

    public void EndDialogue(bool animate = true)
    {
        StopAllCoroutines();
        if (animate)
        {
            StartCoroutine(EndDialogueWithAnimation());
        }
        else
        {
            isDialogueActive = false;
            isTyping = false;
            if (dialogueBox != null) dialogueBox.SetActive(false);
            if (dialogueBoxRect != null) dialogueBoxRect.anchoredPosition = hiddenPosition;
            EnablePlayerControls();
        }
    }

    // Animation Logic
    IEnumerator SlideInAndStartDialogue()
    {
        yield return StartCoroutine(SlideIn());
        StartCoroutine(TypeLine());
    }

    IEnumerator SlideIn()
    {
        if (dialogueBox == null || dialogueBoxRect == null) yield break;
        isAnimating = true;
        dialogueBox.SetActive(true);
        float elapsed = 0f;
        while (elapsed < slideSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / slideSpeed);
            dialogueBoxRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, t);
            yield return null;
        }
        dialogueBoxRect.anchoredPosition = visiblePosition;
        isAnimating = false;
    }

    IEnumerator SlideOut()
    {
        if (dialogueBox == null || dialogueBoxRect == null) yield break;
        isAnimating = true;
        float elapsed = 0f;
        while (elapsed < slideSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / slideSpeed);
            dialogueBoxRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, t);
            yield return null;
        }
        dialogueBoxRect.anchoredPosition = hiddenPosition;
        dialogueBox.SetActive(false);
        isAnimating = false;
    }

    IEnumerator EndDialogueWithAnimation()
    {
        isTyping = false;
        yield return new WaitForSeconds(closeDelay);
        yield return StartCoroutine(SlideOut());
        isDialogueActive = false;
        EnablePlayerControls();
    }

    void FinishLineInstantly()
    {
        StopAllCoroutines();
        dialogueText.text = currentLines[lineIndex].text;
        isTyping = false;
        if (stopAudioOnSkip && audioSource.isPlaying) audioSource.Stop();
        if (currentMode == DialogueMode.Subtitle) StartCoroutine(WaitAndNextLine());
    }

    IEnumerator WaitAndNextLine()
    {
        yield return new WaitForSeconds(subtitleAutoDelay);
        NextLine();
    }

    void DisablePlayerControls()
    {
        if (currentMode == DialogueMode.Subtitle) return;
        if (playerController != null) playerController.enabled = false;
        if (locomotionController != null) locomotionController.enabled = false;
    }

    void EnablePlayerControls()
    {
        if (playerController != null) playerController.enabled = true;
        if (locomotionController != null) locomotionController.enabled = true;
    }

    public bool IsDialogueActive() => isDialogueActive;
}