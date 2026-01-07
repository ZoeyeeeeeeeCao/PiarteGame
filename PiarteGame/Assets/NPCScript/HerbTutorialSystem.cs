using UnityEngine;
using TMPro;
using System.Collections;

public class HerbTutorialSystem : TutorialManagerBase
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip slideTransitionSound;
    public AudioClip missionStartSound;

    [Header("Tutorial Slides")]
    public GameObject tutorialUI;
    public GameObject[] tutorialSlides;
    private int currentSlideIndex = 0;
    private bool tutorialActive = false;

    [Header("Intermediate Dialogue (Subtitle)")]
    public Dialogue afterSlidesDialogue;
    private bool waitingForPostSlideDialogue = false;

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

    private DialogueManager dialogueManager;
    private bool missionActive = false;
    private bool missionComplete = false;
    private bool waitingForCompletionDialogue = false;

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (missionUI != null) missionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);
    }

    void Update()
    {
        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
        {
            NextTutorialSlide();
        }

        // Handle logical flags for flow control
        if (waitingForPostSlideDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForPostSlideDialogue = false;
        }

        if (waitingForCompletionDialogue && dialogueManager != null && !dialogueManager.IsDialogueActive())
        {
            waitingForCompletionDialogue = false;
            StartCoroutine(TeleportBackAndCleanup());
        }
    }

    public override void OnDialogueComplete()
    {
        StartCoroutine(StartSequence());
    }

    public override bool IsMissionActive() => missionActive;

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (tutorialTeleportPoint != null && player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = tutorialTeleportPoint.position;

            if (cc != null) cc.enabled = true;
        }

        yield return new WaitForSeconds(1.0f);

        if (tutorialUI != null && tutorialSlides != null && tutorialSlides.Length > 0)
        {
            ShowTutorialUI();
        }
        else
        {
            StartMission();
        }
    }

    void ShowTutorialUI()
    {
        tutorialActive = true;
        tutorialUI.SetActive(true);
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
            Time.timeScale = 1f;
            tutorialActive = false;
            tutorialUI.SetActive(false);

            StartMission();

            if (afterSlidesDialogue != null && afterSlidesDialogue.dialogueLines.Length > 0)
            {
                StartCoroutine(StartMiddleSubtitleWithDelay());
            }
        }
    }

    void StartMission()
    {
        missionActive = true;
        herbsCollected = 0;
        if (missionUI != null)
        {
            missionUI.SetActive(true);
            if (missionStartSound != null && audioSource != null)
                audioSource.PlayOneShot(missionStartSound);
        }
        UpdateUI();
    }

    IEnumerator StartMiddleSubtitleWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        // FIX: Start as Subtitle (Auto-advancing)
        dialogueManager.StartDialogue(afterSlidesDialogue, DialogueManager.DialogueMode.Subtitle);
        waitingForPostSlideDialogue = true;
    }

    public void OnHerbCollected()
    {
        if (!missionActive || missionComplete) return;
        herbsCollected++;
        UpdateUI();
        if (herbsCollected >= herbsToCollect) CompleteMission();
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

        if (completionDialogue != null && completionDialogue.dialogueLines.Length > 0)
        {
            // FIX: Start as Subtitle (Auto-advancing)
            dialogueManager.StartDialogue(completionDialogue, DialogueManager.DialogueMode.Subtitle);
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