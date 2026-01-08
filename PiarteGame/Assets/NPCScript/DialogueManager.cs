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
    [Tooltip("The 'Space' or 'Enter' prompt text object")]
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

    private RectTransform dialogueBoxRect;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

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

        if (spacePromptText != null)
            spacePromptText.SetActive(false);
    }

    void Update()
    {
        if (isDialogueActive && !isAnimating && currentMode == DialogueMode.Dialogue && Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                FinishLineInstantly();
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(Dialogue dialogue, DialogueMode mode = DialogueMode.Dialogue, bool animate = true)
    {
        // --- FIX: STOP GLITCHING ---
        // Stop any current typewriter effects or slide-out routines
        StopAllCoroutines();

        // Immediately clear old text so it doesn't flash
        if (dialogueText != null) dialogueText.text = "";
        // ---------------------------

        currentMode = mode;
        isDialogueActive = true;
        currentLines = dialogue.dialogueLines;
        lineIndex = 0;

        if (npcNameText != null)
            npcNameText.text = dialogue.npcName;

        if (spacePromptText != null)
        {
            spacePromptText.SetActive(currentMode == DialogueMode.Dialogue);
        }

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
                if (dialogueBoxRect != null)
                    dialogueBoxRect.anchoredPosition = visiblePosition;
            }
            StartCoroutine(TypeLine());
        }
    }

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
            float t = elapsed / slideSpeed;
            t = t * t * (3f - 2f * t);
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
            float t = elapsed / slideSpeed;
            t = t * t * (3f - 2f * t);
            dialogueBoxRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, t);
            yield return null;
        }

        dialogueBoxRect.anchoredPosition = hiddenPosition;
        dialogueBox.SetActive(false);
        isAnimating = false;
    }

    IEnumerator TypeLine()
    {
        isTyping = true;

        // Ensure text is empty before starting typewriter
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

        if (currentMode == DialogueMode.Subtitle)
        {
            if (hasSpecificClip)
            {
                while (audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            yield return new WaitForSeconds(subtitleAutoDelay);
            NextLine();
        }
    }

    void FinishLineInstantly()
    {
        // Stop current typing coroutine
        StopAllCoroutines();
        dialogueText.text = currentLines[lineIndex].text;
        isTyping = false;

        if (stopAudioOnSkip && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (currentMode == DialogueMode.Subtitle)
        {
            StartCoroutine(WaitAndNextLine());
        }
    }

    IEnumerator WaitAndNextLine()
    {
        Dialogue.DialogueLine currentLineData = currentLines[lineIndex];
        bool hasSpecificClip = currentLineData.audioClip != null;

        if (hasSpecificClip && audioSource != null && audioSource.isPlaying)
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(subtitleAutoDelay);
        NextLine();
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

            if (currentMode == DialogueMode.Subtitle)
            {
                CloseDialogueBox();
            }

            Debug.Log("📝 Dialogue part finished");
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

        if (spacePromptText != null)
            spacePromptText.SetActive(false);

        EnablePlayerControls();
    }

    public void EndDialogue(bool animate = true)
    {
        // Stop typewriter immediately if manually ending
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
            if (spacePromptText != null) spacePromptText.SetActive(false);
            EnablePlayerControls();
        }
    }

    IEnumerator EndDialogueWithAnimation()
    {
        isTyping = false;
        yield return new WaitForSeconds(closeDelay);
        yield return StartCoroutine(SlideOut());
        isDialogueActive = false;
        if (spacePromptText != null) spacePromptText.SetActive(false);
        EnablePlayerControls();
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