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
    public GameObject spacePromptText; // Added reference for the space text

    [Header("Settings")]
    public float typeSpeed = 0.05f;
    public float subtitleAutoDelay = 1.0f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip defaultTypingClip;
    public bool stopAudioOnSkip = true;

    [Range(1, 5)]
    public int frequencyLevel = 2;

    private Dialogue.DialogueLine[] currentLines;
    private int lineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;

    void Start()
    {
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (spacePromptText != null) spacePromptText.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isDialogueActive && currentMode == DialogueMode.Dialogue && Input.GetKeyDown(KeyCode.Return))
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

    public void StartDialogue(Dialogue dialogue, DialogueMode mode = DialogueMode.Dialogue)
    {
        currentMode = mode;
        isDialogueActive = true;
        currentLines = dialogue.dialogueLines;
        lineIndex = 0;

        if (dialogueBox != null) dialogueBox.SetActive(true);
        if (npcNameText != null) npcNameText.text = dialogue.npcName;

        // --- UI LOGIC FOR SPACE TEXT ---
        if (spacePromptText != null)
        {
            // Only show the Space prompt if it is a Normal Dialogue
            spacePromptText.SetActive(currentMode == DialogueMode.Dialogue);
        }

        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        Dialogue.DialogueLine currentLineData = currentLines[lineIndex];
        dialogueText.text = "";
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
        StopAllCoroutines();
        dialogueText.text = currentLines[lineIndex].text;
        isTyping = false;

        if (stopAudioOnSkip && audioSource.isPlaying)
        {
            audioSource.Stop();
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
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (spacePromptText != null) spacePromptText.SetActive(false);
    }

    public bool IsDialogueActive() => isDialogueActive;
}