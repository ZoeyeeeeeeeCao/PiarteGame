using UnityEngine;
using TMPro;
using System.Collections;

public class ParkourTutorialSystem : TutorialManagerBase
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip slideTransitionSound;
    public AudioClip missionStartSound;

    [Header("Teleport Settings")]
    public Transform tutorialTeleportPoint;
    public Transform returnTeleportPoint;

    [Header("Mission Settings")]
    public GameObject[] checkpointObjects;
    private int currentTargetIndex = 0;

    [Header("Tutorial Slides")]
    public GameObject tutorialUI;
    public GameObject[] tutorialSlides;
    private int currentSlideIndex = 0;
    private bool tutorialActive = false;

    [Header("Intermediate Dialogue")]
    public Dialogue afterSlidesDialogue;
    private bool waitingForPostSlideDialogue = false;

    [Header("Mission UI")]
    public GameObject missionUI;
    public TextMeshProUGUI progressText;

    [Header("Completion")]
    public Dialogue completionDialogue;
    public GameObject npcToDestroy;

    private DialogueManager dialogueManager;
    private bool missionActive = false;
    private bool waitingForCompletionDialogue = false;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (missionUI != null) missionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);

        foreach (GameObject cp in checkpointObjects)
        {
            if (cp != null) cp.SetActive(false);
        }
    }

    void Update()
    {
        // 1. Handle Slide Navigation (Works while frozen)
        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
        {
            NextTutorialSlide();
        }

        // 2. Handle Dialogue checks
        if (waitingForPostSlideDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForPostSlideDialogue = false;
        }

        if (waitingForCompletionDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForCompletionDialogue = false;
            StartCoroutine(Finish());
        }
    }

    public override void OnDialogueComplete()
    {
        StartCoroutine(StartSequence());
    }

    public override bool IsMissionActive() => missionActive;

    IEnumerator StartSequence()
    {
        // 1. Pause after first dialogue closes
        yield return new WaitForSeconds(0.5f);

        // 2. TELEPORT + ROTATION
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (tutorialTeleportPoint != null && player != null)
        {
            // Temporarily disable CharacterController for teleport safety
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = tutorialTeleportPoint.position;
            player.transform.rotation = tutorialTeleportPoint.rotation;

            if (cc != null) cc.enabled = true;
            Debug.Log("🚀 Parkour Teleport Successful");
        }

        // 3. LONGER DELAY (Ensures player is spawned before freezing)
        yield return new WaitForSeconds(1.0f);

        // 4. Show Slides (Freeze) OR Start Mission
        if (tutorialUI != null && tutorialSlides != null && tutorialSlides.Length > 0)
        {
            ShowTutorialUI();
        }
        else
        {
            StartMission();
        }
    }

    void StartMission()
    {
        missionActive = true;
        currentTargetIndex = 0;

        if (missionUI != null)
        {
            missionUI.SetActive(true);
            // Play mission HUD sound
            if (missionStartSound != null && audioSource != null)
                audioSource.PlayOneShot(missionStartSound);
        }

        if (checkpointObjects.Length > 0 && checkpointObjects[0] != null)
        {
            checkpointObjects[0].SetActive(true);
        }

        UpdateUI();
    }

    // --- SLIDE LOGIC ---
    void ShowTutorialUI()
    {
        tutorialActive = true;
        tutorialUI.SetActive(true);

        // FREEZE THE SCREEN
        Time.timeScale = 0f;

        currentSlideIndex = 0;
        ShowSlide(0);
    }

    void ShowSlide(int index)
    {
        foreach (var s in tutorialSlides) s.SetActive(false);
        if (index < tutorialSlides.Length)
        {
            tutorialSlides[index].SetActive(true);

            // Play Slide Sound
            if (slideTransitionSound != null && audioSource != null)
                audioSource.PlayOneShot(slideTransitionSound);
        }
    }

    void NextTutorialSlide()
    {
        currentSlideIndex++;
        if (currentSlideIndex < tutorialSlides.Length)
        {
            ShowSlide(currentSlideIndex);
        }
        else
        {
            // UNFREEZE
            Time.timeScale = 1f;
            tutorialActive = false;
            tutorialUI.SetActive(false);

            StartMission();

            if (afterSlidesDialogue != null && afterSlidesDialogue.dialogueLines.Length > 0)
            {
                StartCoroutine(StartMiddleDialogueWithDelay());
            }
        }
    }

    IEnumerator StartMiddleDialogueWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        dialogueManager.StartDialogue(afterSlidesDialogue);
        waitingForPostSlideDialogue = true;
    }

    // --- CHECKPOINT LOGIC ---
    public void OnCheckpointReached(int indexFromCheckpoint)
    {
        if (!missionActive) return;

        if (indexFromCheckpoint == currentTargetIndex)
        {
            currentTargetIndex++;
            UpdateUI();

            if (currentTargetIndex < checkpointObjects.Length)
            {
                if (checkpointObjects[currentTargetIndex] != null)
                    checkpointObjects[currentTargetIndex].SetActive(true);
            }
            else
            {
                CompleteMission();
            }
        }
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"Checkpoint {currentTargetIndex}/{checkpointObjects.Length}";
    }

    void CompleteMission()
    {
        missionActive = false;
        if (missionUI != null) missionUI.SetActive(false);

        if (completionDialogue.dialogueLines != null && completionDialogue.dialogueLines.Length > 0)
        {
            dialogueManager.StartDialogue(completionDialogue);
            waitingForCompletionDialogue = true;
        }
        else
        {
            StartCoroutine(Finish());
        }
    }

    IEnumerator Finish()
    {
        yield return new WaitForSeconds(0.5f);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (returnTeleportPoint != null && player != null)
            player.transform.position = returnTeleportPoint.position;

        if (npcToDestroy != null) Destroy(npcToDestroy);
    }
}