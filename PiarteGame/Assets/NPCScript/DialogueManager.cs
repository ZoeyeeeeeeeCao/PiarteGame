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
    public float slideSpeed = 0.4f;
    public float slideDistance = 300f;
    public float closeDelay = 0.1f;

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

        if (dialogueBox != null)
        {
            dialogueBoxRect = dialogueBox.GetComponent<RectTransform>();
            visiblePosition = dialogueBoxRect.anchoredPosition;
            hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y - slideDistance);
            dialogueBoxRect.anchoredPosition = hiddenPosition;
            dialogueBox.SetActive(false);
        }
    }

    void Update()
    {
        if (isDialogueActive && !isAnimating && currentMode == DialogueMode.Dialogue && Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping) FinishLineInstantly();
            else NextLine();
        }
    }

    public void StartDialogue(Dialogue dialogue, DialogueMode mode = DialogueMode.Dialogue, bool animate = true, bool autoClose = true)
    {
        StopAllCoroutines();

        // Clear the text immediately so old dialogue doesn't flash
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

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
            dialogueBox.SetActive(true);
            dialogueBoxRect.anchoredPosition = visiblePosition;
            StartCoroutine(TypeLine());
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        Dialogue.DialogueLine currentLineData = currentLines[lineIndex];
        bool hasSpecificClip = currentLineData.audioClip != null;

        if (hasSpecificClip && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(currentLineData.audioClip);
        }

        int charCount = 0;
        foreach (char c in currentLineData.text.ToCharArray())
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
                while (audioSource.isPlaying) yield return null;
            }
            yield return new WaitForSeconds(subtitleAutoDelay);
            NextLine();
        }
    }

    void NextLine()
    {
        Debug.Log($"[DIALOGUE] NextLine called. lineIndex: {lineIndex}, total lines: {currentLines.Length}");

        lineIndex++;
        if (lineIndex < currentLines.Length)
        {
            Debug.Log($"[DIALOGUE] More lines to show. Starting next line.");
            StartCoroutine(TypeLine());
        }
        else
        {
            Debug.Log($"[DIALOGUE] All lines complete. shouldAutoClose: {shouldAutoClose}");
            isTyping = false;

            // CRITICAL FIX: Set isDialogueActive to false BEFORE starting the close animation
            // This allows other scripts to detect that dialogue content is done
            isDialogueActive = false;
            Debug.Log($"[DIALOGUE] isDialogueActive set to FALSE (dialogue content complete)");

            if (shouldAutoClose)
            {
                Debug.Log($"[DIALOGUE] Starting close animation...");
                StartCoroutine(CloseDialogueBoxWithAnimation());
            }
        }
    }

    public void CloseDialogueBox()
    {
        StartCoroutine(CloseDialogueBoxWithAnimation());
    }

    IEnumerator CloseDialogueBoxWithAnimation()
    {
        Debug.Log($"[DIALOGUE] CloseDialogueBoxWithAnimation started. Waiting {closeDelay}s...");
        yield return new WaitForSeconds(closeDelay);
        Debug.Log($"[DIALOGUE] Starting SlideOut...");
        yield return StartCoroutine(SlideOut());
        Debug.Log($"[DIALOGUE] SlideOut complete.");
        if (spacePromptText != null) spacePromptText.SetActive(false);
        EnablePlayerControls();
    }

    IEnumerator SlideInAndStartDialogue()
    {
        yield return StartCoroutine(SlideIn());
        StartCoroutine(TypeLine());
    }

    IEnumerator SlideIn()
    {
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
        Debug.Log($"[DIALOGUE] SlideOut started. Duration: {slideSpeed}s");
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
        Debug.Log($"[DIALOGUE] SlideOut FINISHED. Box hidden.");
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
}