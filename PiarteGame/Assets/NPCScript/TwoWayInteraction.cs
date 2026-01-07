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

    // NEW FLAG: Tracks if we have already finished this specific interaction
    private bool interactionFinished = false;

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
        // Don't even check for player if we are already done forever
        if (interactionFinished) return;

        CheckForPlayer();

        // CHANGED: Added "!interactionFinished" to the check
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

        // Hide the particle (and it will stay hidden forever now)
        if (particleEffect != null) particleEffect.SetActive(false);

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
        // --- NEW: LOCK THE INTERACTION ---
        interactionFinished = true; // This ensures it can never run again
        // ---------------------------------

        if (countsTowardsMission && !hasCounted)
        {
            hasCounted = true;
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.AddProgress();
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

        isTalking = false;
        Debug.Log("Cutscene Ended. Interaction Locked.");
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
        // If finished, draw it Red to show it's disabled
        Gizmos.color = interactionFinished ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}