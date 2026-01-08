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

    // NEW: Variables to store start transformation
    private Quaternion originalRotation;
    private Vector3 originalPosition;

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
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
            DisplayNextLine();
        }
    }

    void StartConversation()
    {
        isTalking = true;
        currentLineIndex = -1;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (particleEffect != null) particleEffect.SetActive(false);

        // --- 1. SAVE ORIGINAL POS & ROT ---
        originalRotation = transform.rotation;
        originalPosition = transform.position;

        // --- 2. FACE THE PLAYER ---
        StartCoroutine(SmoothLookAt(GameObject.FindGameObjectWithTag(playerTag).transform.position));

        if (shouldStandUp && npcAnimator != null)
        {
            npcAnimator.SetBool(standParameter, true);
        }

        DisplayNextLine();
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

        if (dialogueText != null) dialogueText.text = currentLine.text;

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

        if (audioSource != null)
        {
            audioSource.Stop();
            if (currentLine.voiceLine != null) audioSource.PlayOneShot(currentLine.voiceLine);
        }
    }

    IEnumerator EndSequence()
    {
        interactionFinished = true;

        if (countsTowardsMission && !hasCounted)
        {
            hasCounted = true;
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.AddProgressToCurrent();
            }
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
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

        // --- 3. RESET POSITION AND ROTATION ---
        StartCoroutine(ResetNPC());

        isTalking = false;
        Debug.Log("Cutscene Ended. Interaction Locked.");
    }

    // Helper to rotate to player
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

    // NEW Helper to return to original spot
    IEnumerator ResetNPC()
    {
        float time = 0;
        float duration = 1.5f; // How long it takes to sit back down properly

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (time < duration)
        {
            // Smoothly move Position back
            transform.position = Vector3.Lerp(startPos, originalPosition, time / duration);

            // Smoothly move Rotation back
            transform.rotation = Quaternion.Slerp(startRot, originalRotation, time / duration);

            time += Time.deltaTime;
            yield return null;
        }

        // Force exact finish to prevent floating point errors
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