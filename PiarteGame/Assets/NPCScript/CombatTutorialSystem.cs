using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatTutorialSystem : TutorialManagerBase
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip slideTransitionSound;
    public AudioClip missionStartSound;

    [Header("Target Enemies (Drag Here!)")]
    public List<EnemyTutorialTarget> targetEnemies;

    [Header("Tutorial Slides")]
    public GameObject tutorialUI;
    public GameObject[] tutorialSlides;
    private int currentSlideIndex = 0;
    private bool tutorialActive = false;

    [Header("Intermediate Dialogue")]
    public Dialogue afterSlidesDialogue;
    private bool waitingForPostSlideDialogue = false;

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
    private bool waitingForCompletionDialogue = false;

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (missionUI != null) missionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);

        if (targetEnemies != null && targetEnemies.Count > 0)
        {
            enemiesToDefeat = targetEnemies.Count;
        }
    }

    void Update()
    {
        // 1. Handle Slide Navigation (Works while Time.timeScale is 0)
        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
        {
            NextTutorialSlide();
        }

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

    public override void OnDialogueComplete() { StartCoroutine(StartSequence()); }
    public override bool IsMissionActive() { return missionActive; }

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // 1. TELEPORT FIRST
        if (combatArenaPoint != null && player != null)
        {
            // Disable CharacterController temporarily for teleport safety
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = combatArenaPoint.position;
            player.transform.rotation = combatArenaPoint.rotation;

            if (cc != null) cc.enabled = true;
            Debug.Log("🚀 Player Teleported to Arena");
        }

        // 2. LONGER DELAY (Ensures world settles before freeze)
        yield return new WaitForSeconds(1.0f);

        // 3. Show Slides (Freeze) OR Start Mission
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
        kills = 0;
        if (missionUI != null)
        {
            missionUI.SetActive(true);
            // Play HUD appearance sound
            if (missionStartSound != null && audioSource != null)
                audioSource.PlayOneShot(missionStartSound);
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

    public void OnEnemyKilled(EnemyTutorialTarget enemyDied)
    {
        if (!missionActive) return;

        if (targetEnemies.Contains(enemyDied))
        {
            kills++;
            targetEnemies.Remove(enemyDied);
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

        if (completionDialogue != null && completionDialogue.dialogueLines.Length > 0)
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