using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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

    [Header("Phase 3.5: Head to Location")]
    public string phase3_5_Text = "Head to the ambush location";
    public GameObject headToLocationTrigger;
    public string phase3_5_CompassQuestID = "AmbushLocation";

    [Header("Phase 4: Kill Enemies")]
    public int enemiesToKill = 10;
    private int enemiesKilled = 0;
    private bool phase4Active = false;
    private bool phase4Complete = false;

    [Header("Audio & Compass")]
    public AudioSource audioSource;
    public AudioClip missionUpdateSound;
    public Compass compass;

    private bool explorationComplete = false;
    private bool phase1Complete = false;
    private bool phase2Complete = false;
    private bool phase3Complete = false;
    private bool phase3_5_Complete = false;

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
        SetupHeadToLocationTriggerListener();
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
        {
            compass.HideMarker(exploreAreaQuestID);
        }

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

        // Hide wine cart marker
        if (compass && !string.IsNullOrEmpty(phase3CompassQuestID))
            compass.HideMarker(phase3CompassQuestID);

        // Start Phase 3.5 - Head to location
        StartPhase3_5_HeadToLocation();
    }

    // --- PHASE 3.5: HEAD TO LOCATION ---
    void StartPhase3_5_HeadToLocation()
    {
        UpdateMissionUI(phase3_5_Text);
        PlayMissionSound();

        Debug.Log("🎯 Phase 3.5 Started - Show ambush location marker");

        // Show the new location marker
        if (compass && !string.IsNullOrEmpty(phase3_5_CompassQuestID))
            compass.ShowMarker(phase3_5_CompassQuestID);
    }

    void SetupHeadToLocationTriggerListener()
    {
        if (!headToLocationTrigger) return;

        TriggerListener listener = headToLocationTrigger.GetComponent<TriggerListener>();
        if (!listener) listener = headToLocationTrigger.AddComponent<TriggerListener>();

        listener.triggerTag = triggerTag;
        listener.onTriggerEnter = OnLocationReached;
    }

    void OnLocationReached(Collider other)
    {
        if (phase3_5_Complete) return;

        CompletePhase3_5();
    }

    void CompletePhase3_5()
    {
        if (phase3_5_Complete) return;
        phase3_5_Complete = true;

        Debug.Log("✅ Phase 3.5 Complete - Ambush location reached");

        // Hide the location marker
        if (compass && !string.IsNullOrEmpty(phase3_5_CompassQuestID))
            compass.HideMarker(phase3_5_CompassQuestID);

        // Start the kill enemies phase
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
        if (!phase4Active || phase4Complete) return;
        enemiesKilled = currentGlobalDeaths;
        UpdateMissionUI($"Kill enemies ({enemiesKilled}/{enemiesToKill})");
        if (enemiesKilled >= enemiesToKill)
        {
            phase4Complete = true;
            UpdateMissionUI("All enemies defeated! Return to Celyne.");
            PlayMissionSound();
        }
    }

    void UpdateMissionUI(string text) { if (missionText != null) missionText.text = text; }
    void PlayMissionSound() { if (audioSource != null && missionUpdateSound != null) audioSource.PlayOneShot(missionUpdateSound); }

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
}

