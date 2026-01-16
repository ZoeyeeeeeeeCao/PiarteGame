using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CelyneMissionManager : MonoBehaviour
{
    [System.Serializable]
    public class NPCMissionData
    {
        [Tooltip("The NPC GameObject (will be destroyed when talked to)")]
        public GameObject npcObject;
        [Tooltip("Quest ID for this specific NPC's compass marker")]
        public string compassQuestID = "";
        [HideInInspector]
        public bool hasBeenTalkedTo = false;
    }

    [Header("Mission UI Group")]
    public GameObject missionBox;
    public Image missionImage;
    public TextMeshProUGUI missionText;

    [Header("Exploration Phase")]
    public string triggerTag = "Player";
    [Tooltip("Trigger to END exploration phase (shows NPC markers)")]
    public GameObject explorationEndTrigger;

    [Header("Phase 1: Talk to NPCs")]
    [Tooltip("List of NPCs with their individual compass quest IDs")]
    public List<NPCMissionData> npcMissions;
    public string phase1Text = "Talk to the villagers (0/3)";

    [Header("Phase 2: New Objective")]
    public string phase2Text = "Investigate the strange particles.";
    public GameObject newObjectiveMarker;
    public GameObject phase2ObjectToReveal;
    public GameObject phase2ObjectToHide1;
    public GameObject phase2ObjectToHide2;
    public MonoBehaviour scriptToUnlock;

    [Header("Phase 3: Final Mission")]
    public string finalMissionText = "Follow the wine cart";

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip missionUpdateSound;

    [Header("Slide Through Settings")]
    public float slideSpeed = 500f;
    public float slideDuration = 1f;
    public Color yellowColor = Color.yellow;

    [Header("Compass Integration")]
    [Tooltip("Reference to the Compass script")]
    public Compass compass;
    [Tooltip("Quest ID for exploration area (must match ReusableOpeningMission)")]
    public string exploreAreaQuestID = "Explore_Area";
    [Tooltip("Quest ID for Phase 2 particles (Silas)")]
    public string phase2CompassQuestID = "SilasInteraction";
    [Tooltip("Quest ID for Phase 3 cart")]
    public string phase3CompassQuestID = "FollowCart";

    private int npcsTalkedTo = 0;
    private bool explorationPhaseComplete = false;
    private bool phase1Complete = false;
    private bool phase2Complete = false;
    private bool endTriggerDetected = false;
    private bool isSliding = false;

    void Start()
    {
        if (newObjectiveMarker != null) newObjectiveMarker.SetActive(false);
        if (phase2ObjectToReveal != null) phase2ObjectToReveal.SetActive(false);
        if (phase2ObjectToHide1 != null) phase2ObjectToHide1.SetActive(false);
        if (phase2ObjectToHide2 != null) phase2ObjectToHide2.SetActive(false);
        if (scriptToUnlock != null) scriptToUnlock.enabled = false;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (compass == null) compass = FindObjectOfType<Compass>();

        foreach (var npc in npcMissions)
        {
            npc.hasBeenTalkedTo = false;
        }

        SetupEndTriggerListener();
    }

    void SetupEndTriggerListener()
    {
        if (explorationEndTrigger != null)
        {
            TriggerListener endListener = explorationEndTrigger.GetComponent<TriggerListener>();
            if (endListener == null)
                endListener = explorationEndTrigger.AddComponent<TriggerListener>();

            endListener.triggerTag = triggerTag;
            endListener.onTriggerEnter = OnExplorationEnd;
        }
    }

    void OnExplorationEnd(Collider other)
    {
        if (explorationPhaseComplete || endTriggerDetected) return;
        endTriggerDetected = true;
        CompleteExplorationPhase();
    }

    void CompleteExplorationPhase()
    {
        explorationPhaseComplete = true;

        if (compass != null && !string.IsNullOrEmpty(exploreAreaQuestID))
        {
            compass.HideMarker(exploreAreaQuestID);
        }

        StartCoroutine(StartPhase1WithDelay());
    }

    IEnumerator StartPhase1WithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        StartPhase1();
    }

    void StartPhase1()
    {
        UpdateMissionUI(phase1Text.Replace("0/3", "0/" + npcMissions.Count));
        PlayMissionSound();
        ShowAllNPCMarkers();
    }

    void ShowAllNPCMarkers()
    {
        if (compass == null) return;

        foreach (var npc in npcMissions)
        {
            if (!npc.hasBeenTalkedTo && !string.IsNullOrEmpty(npc.compassQuestID))
            {
                compass.ShowMarker(npc.compassQuestID);
            }
        }
    }

    void Update()
    {
        if (!explorationPhaseComplete) return;

        if (!phase1Complete && !isSliding)
        {
            CheckNPCProgress();
        }

        if (phase1Complete && !phase2Complete)
        {
            if (newObjectiveMarker == null)
            {
                CompletePhase2();
            }
        }
    }

    void CheckNPCProgress()
    {
        int currentCount = 0;

        foreach (var npc in npcMissions)
        {
            if (npc.npcObject != null)
            {
                TwoWayInteraction interaction = npc.npcObject.GetComponent<TwoWayInteraction>();

                if (interaction != null && interaction.IsInteractionFinished() && !npc.hasBeenTalkedTo)
                {
                    npc.hasBeenTalkedTo = true;
                    currentCount++;

                    if (compass != null && !string.IsNullOrEmpty(npc.compassQuestID))
                    {
                        compass.HideMarker(npc.compassQuestID);
                    }
                }
                else if (npc.hasBeenTalkedTo)
                {
                    currentCount++;
                }
            }
            else if (npc.npcObject == null && !npc.hasBeenTalkedTo)
            {
                npc.hasBeenTalkedTo = true;
                currentCount++;

                if (compass != null && !string.IsNullOrEmpty(npc.compassQuestID))
                {
                    compass.HideMarker(npc.compassQuestID);
                }
            }
            else if (npc.hasBeenTalkedTo)
            {
                currentCount++;
            }
        }

        if (currentCount != npcsTalkedTo)
        {
            npcsTalkedTo = currentCount;
            UpdateMissionUI($"Talk to the villagers ({npcsTalkedTo}/{npcMissions.Count})");
            PlayMissionSound();

            if (npcsTalkedTo >= npcMissions.Count)
            {
                StartCoroutine(CompletePhase1WithSlide());
            }
        }
    }

    IEnumerator CompletePhase1WithSlide()
    {
        isSliding = true;
        phase1Complete = true;

        // 1. Hide markers immediately
        foreach (var npc in npcMissions)
        {
            if (compass != null && !string.IsNullOrEmpty(npc.compassQuestID))
                compass.HideMarker(npc.compassQuestID);
        }

        RectTransform missionRect = missionBox.GetComponent<RectTransform>();
        if (missionRect != null)
        {
            Vector2 originalPos = missionRect.anchoredPosition;
            // Slide left (negative X)
            Vector2 slideOutPos = originalPos - new Vector2(slideSpeed, 0);

            // --- SPEED UP: Use a faster duration for a "snappy" feel ---
            float fastDuration = 0.3f;
            float elapsed = 0f;

            // 2. SLIDE OUT FAST
            while (elapsed < fastDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fastDuration;
                // Use SmoothStep for a "zip" effect
                missionRect.anchoredPosition = Vector2.Lerp(originalPos, slideOutPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            // 3. UPDATE TEXT WHILE HIDDEN
            // The text is now "attached" to the box as it slides back in
            if (missionText != null)
            {
                missionText.color = yellowColor; // Highlight it's new
                missionText.text = phase2Text;   // Switch to Silas mission
            }

            yield return new WaitForSeconds(0.1f); // Tiny pause while invisible

            // 4. SLIDE BACK IN FAST
            elapsed = 0f;
            while (elapsed < fastDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fastDuration;
                missionRect.anchoredPosition = Vector2.Lerp(slideOutPos, originalPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            missionRect.anchoredPosition = originalPos;
        }

        // 5. Trigger Phase 2 logic (Objects revealing, etc.)
        StartPhase2();

        // Fade color back to white after a delay
        yield return new WaitForSeconds(1.5f);
        if (missionText != null) missionText.color = Color.white;

        isSliding = false;
    }

    void StartPhase2()
    {
        if (phase2ObjectToHide1 != null) phase2ObjectToHide1.SetActive(true);
        if (phase2ObjectToHide2 != null) phase2ObjectToHide2.SetActive(true);
        if (newObjectiveMarker != null) newObjectiveMarker.SetActive(true);
        if (phase2ObjectToReveal != null) phase2ObjectToReveal.SetActive(true);
        if (scriptToUnlock != null) scriptToUnlock.enabled = true;

        UpdateMissionUI(phase2Text);
        PlayMissionSound();

        if (compass != null && !string.IsNullOrEmpty(phase2CompassQuestID))
        {
            compass.ShowMarker(phase2CompassQuestID);
        }
    }

    void CompletePhase2()
    {
        phase2Complete = true;

        if (compass != null && !string.IsNullOrEmpty(phase2CompassQuestID))
        {
            compass.HideMarker(phase2CompassQuestID);
        }

        if (compass != null && !string.IsNullOrEmpty(phase3CompassQuestID))
        {
            compass.ShowMarker(phase3CompassQuestID);
        }

        UpdateMissionUI(finalMissionText);
        PlayMissionSound();
    }

    void UpdateMissionUI(string newText)
    {
        if (missionText != null)
        {
            missionText.text = newText;
        }
    }

    void PlayMissionSound()
    {
        if (audioSource != null && missionUpdateSound != null)
        {
            audioSource.PlayOneShot(missionUpdateSound);
        }
    }

    public void CompletePhase3()
    {
        if (compass != null && !string.IsNullOrEmpty(phase3CompassQuestID))
        {
            compass.HideMarker(phase3CompassQuestID);
        }

        if (missionBox != null) missionBox.SetActive(false);
        if (missionImage != null) missionImage.gameObject.SetActive(false);
    }

    public class TriggerListener : MonoBehaviour
    {
        public string triggerTag = "Player";
        public System.Action<Collider> onTriggerEnter;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(triggerTag))
            {
                onTriggerEnter?.Invoke(other);
            }
        }

    }
}