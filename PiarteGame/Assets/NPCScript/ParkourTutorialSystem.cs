using UnityEngine;
using TMPro;
using System.Collections;

public class ParkourTutorialSystem : TutorialManagerBase
{
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
    [Tooltip("This plays right after the player finishes reading the slides.")]
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

        if (missionUI != null) missionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);

        foreach (GameObject cp in checkpointObjects)
        {
            if (cp != null) cp.SetActive(false);
        }
    }

    void Update()
    {
        // 1. Handle Slide Navigation
        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
        {
            NextTutorialSlide();
        }

        // 2. Handle "After Slides" Dialogue
        if (waitingForPostSlideDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForPostSlideDialogue = false;
            Debug.Log("🗣️ Post-Slide dialogue finished.");
        }

        // 3. Handle Completion Dialogue
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

    public override bool IsMissionActive()
    {
        return missionActive;
    }

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // Teleport
        if (tutorialTeleportPoint != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = tutorialTeleportPoint.position;
                player.transform.rotation = tutorialTeleportPoint.rotation;
            }
        }

        StartMission();

        if (tutorialUI != null && tutorialSlides != null && tutorialSlides.Length > 0)
        {
            ShowTutorialUI();
        }
    }

    void StartMission()
    {
        missionActive = true;
        currentTargetIndex = 0;

        if (missionUI != null) missionUI.SetActive(true);

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
        currentSlideIndex = 0;
        ShowSlide(0);
    }

    void ShowSlide(int index)
    {
        foreach (var s in tutorialSlides) s.SetActive(false);
        if (index < tutorialSlides.Length) tutorialSlides[index].SetActive(true);
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
            // Slides Finished
            tutorialActive = false;
            tutorialUI.SetActive(false);

            // --- FIX IS HERE: USE COROUTINE TO DELAY START ---
            if (afterSlidesDialogue.dialogueLines != null && afterSlidesDialogue.dialogueLines.Length > 0)
            {
                StartCoroutine(StartMiddleDialogueWithDelay());
            }
        }
    }

    // NEW: Small delay ensures the "Enter" key press doesn't skip the animation
    IEnumerator StartMiddleDialogueWithDelay()
    {
        yield return new WaitForSeconds(0.1f);

        Debug.Log("🗣️ Triggering Intermediate Dialogue.");
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
        if (progressText != null) progressText.text = $"Checkpoint {currentTargetIndex}/{checkpointObjects.Length}";
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
        if (returnTeleportPoint != null) GameObject.FindGameObjectWithTag("Player").transform.position = returnTeleportPoint.position;
        if (npcToDestroy != null) Destroy(npcToDestroy);
    }
}