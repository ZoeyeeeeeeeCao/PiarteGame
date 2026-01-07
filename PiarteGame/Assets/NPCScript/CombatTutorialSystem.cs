using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatTutorialSystem : TutorialManagerBase
{
    [Header("Target Enemies (Drag Here!)")]
    [Tooltip("Drag the specific enemies the player must kill.")]
    public List<EnemyTutorialTarget> targetEnemies;

    [Header("Tutorial Slides (NEW)")]
    public GameObject tutorialUI;
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
    public Transform combatArenaPoint;
    public Transform returnPoint;

    [Header("UI")]
    public GameObject missionUI;
    public TextMeshProUGUI progressText;

    [Header("References")]
    public GameObject player;
    public GameObject npcToDestroy;
    public Dialogue completionDialogue;

    // Internal State
    private int enemiesToDefeat;
    private int kills = 0;
    private DialogueManager dialogueManager;
    private bool missionActive = false;
    private bool waitingForCompletionDialogue = false; // Renamed for clarity

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        dialogueManager = FindObjectOfType<DialogueManager>();

        // Hide UIs
        if (missionUI != null) missionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);

        // Auto-calculate target count based on the list size
        if (targetEnemies != null && targetEnemies.Count > 0)
        {
            enemiesToDefeat = targetEnemies.Count;
        }
        else
        {
            Debug.LogWarning("⚠️ Combat System: No enemies dragged into 'Target Enemies' list!");
        }
    }

    void Update()
    {
        // 1. Handle Slide Navigation
        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
        {
            NextTutorialSlide();
        }

        // 2. Handle "After Slides" Dialogue (The Middle One)
        if (waitingForPostSlideDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForPostSlideDialogue = false;
            Debug.Log("🗣️ Post-Slide dialogue finished. Combat Start!");
        }

        // 3. Handle Completion Dialogue (The End One)
        if (waitingForCompletionDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForCompletionDialogue = false;
            StartCoroutine(Finish());
        }
    }

    public override void OnDialogueComplete() { StartCoroutine(StartSequence()); }
    public override bool IsMissionActive() { return missionActive; }

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // Teleport
        if (combatArenaPoint != null) player.transform.position = combatArenaPoint.position;

        // Start Mission
        StartMission();

        // Show Slides
        if (tutorialUI != null && tutorialSlides != null && tutorialSlides.Length > 0)
        {
            ShowTutorialUI();
        }
    }

    void StartMission()
    {
        missionActive = true;
        kills = 0;
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

        Debug.Log("🗣️ Triggering Intermediate Combat Dialogue.");
        dialogueManager.StartDialogue(afterSlidesDialogue);
        waitingForPostSlideDialogue = true;
    }
    // -------------------

    public void OnEnemyKilled(EnemyTutorialTarget enemyDied)
    {
        if (!missionActive) return;

        // Check if this enemy is in our "To Kill" list
        if (targetEnemies.Contains(enemyDied))
        {
            kills++;
            targetEnemies.Remove(enemyDied); // Remove so it can't be counted twice

            UpdateUI();

            if (kills >= enemiesToDefeat) CompleteMission();
        }
    }

    void UpdateUI()
    {
        if (progressText != null) progressText.text = $"Defeated: {kills}/{enemiesToDefeat}";
    }

    void CompleteMission()
    {
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
            StartCoroutine(Finish());
        }
    }

    IEnumerator Finish()
    {
        yield return new WaitForSeconds(0.5f);
        if (returnPoint != null) player.transform.position = returnPoint.position;
        if (npcToDestroy != null) Destroy(npcToDestroy);
    }
}