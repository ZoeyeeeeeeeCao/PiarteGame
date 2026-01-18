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
        [TextArea(3, 10)]
        public string text;
        public AudioClip voiceLine;
    }

    // --- SETTINGS ---
    [Header("Conversation Data")]
    public string npcName = "Villager";
    public string playerName = "Me";
    public List<ConversationLine> conversationLines;

    [Header("Text Animation")]
    public float typingSpeed = 0.05f;

    [Header("UI Animation")]
    public float slideSpeed = 0.5f;
    public float slideDistance = 500f;
    public bool slideFromBottom = true;

    [Header("Custom Position Settings")]
    [Tooltip("Check this to ignore where the UI is in the Editor and force it to a specific position when talking.")]
    public bool useCustomPosition = false;
    [Tooltip("The anchored position the UI should appear at (e.g., X=0, Y=0)")]
    public Vector2 customVisiblePos = Vector2.zero;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    [Tooltip("The 'Press E' UI Image/Object")]
    public GameObject interactionPrompt;

    [Header("Detection Settings")]
    public string playerTag = "Player";
    public GameObject particleEffect;

    [Header("Mission & Cutscene")]
    public bool countsTowardsMission = false;
    private bool hasCounted = false;
    public GameObject cutsceneCamera;
    public GameObject objectToEnable;
    public float cutsceneDuration = 2.0f;

    [Header("NPC Animation")]
    public bool shouldStandUp = false;
    public Animator npcAnimator;
    public string standParameter = "isStanding";

    [Header("Compass Integration")]
    public Compass compass;
    public string compassQuestID = "";
    public bool hideMarkerOnComplete = true;

    // --- PRIVATE ---
    private bool playerInRange = false;
    private bool isTalking = false;
    private bool interactionFinished = false;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private string currentFullSentence = "";
    private Coroutine typingCoroutine;
    private Coroutine animationCoroutine;
    private RectTransform dialogueBoxRect;
    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;
    private Quaternion originalRotation;
    private Vector3 originalPosition;

    void Start()
    {
        if (dialoguePanel != null)
        {
            dialogueBoxRect = dialoguePanel.GetComponent<RectTransform>();
            if (dialogueBoxRect != null)
            {
                // --- FIX: Logic to handle Position Issues ---
                if (useCustomPosition)
                {
                    // Use the user-defined position
                    visiblePosition = customVisiblePos;
                }
                else
                {
                    // Use whatever position the UI is currently sitting at in the Editor
                    visiblePosition = dialogueBoxRect.anchoredPosition;
                }
            }
            dialoguePanel.SetActive(false);
        }

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (particleEffect != null) particleEffect.SetActive(true);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (shouldStandUp && npcAnimator != null)
            npcAnimator.SetBool(standParameter, false);

        if (compass == null)
            compass = FindObjectOfType<Compass>();
    }

    void Update()
    {
        if (interactionFinished) return;

        if (playerInRange && !isTalking && Input.GetKeyDown(KeyCode.E))
        {
            StartConversation();
        }
        else if (isTalking && Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !interactionFinished && !isTalking)
        {
            playerInRange = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }

    void StartConversation()
    {
        isTalking = true;
        currentLineIndex = -1;

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (particleEffect != null) particleEffect.SetActive(false);

        if (dialoguePanel != null && dialogueBoxRect != null)
        {
            float yOffset = slideFromBottom ? -slideDistance : slideDistance;

            // Calculate hidden position relative to the correct Visible position
            hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y + yOffset);

            // Snap to hidden immediately
            dialogueBoxRect.anchoredPosition = hiddenPosition;
            dialoguePanel.SetActive(true);

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            // Slide TO the Visible Position
            animationCoroutine = StartCoroutine(SlideUI(hiddenPosition, visiblePosition));
        }

        originalRotation = transform.rotation;
        originalPosition = transform.position;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null) StartCoroutine(SmoothLookAt(player.transform.position));

        if (shouldStandUp && npcAnimator != null)
            npcAnimator.SetBool(standParameter, true);

        DisplayNextLine();
    }

    IEnumerator SlideUI(Vector2 startPos, Vector2 endPos)
    {
        float elapsed = 0f;
        while (elapsed < slideSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / slideSpeed);
            dialogueBoxRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
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
            nameText.text = (currentLine.speaker == Speaker.NPC) ? npcName : playerName;
            nameText.color = Color.white;
        }

        if (dialogueText != null)
        {
            dialogueText.color = Color.white;
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(currentLine.text));
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            if (currentLine.voiceLine != null)
                audioSource.PlayOneShot(currentLine.voiceLine);
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
            hasCounted = true;

        if (hideMarkerOnComplete && compass != null && !string.IsNullOrEmpty(compassQuestID))
        {
            compass.HideMarker(compassQuestID);
        }

        if (dialoguePanel != null && dialogueBoxRect != null)
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            // Slide Back to Hidden
            yield return StartCoroutine(SlideUI(visiblePosition, hiddenPosition));
            dialoguePanel.SetActive(false);
        }

        if (audioSource != null) audioSource.Stop();
        if (objectToEnable != null) objectToEnable.SetActive(true);

        if (cutsceneCamera != null) cutsceneCamera.SetActive(true);
        yield return new WaitForSeconds(cutsceneDuration);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);

        if (shouldStandUp && npcAnimator != null)
            npcAnimator.SetBool(standParameter, false);

        StartCoroutine(ResetNPC());
        isTalking = false;

        if (countsTowardsMission)
        {
            yield return new WaitForSeconds(2f);
            Destroy(gameObject);
        }
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

    public bool IsInteractionFinished()
    {
        return interactionFinished;
    }

    public bool CountsTowardsMission()
    {
        return countsTowardsMission && hasCounted;
    }
}