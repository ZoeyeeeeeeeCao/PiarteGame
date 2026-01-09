using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TwoWayInteraction : MonoBehaviour
{
    // --- DATA STRUCTURES ---
    public enum Speaker { NPC, Player }

    [System.Serializable]
    public struct ConversationLine
    {
        public Speaker speaker;
        [TextArea(3, 10)] public string text;
        public AudioClip voiceLine;
    }

    // --- SETTINGS ---
    [Header("Conversation Data")]
    public string npcName = "Villager";
    public string playerName = "Me";
    public List<ConversationLine> conversationLines;

    [Header("Text Animation Settings")]
    public float typingSpeed = 0.05f;

    [Header("UI Animation Settings")]
    public float slideSpeed = 0.5f;     // Speed of the slide
    public float slideDistance = 500f;  // Distance to slide from (make sure this is large enough)
    public bool slideFromBottom = true; // Check true if panel is at bottom, false if at top

    [Header("Audio Settings")]
    public AudioSource audioSource;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Detection Settings")]
    public float detectionRadius = 3f;
    public string playerTag = "Player";
    public GameObject particleEffect;

    [Header("Mission Settings")]
    public bool countsTowardsMission = false;
    private bool hasCounted = false;

    [Header("Animation Settings (Optional)")]
    public bool shouldStandUp = false;
    public Animator npcAnimator;
    public string standParameter = "isStanding";

    [Header("Cutscene Settings (End of Dialogue)")]
    public GameObject cutsceneCamera;
    public GameObject objectToEnable;
    public float cutsceneDuration = 2.0f;

    // --- PRIVATE VARIABLES ---
    private bool playerInRange = false;
    private bool isTalking = false;
    private int currentLineIndex = 0;
    private bool interactionFinished = false;

    // Typewriter variables
    private bool isTyping = false;
    private string currentFullSentence = "";
    private Coroutine typingCoroutine;
    private Coroutine animationCoroutine;

    // Position variables
    private RectTransform dialogueBoxRect;
    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;

    // NPC Position restoration
    private Quaternion originalRotation;
    private Vector3 originalPosition;

    void Start()
    {
        // --- UI SETUP ---
        if (dialoguePanel != null)
        {
            dialogueBoxRect = dialoguePanel.GetComponent<RectTransform>();

            // 1. Capture the EXACT position you set in the Editor as the "Visible" target
            if (dialogueBoxRect != null)
            {
                visiblePosition = dialogueBoxRect.anchoredPosition;
            }

            // 2. Hide the panel immediately
            dialoguePanel.SetActive(false);
        }

        if (particleEffect != null) particleEffect.SetActive(true);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);

        if (shouldStandUp && npcAnimator != null)
        {
            npcAnimator.SetBool(standParameter, false);
        }
    }

    void Update()
    {
        if (interactionFinished) return;

        CheckForPlayer();

        if (playerInRange && !isTalking && !interactionFinished && Input.GetKeyDown(KeyCode.E))
        {
            StartConversation();
        }
        else if (isTalking && Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                // Skip typing
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                dialogueText.text = currentFullSentence;
                isTyping = false;
            }
            else
            {
                DisplayNextLine();
            }
        }
    }

    void StartConversation()
    {
        isTalking = true;
        currentLineIndex = -1;

        if (particleEffect != null) particleEffect.SetActive(false);

        // --- START SLIDE IN ---
        if (dialoguePanel != null && dialogueBoxRect != null)
        {
            // 1. Calculate hidden position based on where it needs to end up
            float yOffset = slideFromBottom ? -slideDistance : slideDistance;
            hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y + yOffset);

            // 2. FORCE position to hidden BEFORE enabling
            dialogueBoxRect.anchoredPosition = hiddenPosition;

            // 3. Enable it
            dialoguePanel.SetActive(true);

            // 4. Animate it
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(SlideUI(hiddenPosition, visiblePosition));
        }

        // Save NPC transforms
        originalRotation = transform.rotation;
        originalPosition = transform.position;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            StartCoroutine(SmoothLookAt(player.transform.position));
        }

        if (shouldStandUp && npcAnimator != null)
        {
            npcAnimator.SetBool(standParameter, true);
        }

        DisplayNextLine();
    }

    // --- GENERIC SLIDE COROUTINE ---
    IEnumerator SlideUI(Vector2 startPos, Vector2 endPos)
    {
        float elapsed = 0f;
        while (elapsed < slideSpeed)
        {
            elapsed += Time.deltaTime;
            // SmoothStep creates a nice "Ease In / Ease Out" effect
            float t = Mathf.SmoothStep(0, 1, elapsed / slideSpeed);
            dialogueBoxRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        // Ensure it ends exactly at the target
        dialogueBoxRect.anchoredPosition = endPos;
    }

    void DisplayNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= conversationLines.Count)
        {
            StartCoroutine(EndSequence());
            return;
        }

        ConversationLine currentLine = conversationLines[currentLineIndex];

        if (nameText != null)
        {
            if (currentLine.speaker == Speaker.NPC)
            {
                nameText.text = npcName;
                nameText.color = Color.yellow;
            }
            else
            {
                nameText.text = playerName;
                nameText.color = Color.cyan;
            }
        }

        if (dialogueText != null)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(currentLine.text));
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            if (currentLine.voiceLine != null) audioSource.PlayOneShot(currentLine.voiceLine);
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        currentFullSentence = sentence;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    IEnumerator EndSequence()
    {
        interactionFinished = true;

        if (countsTowardsMission && !hasCounted)
        {
            hasCounted = true;
            // if (MissionManager.Instance != null) MissionManager.Instance.AddProgressToCurrent();
        }

        // --- SLIDE OUT ---
        if (dialoguePanel != null && dialogueBoxRect != null)
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            // Wait for the slide out to finish
            yield return StartCoroutine(SlideUI(visiblePosition, hiddenPosition));
            dialoguePanel.SetActive(false);
        }

        if (audioSource != null) audioSource.Stop();

        Debug.Log("Starting Cutscene...");

        if (objectToEnable != null) objectToEnable.SetActive(true);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(true);

        yield return new WaitForSeconds(cutsceneDuration);

        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);

        if (shouldStandUp && npcAnimator != null)
        {
            npcAnimator.SetBool(standParameter, false);
        }

        StartCoroutine(ResetNPC());

        isTalking = false;
        Debug.Log("Cutscene Ended. Interaction Locked.");
    }

    IEnumerator SmoothLookAt(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            float time = 0;
            while (time < 1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, time * 2 * Time.deltaTime);
                time += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator ResetNPC()
    {
        float time = 0;
        float duration = 1.5f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPos, originalPosition, time / duration);
            transform.rotation = Quaternion.Slerp(startRot, originalRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

    void CheckForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        bool currentlyInRange = false;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(playerTag))
            {
                currentlyInRange = true;
                break;
            }
        }
        playerInRange = currentlyInRange;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = interactionFinished ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}