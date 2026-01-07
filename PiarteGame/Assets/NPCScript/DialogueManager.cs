using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogueBox;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Settings")]
    public float typeSpeed = 0.05f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip defaultTypingClip; // The "blip" sound (optional)
    public bool stopAudioOnSkip = true;

    [Range(1, 5)]
    public int frequencyLevel = 2;

    private Dialogue.DialogueLine[] currentLines;
    private int lineIndex = 0;

    // --- KEEPING YOUR VARIABLES ---
    private bool isDialogueActive = false;
    private bool isTyping = false;

    void Start()
    {
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // STRICTLY ENTER KEY ONLY
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = currentLines[lineIndex].text;
                isTyping = false;

                // If player skips text, stop the voice line?
                if (stopAudioOnSkip && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;
        currentLines = dialogue.dialogueLines;
        lineIndex = 0;

        if (dialogueBox != null) dialogueBox.SetActive(true);
        if (npcNameText != null) npcNameText.text = dialogue.npcName;

        StartCoroutine(TypeLine());
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

    IEnumerator TypeLine()
    {
        isTyping = true;

        // 1. Get the data for the current line
        Dialogue.DialogueLine currentLineData = currentLines[lineIndex];

        dialogueText.text = "";
        string lineToType = currentLineData.text;

        // 2. CHECK FOR SPECIFIC AUDIO CLIP (Like 'herbdone')
        // This assumes your struct variable is named 'audioClip'
        bool hasSpecificClip = currentLineData.audioClip != null;

        if (hasSpecificClip && audioSource != null)
        {
            audioSource.pitch = 1f; // Normal pitch for voice acting
            audioSource.PlayOneShot(currentLineData.audioClip);
        }

        int charCount = 0;

        foreach (char c in lineToType.ToCharArray())
        {
            dialogueText.text += c;
            charCount++;

            // 3. PLAY TYPING BLIPS (Only if there is NO specific voice clip)
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
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    // --- KEEPING YOUR PUBLIC METHOD ---
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}