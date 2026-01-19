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
    public bool useCustomPosition = false;
    public Vector2 customVisiblePos = Vector2.zero;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
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

    // ✅ NEW: Hide UI During Dialogue (hooks into your mission manager)
    [Header("Hide UI During Dialogue")]
    [Tooltip("Optional. If left empty, it will auto-find one in the scene.")]
    public CelyneMissionManager missionManager;

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
                if (useCustomPosition) visiblePosition = customVisiblePos;
                else visiblePosition = dialogueBoxRect.anchoredPosition;
            }
            dialoguePanel.SetActive(false);
        }

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (particleEffect != null) particleEffect.SetActive(true);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (shouldStandUp && npcAnimator != null) npcAnimator.SetBool(standParameter, false);

        if (compass == null) compass = FindObjectOfType<Compass>();

        // ✅ auto-find mission manager if not assigned
        if (missionManager == null) missionManager = FindObjectOfType<CelyneMissionManager>();
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

        // ✅ HIDE OTHER UI WHILE TALKING
        if (missionManager != null)
            missionManager.EnterDialogueMode();

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
            compass.HideMarker(compassQuestID);

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

        if (shouldStandUp && npcAnimator != null)
            npcAnimator.SetBool(standParameter, false);

        StartCoroutine(ResetNPC());
        isTalking = false;

        // ✅ SHOW UI BACK AFTER DIALOGUE IS FULLY DONE (and cutscene is done)
        if (missionManager != null)
            missionManager.ExitDialogueMode();

        if (countsTowardsMission)
        {
            yield return new WaitForSeconds(2f);
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

    public bool IsInteractionFinished() => interactionFinished;

    public bool CountsTowardsMission() => countsTowardsMission && hasCounted;
}
