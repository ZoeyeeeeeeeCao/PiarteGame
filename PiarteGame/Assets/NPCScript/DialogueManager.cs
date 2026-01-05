using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogueBox;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject continueIndicator; // Optional: "Press Space" text

    [Header("Typewriter Settings")]
    public float typeSpeed = 0.05f; // Time between each character
    public bool canSkipTyping = true; // Press Space to instantly show full text

    [Header("Animation Settings")]
    public float slideSpeed = 0.3f; // Duration of slide animation
    public float slideDistance = 300f; // How far to slide from bottom
    public float closeDelay = 0.2f; // Delay before sliding down

    private string[] currentDialogueLines;
    private int currentLineIndex = 0;
    private bool dialogueActive = false;
    private bool isTyping = false;
    private bool isAnimating = false;
    private Coroutine typingCoroutine;

    private RectTransform dialogueBoxRect;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    void Start()
    {
        if (dialogueBox != null)
        {
            dialogueBoxRect = dialogueBox.GetComponent<RectTransform>();

            if (dialogueBoxRect != null)
            {
                // Store the visible position (current position in editor)
                visiblePosition = dialogueBoxRect.anchoredPosition;
                // Calculate hidden position (below screen)
                hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y - slideDistance);
                // Start hidden
                dialogueBoxRect.anchoredPosition = hiddenPosition;
            }

            dialogueBox.SetActive(false);
        }
    }

    void Update()
    {
        if (dialogueActive && !isAnimating && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping && canSkipTyping)
            {
                // Skip typing animation
                SkipTyping();
            }
            else if (!isTyping)
            {
                // Go to next line
                DisplayNextLine();
            }
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (dialogue == null || dialogue.dialogueLines.Length == 0)
            return;

        dialogueActive = true;
        currentDialogueLines = dialogue.dialogueLines;
        currentLineIndex = 0;

        if (npcNameText != null)
            npcNameText.text = dialogue.npcName;

        // Slide up animation
        StartCoroutine(SlideIn());

        DisplayLine(currentDialogueLines[currentLineIndex]);
    }

    void DisplayNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentDialogueLines.Length)
        {
            DisplayLine(currentDialogueLines[currentLineIndex]);
        }
        else
        {
            StartCoroutine(EndDialogueWithAnimation());
        }
    }

    void DisplayLine(string line)
    {
        // Stop any existing typing animation
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Start typing animation
        typingCoroutine = StartCoroutine(TypeText(line));

        // Hide continue indicator while typing
        if (continueIndicator != null)
            continueIndicator.SetActive(false);
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;

        // Show continue indicator after typing (hide on last line)
        if (continueIndicator != null)
        {
            bool isLastLine = currentLineIndex >= currentDialogueLines.Length - 1;
            continueIndicator.SetActive(!isLastLine);
        }
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Show full text immediately
        dialogueText.text = currentDialogueLines[currentLineIndex];
        isTyping = false;

        // Show continue indicator
        if (continueIndicator != null)
        {
            bool isLastLine = currentLineIndex >= currentDialogueLines.Length - 1;
            continueIndicator.SetActive(!isLastLine);
        }
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
            // Smooth easing
            t = t * t * (3f - 2f * t); // Smoothstep

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
            // Smooth easing
            t = t * t * (3f - 2f * t); // Smoothstep

            dialogueBoxRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, t);
            yield return null;
        }

        dialogueBoxRect.anchoredPosition = hiddenPosition;
        dialogueBox.SetActive(false);
        isAnimating = false;
    }

    IEnumerator EndDialogueWithAnimation()
    {
        dialogueActive = false;
        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Small delay before sliding down
        yield return new WaitForSeconds(closeDelay);

        // Slide down before closing
        yield return StartCoroutine(SlideOut());

        currentDialogueLines = null;
        currentLineIndex = 0;
    }
}