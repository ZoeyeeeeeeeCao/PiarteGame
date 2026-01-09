using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class OpeningTutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class MissionObjective
    {
        public GameObject npc;
        [Tooltip("The actual TextMeshPro component for this mission")]
        public TextMeshProUGUI missionText;
        public string missionDescription = "Talk to NPC";

        // NEW: Add Quest ID for compass
        [Tooltip("The Quest ID that matches the compass questPoint")]
        public string compassQuestID = "";

        [HideInInspector]
        public bool isCompleted = false;
    }

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip tutorialSlideAudio;
    [Tooltip("Sound played when missions first appear on HUD")]
    public AudioClip missionStartAudio;
    [Tooltip("Sound played when a specific NPC mission is finished")]
    public AudioClip missionCompleteAudio;

    [Header("Opening Tutorial System")]
    public GameObject tutorialUIPanel;
    public GameObject[] tutorialTexts;
    private int currentSlideIndex = 0;

    [Header("Generic Mission System")]
    public GameObject[] missionTexts;

    [Header("NPC Mission System")]
    [Tooltip("The main background panel/box for the NPC missions")]
    public GameObject missionBoxUI;
    public MissionObjective[] missions;
    public Color completedColor = new Color(1f, 0.84f, 0f); // Yellow

    [Header("Welcome Dialogue")]
    public Dialogue welcomeDialogue;
    private DialogueManager dialogueManager;

    [Header("Player Control")]
    public GameObject player;

    [Header("Movement Detection")]
    public float movementTimeRequired = 1.5f;
    public float minMovementSpeed = 0.1f;

    // NEW: Add reference to compass
    [Header("Compass Integration")]
    public Compass compass;

    private bool tutorialActive = false;
    private bool missionActive = false;
    private bool tutorialStarted = false;
    private bool allMissionsCompleted = false;
    private float movementTimer = 0f;
    private Vector3 lastPlayerPosition;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        dialogueManager = FindObjectOfType<DialogueManager>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (player != null)
            lastPlayerPosition = player.transform.position;

        // NEW: Find compass if not assigned
        if (compass == null)
            compass = FindObjectOfType<Compass>();

        // Hide Mission Box and NPC mission HUD elements initially
        if (missionBoxUI != null) missionBoxUI.SetActive(false);

        if (missions != null)
        {
            foreach (var m in missions)
            {
                m.isCompleted = false;
                if (m.missionText != null)
                    m.missionText.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (!tutorialStarted && player != null)
            DetectPlayerMovement();

        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
            NextSlide();

        if (missionActive && !allMissionsCompleted)
            CheckMissionProgress();

        if (!allMissionsCompleted)
            HandleNPCMissionVisibility();
    }

    void HandleNPCMissionVisibility()
    {
        bool anyGenericMissionActive = false;
        if (missionTexts != null)
        {
            foreach (GameObject txt in missionTexts)
            {
                if (txt != null && txt.activeInHierarchy)
                {
                    anyGenericMissionActive = true;
                    break;
                }
            }
        }

        if (missions != null && missionActive)
        {
            bool showOverallUI = !anyGenericMissionActive;

            if (missionBoxUI != null && missionBoxUI.activeSelf != showOverallUI)
                missionBoxUI.SetActive(showOverallUI);

            foreach (var mission in missions)
            {
                if (mission.missionText != null)
                {
                    if (mission.missionText.gameObject.activeSelf != showOverallUI)
                    {
                        mission.missionText.gameObject.SetActive(showOverallUI);
                    }
                }
            }

            // NEW: Hide compass markers when generic mission is active
            if (compass != null && anyGenericMissionActive)
            {
                foreach (var mission in missions)
                {
                    if (!string.IsNullOrEmpty(mission.compassQuestID))
                    {
                        compass.HideMarker(mission.compassQuestID);
                    }
                }
            }
            else if (compass != null && !anyGenericMissionActive)
            {
                // Show markers again when generic mission ends
                foreach (var mission in missions)
                {
                    if (!mission.isCompleted && !string.IsNullOrEmpty(mission.compassQuestID))
                    {
                        compass.ShowMarker(mission.compassQuestID);
                    }
                }
            }
        }
    }

    void DetectPlayerMovement()
    {
        Vector3 currentPosition = player.transform.position;
        float distanceMoved = Vector3.Distance(currentPosition, lastPlayerPosition);

        if (distanceMoved > (minMovementSpeed * Time.deltaTime))
        {
            movementTimer += Time.deltaTime;
            if (movementTimer >= movementTimeRequired)
            {
                tutorialStarted = true;
                StartCoroutine(StartOpeningTutorial());
            }
        }
        else
        {
            movementTimer = 0f;
        }
        lastPlayerPosition = currentPosition;
    }

    IEnumerator StartOpeningTutorial()
    {
        yield return new WaitForSeconds(0.2f);
        Time.timeScale = 0f;

        if (tutorialUIPanel != null)
            tutorialUIPanel.SetActive(true);

        tutorialActive = true;
        currentSlideIndex = 0;
        ShowSlide(currentSlideIndex);
    }

    void ShowSlide(int index)
    {
        foreach (GameObject t in tutorialTexts)
            if (t != null) t.SetActive(false);

        if (index >= 0 && index < tutorialTexts.Length && tutorialTexts[index] != null)
        {
            tutorialTexts[index].SetActive(true);
            if (tutorialSlideAudio != null && audioSource != null)
                audioSource.PlayOneShot(tutorialSlideAudio);
        }
    }

    void NextSlide()
    {
        currentSlideIndex++;
        if (currentSlideIndex < tutorialTexts.Length)
            ShowSlide(currentSlideIndex);
        else
            OnTutorialSlidesComplete();
    }

    void OnTutorialSlidesComplete()
    {
        tutorialActive = false;
        if (tutorialUIPanel != null)
            tutorialUIPanel.SetActive(false);

        Time.timeScale = 1f;

        StartMissionHUD();

        StartCoroutine(ShowDialogueWithDelay());
    }

    IEnumerator ShowDialogueWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        if (welcomeDialogue != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(welcomeDialogue, DialogueManager.DialogueMode.Subtitle);
        }
    }

    void StartMissionHUD()
    {
        missionActive = true;

        if (missionBoxUI != null) missionBoxUI.SetActive(true);

        if (missionStartAudio != null && audioSource != null)
            audioSource.PlayOneShot(missionStartAudio);

        if (missions != null)
        {
            foreach (var m in missions)
            {
                if (m.missionText != null)
                {
                    m.missionText.gameObject.SetActive(true);
                    m.missionText.text = m.missionDescription;
                    m.missionText.color = Color.white;
                }

                // NEW: Show compass marker when mission starts
                if (compass != null && !string.IsNullOrEmpty(m.compassQuestID))
                {
                    compass.ShowMarker(m.compassQuestID);
                }
            }
        }
    }

    void CheckMissionProgress()
    {
        if (missions == null) return;

        bool allComplete = true;

        foreach (var mission in missions)
        {
            if (!mission.isCompleted && mission.npc == null)
            {
                mission.isCompleted = true;

                if (mission.missionText != null)
                {
                    mission.missionText.color = completedColor;
                    mission.missionText.text = "<s>" + mission.missionDescription + "</s>";

                    if (missionCompleteAudio != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(missionCompleteAudio);
                    }
                }

                // NEW: Hide compass marker when mission completes
                if (compass != null && !string.IsNullOrEmpty(mission.compassQuestID))
                {
                    compass.HideMarker(mission.compassQuestID);
                }
            }

            if (!mission.isCompleted)
            {
                allComplete = false;
            }
        }

        if (allComplete && !allMissionsCompleted)
        {
            allMissionsCompleted = true;
            StartCoroutine(HideAllMissionTextsAndBox());
        }
    }

    IEnumerator HideAllMissionTextsAndBox()
    {
        yield return new WaitForSeconds(2f);

        if (missionBoxUI != null) missionBoxUI.SetActive(false);

        if (missions != null)
        {
            foreach (var mission in missions)
            {
                if (mission.missionText != null)
                {
                    mission.missionText.gameObject.SetActive(false);
                }
            }
        }
    }
}