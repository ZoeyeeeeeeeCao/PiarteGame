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
    public float slideSpeed = 0.4f; // Slightly slower for a smoother feel
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

        // FIX: Clear the text immediately so the old dialogue doesn't show for a brief second
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
            // Force box active but it will now be empty because of the clear above
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
        lineIndex++;
        if (lineIndex < currentLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            // Signal finished so the Trigger loop can proceed
            isDialogueActive = false;
            isTyping = false;

            if (shouldAutoClose)
            {
                CloseDialogueBox();
            }
            // If shouldAutoClose is false, we stay visible and do nothing
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

    // Added to help FinishLineInstantly respect the new logic
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