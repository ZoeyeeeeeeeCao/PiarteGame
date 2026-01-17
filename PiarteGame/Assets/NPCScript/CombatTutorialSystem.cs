using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatTutorialSystem : TutorialManagerBase
{
    [Header("Target Enemies")]
    [Tooltip("Drag the enemies ALREADY in your scene here.")]
    public List<GameObject> targetEnemies;

    [Header("Teleport Points")]
    public Transform combatArenaPoint;
    public Transform returnPoint;

    [Header("UI & Audio")]
    public GameObject missionUI;
    public TextMeshProUGUI progressText;
    public AudioSource audioSource;
    public AudioClip slideTransitionSound;
    public AudioClip missionStartSound;

    [Header("Tutorial Slides")]
    public GameObject tutorialUI;
    public GameObject[] tutorialSlides;
    private int currentSlideIndex = 0;
    private bool tutorialActive = false;

    [Header("References")]
    public GameObject player;
    public GameObject npcToDestroy;
    public Dialogue completionDialogue;
    public Dialogue afterSlidesDialogue;
    public Compass compass;
    public string compassQuestID = "CombatTutorial";

    private int kills = 0;
    private DialogueManager dialogueManager;
    private bool missionActive = false;
    private bool waitingForCompletionDialogue = false;

    void Start()
    {
        // Automatically finds player by tag so you don't have to drag it
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (compass == null) compass = Object.FindAnyObjectByType<Compass>();

        if (missionUI != null) missionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);
    }

    void Update()
    {
        // Advance slides with Enter key
        if (tutorialActive && Input.GetKeyDown(KeyCode.Return)) NextTutorialSlide();

        // Check if completion dialogue ended
        if (waitingForCompletionDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForCompletionDialogue = false;
            StartCoroutine(Finish());
        }

        // Track kills
        if (missionActive) CheckEnemyProgress();
    }

    void CheckEnemyProgress()
    {
        int currentCount = 0;
        foreach (GameObject enemy in targetEnemies)
        {
            // Count as kill if object is destroyed or hidden
            if (enemy == null || !enemy.activeInHierarchy) currentCount++;
        }

        if (currentCount != kills)
        {
            kills = currentCount;
            UpdateUI();

            if (kills >= targetEnemies.Count && targetEnemies.Count > 0)
            {
                CompleteMission();
            }
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

        // Teleport Player
        if (combatArenaPoint != null && player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = combatArenaPoint.position;
            player.transform.rotation = combatArenaPoint.rotation;
            if (cc != null) cc.enabled = true;
        }

        yield return new WaitForSeconds(1.0f);

        if (tutorialUI != null && tutorialSlides.Length > 0)
            ShowTutorialUI();
        else
            StartMission();
    }

    void StartMission()
    {
        missionActive = true;
        kills = 0;
        if (missionUI != null) missionUI.SetActive(true);
        if (compass != null) compass.ShowMarker(compassQuestID);
        if (missionStartSound && audioSource) audioSource.PlayOneShot(missionStartSound);
        UpdateUI();
    }

    void ShowTutorialUI()
    {
        tutorialActive = true;
        tutorialUI.SetActive(true);
        Time.timeScale = 0f;
        ShowSlide(0);
    }

    void ShowSlide(int index)
    {
        for (int i = 0; i < tutorialSlides.Length; i++)
            tutorialSlides[i].SetActive(i == index);

        if (slideTransitionSound && audioSource)
            audioSource.PlayOneShot(slideTransitionSound);
    }

    void NextTutorialSlide()
    {
        currentSlideIndex++;
        if (currentSlideIndex < tutorialSlides.Length) ShowSlide(currentSlideIndex);
        else
        {
            Time.timeScale = 1f;
            tutorialActive = false;
            tutorialUI.SetActive(false);
            StartMission();

            if (afterSlidesDialogue != null && dialogueManager != null)
                dialogueManager.StartDialogue(afterSlidesDialogue, DialogueManager.DialogueMode.Subtitle);
        }
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"Defeat the crews ({kills}/{targetEnemies.Count})";
    }

    void CompleteMission()
    {
        missionActive = false;
        if (missionUI != null) missionUI.SetActive(false);
        if (compass != null) compass.HideMarker(compassQuestID);

        if (completionDialogue != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(completionDialogue, DialogueManager.DialogueMode.Subtitle);
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