using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CelyneMissionManager : MonoBehaviour
{
    [System.Serializable]
    public class NPCMissionData
    {
        public GameObject npcObject;
        public string compassQuestID = "";
        [HideInInspector] public bool hasBeenTalkedTo = false;
    }

    [Header("Mission UI Group")]
    public TextMeshProUGUI missionText;

    [Header("Exploration Phase")]
    public string triggerTag = "Player";
    public GameObject explorationEndTrigger;
    public string exploreAreaQuestID = "Explore_Area";

    [Header("Phase 1: Talk to NPCs")]
    public List<NPCMissionData> npcMissions;
    private int npcsTalkedTo = 0;

    [Header("Phase 2: Investigate Silas")]
    public string phase2Text = "Investigate the strange particles.";
    public GameObject phase2ObjectToReveal;
    public GameObject silasParticleSystem;
    public string phase2CompassQuestID = "SilasInteraction";

    [Header("Phase 3: Follow Cart")]
    public string phase3Text = "Follow the wine cart";
    public GameObject wineCartTrigger;
    public string phase3CompassQuestID = "FollowCart";

    // Note: Phase 3.5 (Ambush Location) logic removed to streamline flow

    [Header("Phase 4: Kill Enemies")]
    public int enemiesToKill = 10;
    private int enemiesKilled = 0;
    private bool phase4Active = false;
    private bool phase4Complete = false;

    [Header("Phase 5: Enter Interior")]
    public string phase5Text = "Enter the Main Hall";
    public GameObject interiorEntranceTrigger; // Drag your door trigger here
    public string phase5CompassQuestID = "EnterInterior";
    private bool phase5Complete = false;

    [Header("Audio & Compass")]
    public AudioSource audioSource;
    public AudioClip missionUpdateSound;
    public Compass compass;

    private bool explorationComplete = false;
    private bool phase1Complete = false;
    private bool phase2Complete = false;
    private bool phase3Complete = false;

    private void OnEnable() { EnemyHealthController.OnEnemyCountUpdated += UpdateKillMissionUI; }
    private void OnDisable() { EnemyHealthController.OnEnemyCountUpdated -= UpdateKillMissionUI; }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (compass == null) compass = FindObjectOfType<Compass>();

        UpdateMissionUI("Explore the area");

        // 1. SILAS SETUP: Keep him visible, but turn off his interaction
        if (phase2ObjectToReveal != null)
        {
            phase2ObjectToReveal.SetActive(true);
            var interaction = phase2ObjectToReveal.GetComponent<TwoWayInteraction>();
            if (interaction != null) interaction.enabled = false;
        }

        if (silasParticleSystem != null) silasParticleSystem.SetActive(false);

        foreach (var npc in npcMissions) npc.hasBeenTalkedTo = false;

        SetupEndTriggerListener();
        SetupWineCartTriggerListener();
        SetupInteriorTriggerListener(); // Initialize Phase 5 listener
    }

    void Update()
    {
        if (!explorationComplete) return;

        if (!phase1Complete)
            CheckNPCProgress();
        else if (!phase2Complete)
            CheckSilasProgress();
    }

    // --- EXPLORATION TRIGGER ---
    void SetupEndTriggerListener()
    {
        if (!explorationEndTrigger) return;
        TriggerListener listener = explorationEndTrigger.GetComponent<TriggerListener>();
        if (!listener) listener = explorationEndTrigger.AddComponent<TriggerListener>();
        listener.triggerTag = triggerTag;
        listener.onTriggerEnter = OnExplorationEnd;
    }

    void OnExplorationEnd(Collider other)
    {
        if (explorationComplete) return;
        explorationComplete = true;

        if (compass != null && !string.IsNullOrEmpty(exploreAreaQuestID))
            compass.HideMarker(exploreAreaQuestID);

        UpdateMissionUI($"Talk to the villagers (0/{npcMissions.Count})");
        PlayMissionSound();
        ShowNPCMarkers();
    }

    // --- PHASE 1 ---
    void CheckNPCProgress()
    {
        int count = 0;
        foreach (var npc in npcMissions)
        {
            if (npc.hasBeenTalkedTo) { count++; continue; }
            if (!npc.npcObject) { npc.hasBeenTalkedTo = true; count++; continue; }

            var interaction = npc.npcObject.GetComponent<TwoWayInteraction>();
            if (interaction && interaction.IsInteractionFinished())
            {
                npc.hasBeenTalkedTo = true;
                count++;
                if (compass && !string.IsNullOrEmpty(npc.compassQuestID)) compass.HideMarker(npc.compassQuestID);
            }
        }

        if (count != npcsTalkedTo)
        {
            npcsTalkedTo = count;
            UpdateMissionUI($"Talk to the villagers ({npcsTalkedTo}/{npcMissions.Count})");
            PlayMissionSound();

            if (npcsTalkedTo >= npcMissions.Count)
            {
                phase1Complete = true;
                StartPhase2();
            }
        }
    }

    void ShowNPCMarkers()
    {
        if (!compass) return;
        foreach (var npc in npcMissions)
            if (!npc.hasBeenTalkedTo && !string.IsNullOrEmpty(npc.compassQuestID))
                compass.ShowMarker(npc.compassQuestID);
    }

    // --- PHASE 2 ---
    void StartPhase2()
    {
        UpdateMissionUI(phase2Text);
        PlayMissionSound();

        if (phase2ObjectToReveal != null)
        {
            var interaction = phase2ObjectToReveal.GetComponent<TwoWayInteraction>();
            if (interaction != null) interaction.enabled = true;
        }

        if (silasParticleSystem != null) silasParticleSystem.SetActive(true);

        if (compass && !string.IsNullOrEmpty(phase2CompassQuestID))
            compass.ShowMarker(phase2CompassQuestID);
    }

    void CheckSilasProgress()
    {
        if (phase2ObjectToReveal == null) return;
        var silas = phase2ObjectToReveal.GetComponent<TwoWayInteraction>();
        if (silas != null && silas.IsInteractionFinished()) CompletePhase2();
    }

    public void CompletePhase2()
    {
        if (phase2Complete) return;
        phase2Complete = true;

        if (compass && !string.IsNullOrEmpty(phase2CompassQuestID))
            compass.HideMarker(phase2CompassQuestID);

        UpdateMissionUI(phase3Text);
        PlayMissionSound();

        if (compass && !string.IsNullOrEmpty(phase3CompassQuestID))
            compass.ShowMarker(phase3CompassQuestID);
    }

    // --- PHASE 3: WINE CART TRIGGER ---
    void SetupWineCartTriggerListener()
    {
        if (!wineCartTrigger) return;
        TriggerListener listener = wineCartTrigger.GetComponent<TriggerListener>();
        if (!listener) listener = wineCartTrigger.AddComponent<TriggerListener>();
        listener.triggerTag = triggerTag;
        listener.onTriggerEnter = OnWineCartReached;
    }

    void OnWineCartReached(Collider other)
    {
        if (phase3Complete) return;
        CompletePhase3();
    }

    public void CompletePhase3()
    {
        if (phase3Complete) return;
        phase3Complete = true;

        Debug.Log("✅ Phase 3 Complete - Wine cart reached");

        if (compass && !string.IsNullOrEmpty(phase3CompassQuestID))
            compass.HideMarker(phase3CompassQuestID);

        // DIRECTLY START PHASE 4 (Kill Enemies)
        StartPhase4_KillEnemies();
    }

    // --- PHASE 4: KILL ENEMIES ---
    void StartPhase4_KillEnemies()
    {
        EnemyHealthController.ResetDeathCount();
        enemiesKilled = 0;
        phase4Active = true;

        UpdateMissionUI($"Kill enemies (0/{enemiesToKill})");
        PlayMissionSound();

        Debug.Log("⚔️ Phase 4 Started - Kill enemies phase active");
    }

    private void UpdateKillMissionUI(int currentGlobalDeaths)
    {
        // Only update if Phase 4 is active and not yet complete
        if (!phase4Active || phase4Complete) return;

        enemiesKilled = currentGlobalDeaths;
        UpdateMissionUI($"Kill enemies ({enemiesKilled}/{enemiesToKill})");

        if (enemiesKilled >= enemiesToKill)
        {
            CompletePhase4();
        }
    }

    void CompletePhase4()
    {
        phase4Complete = true;
        Debug.Log("✅ Phase 4 Complete - All enemies defeated");

        // DIRECTLY START PHASE 5 (Enter Interior)
        StartPhase5_EnterInterior();
    }

    // --- PHASE 5: ENTER INTERIOR ---
    void StartPhase5_EnterInterior()
    {
        UpdateMissionUI(phase5Text);
        PlayMissionSound();

        Debug.Log("🏠 Phase 5 Started - Enter the interior.");

        if (compass && !string.IsNullOrEmpty(phase5CompassQuestID))
            compass.ShowMarker(phase5CompassQuestID);
    }

    void SetupInteriorTriggerListener()
    {
        if (!interiorEntranceTrigger) return;

        TriggerListener listener = interiorEntranceTrigger.GetComponent<TriggerListener>();
        if (!listener) listener = interiorEntranceTrigger.AddComponent<TriggerListener>();
        listener.triggerTag = triggerTag;
        listener.onTriggerEnter = OnInteriorEntered;
    }

    void OnInteriorEntered(Collider other)
    {
        // Only trigger if Phase 4 is finished
        if (phase5Complete || !phase4Complete) return;

        CompletePhase5();
    }

    void CompletePhase5()
    {
        phase5Complete = true;
        Debug.Log("✅ Phase 5 Complete - Entered Interior");

        if (compass && !string.IsNullOrEmpty(phase5CompassQuestID))
            compass.HideMarker(phase5CompassQuestID);

        UpdateMissionUI("Mission Complete!");
        PlayMissionSound();

        // Optional: Load Scene Logic
        // UnityEngine.SceneManagement.SceneManager.LoadScene("InteriorLevel");
    }

    // --- HELPER CLASS ---
    public class TriggerListener : MonoBehaviour
    {
        public string triggerTag = "Player";
        public System.Action<Collider> onTriggerEnter;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(triggerTag)) onTriggerEnter?.Invoke(other);
        }
    }

    // Helpers
    void UpdateMissionUI(string text) { if (missionText != null) missionText.text = text; }
    void PlayMissionSound() { if (audioSource != null && missionUpdateSound != null) audioSource.PlayOneShot(missionUpdateSound); }
}