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

    [Header("Text Animation")]
    public float typingSpeed = 0.05f;

    [Header("UI Animation")]
    public float slideSpeed = 0.5f;
    public float slideDistance = 500f;
    public bool slideFromBottom = true;

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
            if (dialogueBoxRect != null) visiblePosition = dialogueBoxRect.anchoredPosition;
            dialoguePanel.SetActive(false);
        }

        // Initially hide UI prompt
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        // Particles can be visible from far away as a marker
        if (particleEffect != null) particleEffect.SetActive(true);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);

        if (shouldStandUp && npcAnimator != null)
            npcAnimator.SetBool(standParameter, false);
    }

    void Update()
    {
        if (interactionFinished) return;

        // Trigger conversation
        if (playerInRange && !isTalking && Input.GetKeyDown(KeyCode.E))
        {
            StartConversation();
        }
        // Advance text
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

    // --- TRIGGER DETECTION ---
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

        // Force hide prompts when talking starts
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (particleEffect != null) particleEffect.SetActive(false);

        if (dialoguePanel != null && dialogueBoxRect != null)
        {
            float yOffset = slideFromBottom ? -slideDistance : slideDistance;
            hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y + yOffset);
            dialogueBoxRect.anchoredPosition = hiddenPosition;
            dialoguePanel.SetActive(true);

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(SlideUI(hiddenPosition, visiblePosition));
        }

        originalRotation = transform.rotation;
        originalPosition = transform.position;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null) StartCoroutine(SmoothLookAt(player.transform.position));

        if (shouldStandUp && npcAnimator != null) npcAnimator.SetBool(standParameter, true);

        DisplayNextLine();
    }

    // --- (Keep the rest of the logic: SlideUI, DisplayNextLine, TypeSentence, EndSequence, etc.) ---
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
        if (currentLineIndex >= conversationLines.Count) { StartCoroutine(EndSequence()); return; }
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
        if (countsTowardsMission && !hasCounted) hasCounted = true;
        if (dialoguePanel != null && dialogueBoxRect != null)
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            yield return StartCoroutine(SlideUI(visiblePosition, hiddenPosition));
            dialoguePanel.SetActive(false);
        }
        if (audioSource != null) audioSource.Stop();
        if (objectToEnable != null) objectToEnable.SetActive(true);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(true);
        yield return new WaitForSeconds(cutsceneDuration);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (shouldStandUp && npcAnimator != null) npcAnimator.SetBool(standParameter, false);
        StartCoroutine(ResetNPC());
        isTalking = false;
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
}