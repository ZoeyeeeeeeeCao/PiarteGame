using UnityEngine;
using TMPro;
using System.Collections;

public class HerbTutorialSystem : TutorialManagerBase
{
    [Header("Tutorial Slides (NEW)")]
    public GameObject tutorialUI;
    [Tooltip("Drag your slide images here. You can add more than 1!")]
    public GameObject[] tutorialSlides;
    private int currentSlideIndex = 0;
    private bool tutorialActive = false;

    // --- NEW: INTERMEDIATE DIALOGUE ---
    [Header("Intermediate Dialogue")]
    [Tooltip("This plays right after the player finishes reading the slides.")]
    public Dialogue afterSlidesDialogue;
    private bool waitingForPostSlideDialogue = false;
    // ----------------------------------

    [Header("Teleport Points")]
    public Transform tutorialTeleportPoint;
    public Transform returnTeleportPoint;

    [Header("Mission UI")]
    public GameObject missionUI;
    public TextMeshProUGUI missionTitleText;
    public TextMeshProUGUI missionProgressText;

    [Header("Mission Settings")]
    public string missionTitle = "Gather Herbs";
    public int herbsToCollect = 5;
    private int herbsCollected = 0;

    [Header("References")]
    public GameObject player;
    public GameObject npcToDestroy;
    public Dialogue completionDialogue;

    // Internal State
    private DialogueManager dialogueManager;
    private bool missionActive = false;
    private bool missionComplete = false;
    private bool waitingForCompletionDialogue = false; // Renamed for clarity

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        dialogueManager = FindObjectOfType<DialogueManager>();

        // Hide UIs at start
        if (missionUI != null) missionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);
    }

    void Update()
    {
        // 1. Handle Slide Navigation (Enter Key)
        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
        {
            NextTutorialSlide();
        }

        // 2. Handle "After Slides" Dialogue (The Middle One)
        if (waitingForPostSlideDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForPostSlideDialogue = false;
            Debug.Log("🗣️ Post-Slide dialogue finished. Player is free to gather herbs.");
        }

        // 3. Handle Completion Logic (The End One)
        if (waitingForCompletionDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForCompletionDialogue = false;
            StartCoroutine(TeleportBackAndCleanup());
        }
    }

    // --- BASE CLASS METHODS ---
    public override void OnDialogueComplete()
    {
        StartCoroutine(StartSequence());
    }

    public override bool IsMissionActive()
    {
        return missionActive;
    }
    // --------------------------

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // 1. Teleport
        if (tutorialTeleportPoint != null)
            player.transform.position = tutorialTeleportPoint.position;

        // 2. Start Mission Logic
        StartMission();

        // 3. Show Slides (if any)
        if (tutorialUI != null && tutorialSlides != null && tutorialSlides.Length > 0)
        {
            ShowTutorialUI();
        }
    }

    void StartMission()
    {
        missionActive = true;
        herbsCollected = 0;

        if (missionUI != null) missionUI.SetActive(true);
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
        // Hide all slides first
        foreach (var s in tutorialSlides) s.SetActive(false);

        // Show the correct one
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
            // All slides finished
            tutorialActive = false;
            tutorialUI.SetActive(false);

            // --- TRIGGER INTERMEDIATE DIALOGUE ---
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

        Debug.Log("🗣️ Triggering Intermediate Herb Dialogue.");
        dialogueManager.StartDialogue(afterSlidesDialogue);
        waitingForPostSlideDialogue = true;
    }
    // -------------------

    public void OnHerbCollected()
    {
        if (!missionActive || missionComplete) return;

        herbsCollected++;
        UpdateUI();

        if (herbsCollected >= herbsToCollect)
        {
            CompleteMission();
        }
    }

    void UpdateUI()
    {
        if (missionTitleText != null) missionTitleText.text = missionTitle;

        if (missionProgressText != null)
        {
            missionProgressText.text = $"{herbsCollected}/{herbsToCollect}";

            if (herbsCollected >= herbsToCollect)
                missionProgressText.color = Color.yellow;

            Canvas.ForceUpdateCanvases();
        }
    }

    void CompleteMission()
    {
        missionComplete = true;
        missionActive = false;

        if (missionUI != null) missionUI.SetActive(false);

        // --- TRIGGER COMPLETION DIALOGUE ---
        if (completionDialogue.dialogueLines != null && completionDialogue.dialogueLines.Length > 0)
        {
            dialogueManager.StartDialogue(completionDialogue);
            waitingForCompletionDialogue = true;
        }
        else
        {
            StartCoroutine(TeleportBackAndCleanup());
        }
    }

    IEnumerator TeleportBackAndCleanup()
    {
        yield return new WaitForSeconds(0.5f);

        if (returnTeleportPoint != null)
            player.transform.position = returnTeleportPoint.position;

        if (npcToDestroy != null)
            Destroy(npcToDestroy);
    }
}