using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class OpeningTutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class MissionObjective
    {
        public GameObject npc;
        [Tooltip("The actual TextMeshPro component for this mission")]
        public TextMeshProUGUI missionText;
        public string missionDescription = "Talk to NPC";

        [Tooltip("The Quest ID that matches the compass questPoint")]
        public string compassQuestID = "";

        [HideInInspector]
        public bool isCompleted = false;
    }

    [Header("Opening Video")]
    [Tooltip("Video Player to play at scene start")]
    public VideoPlayer openingVideoPlayer;
    [Tooltip("Canvas/UI for displaying the video")]
    public GameObject videoUICanvas;
    [Tooltip("Allow skipping video with Enter key")]
    public bool allowVideoSkip = true;

    [Header("Hide UI During Video / Dialogue")]
    [Tooltip("Drag HUD roots here (mission box, compass UI, prompts, etc). DO NOT include videoUICanvas or tutorialUIPanel.")]
    public List<GameObject> uiToHide = new();

    private bool[] _uiPrevStates;
    private bool _uiHidden;


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
    public MonoBehaviour playerController;
    public MonoBehaviour locomotionController;

    [Header("Movement Detection")]
    public float movementTimeRequired = 1.5f;
    public float minMovementSpeed = 0.1f;

    [Header("Compass Integration")]
    public Compass compass;

    private bool tutorialActive = false;
    private bool missionActive = false;
    private bool tutorialStarted = false;
    private bool allMissionsCompleted = false;
    private bool videoCompleted = false;
    private bool isPlayingVideo = false;
    private float movementTimer = 0f;
    private Vector3 lastPlayerPosition;

    private void HideUIRoots()
    {
        if (_uiHidden) return;
        if (uiToHide == null || uiToHide.Count == 0) return;

        _uiPrevStates = new bool[uiToHide.Count];

        for (int i = 0; i < uiToHide.Count; i++)
        {
            var go = uiToHide[i];
            if (go == null)
            {
                _uiPrevStates[i] = false;
                continue;
            }

            _uiPrevStates[i] = go.activeSelf;
            if (go.activeSelf) go.SetActive(false);
        }

        _uiHidden = true;
    }

    private void RestoreUIRoots()
    {
        if (!_uiHidden) return;
        if (uiToHide == null || uiToHide.Count == 0) return;

        if (_uiPrevStates == null || _uiPrevStates.Length != uiToHide.Count)
        {
            // fallback: enable everything in list
            for (int i = 0; i < uiToHide.Count; i++)
                if (uiToHide[i] != null) uiToHide[i].SetActive(true);

            _uiHidden = false;
            return;
        }

        for (int i = 0; i < uiToHide.Count; i++)
        {
            var go = uiToHide[i];
            if (go == null) continue;
            go.SetActive(_uiPrevStates[i]);
        }

        _uiHidden = false;
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        dialogueManager = FindObjectOfType<DialogueManager>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (player != null)
            lastPlayerPosition = player.transform.position;

        if (compass == null)
            compass = FindObjectOfType<Compass>();

        // Find player controllers if not assigned
        if (playerController == null || locomotionController == null)
        {
            if (player != null)
            {
                MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    string scriptName = script.GetType().Name;
                    if (playerController == null && (scriptName.Contains("Player") || scriptName.Contains("Controller")))
                    {
                        playerController = script;
                    }
                    if (locomotionController == null && (scriptName.Contains("Locomotion") || scriptName.Contains("Movement")))
                    {
                        locomotionController = script;
                    }
                }
            }
        }

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

        // Setup and play opening video
        if (openingVideoPlayer != null)
        {
            if (videoUICanvas != null)
                videoUICanvas.SetActive(false);

            openingVideoPlayer.loopPointReached += OnVideoFinished;
            StartCoroutine(PlayOpeningVideo());
        }
        else
        {
            // No video - start tutorial immediately
            videoCompleted = true;
        }
    }

    IEnumerator PlayOpeningVideo()
    {
        yield return new WaitForSeconds(0.1f);
        

        isPlayingVideo = true;
        HideUIRoots();
        // Disable player controls
        DisablePlayerControls();

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show video UI
        if (videoUICanvas != null)
            videoUICanvas.SetActive(true);

        // Play video
        if (openingVideoPlayer != null)
        {
            openingVideoPlayer.Play();
            Debug.Log("Opening video playing... Press Enter to skip.");
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (!isPlayingVideo) return;

        Debug.Log("Opening video finished.");
        EndVideo();
    }

    void SkipVideo()
    {
        Debug.Log("Opening video skipped.");

        if (openingVideoPlayer != null && openingVideoPlayer.isPlaying)
        {
            openingVideoPlayer.Stop();
        }

        if (openingVideoPlayer != null)
        {
            openingVideoPlayer.loopPointReached -= OnVideoFinished;
        }

        EndVideo();
    }

    void EndVideo()
    {
        isPlayingVideo = false;
        videoCompleted = true;
        RestoreUIRoots();

        // Hide and destroy video UI
        if (videoUICanvas != null)
        {
            videoUICanvas.SetActive(false);
            Destroy(videoUICanvas);
        }

        // Destroy video player
        if (openingVideoPlayer != null)
        {
            Destroy(openingVideoPlayer.gameObject);
        }

        // Re-enable player controls
        EnablePlayerControls();

        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Video ended. Tutorial system ready.");
    }

    void Update()
    {
        // Handle video skip
        if (isPlayingVideo && allowVideoSkip && Input.GetKeyDown(KeyCode.Return))
        {
            SkipVideo();
            return;
        }

        // Wait for video to complete before starting tutorial
        if (!videoCompleted) return;

        if (!tutorialStarted && player != null)
            DetectPlayerMovement();

        if (tutorialActive && Input.GetKeyDown(KeyCode.Return))
            NextSlide();

        if (missionActive && !allMissionsCompleted)
            CheckMissionProgress();

        if (!allMissionsCompleted)
            HandleNPCMissionVisibility();
    }

    void DisablePlayerControls()
    {
        if (playerController != null)
            playerController.enabled = false;
        if (locomotionController != null)
            locomotionController.enabled = false;
    }

    void EnablePlayerControls()
    {
        if (playerController != null)
            playerController.enabled = true;
        if (locomotionController != null)
            locomotionController.enabled = true;
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

    void OnDestroy()
    {
        if (openingVideoPlayer != null)
        {
            openingVideoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
