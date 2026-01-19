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

    // ✅ NEW: UI to hide during dialogue
    [Header("Hide UI During Dialogue")]
    [Tooltip("Drag any UI roots you want hidden during dialogue (mission canvas, compass UI canvas, interact prompts, etc.)")]
    public GameObject[] uiRootsToHide;

    [Tooltip("If true, compass markers will be hidden while in dialogue and restored after.")]
    public bool hideCompassMarkersDuringDialogue = false;

    private bool inDialogue = false;
    private readonly List<string> markersHiddenForDialogue = new();

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

    [Header("Phase 4: Kill Enemies")]
    public int enemiesToKill = 10;
    private int enemiesKilled = 0;
    private bool phase4Active = false;
    private bool phase4Complete = false;

    [Header("Phase 5: Enter Interior")]
    public string phase5Text = "Enter the Main Hall";
    public GameObject interiorEntranceTrigger;
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
        SetupInteriorTriggerListener();
    }

    void Update()
    {
        // ✅ If you want missions to STILL update during dialogue, remove this early return.
        // I recommend pausing mission updates so UI doesn't change while talking.
        if (inDialogue) return;

        if (!explorationComplete) return;

        if (!phase1Complete)
            CheckNPCProgress();
        else if (!phase2Complete)
            CheckSilasProgress();
    }

    // =========================
    // ✅ PUBLIC API FOR DIALOGUE
    // =========================
    public void EnterDialogueMode()
    {
        if (inDialogue) return;
        inDialogue = true;

        // Hide chosen UI roots
        if (uiRootsToHide != null)
        {
            for (int i = 0; i < uiRootsToHide.Length; i++)
            {
                if (uiRootsToHide[i] != null)
                    uiRootsToHide[i].SetActive(false);
            }
        }

        // Optional: hide compass markers during dialogue
        if (hideCompassMarkersDuringDialogue && compass != null)
        {
            // If your Compass has HideAllMarkers(), use it:
            // BUT we need to restore after. If your Compass doesn't support "get active markers",
            // you can either skip restore or manually list IDs to hide.
            // Here is a safe minimal behavior: hide all (no restore) OR hide specific known IDs.
            compass.HideAllMarkers();
        }
    }

    public void ExitDialogueMode()
    {
        if (!inDialogue) return;
        inDialogue = false;

        // Show UI again
        if (uiRootsToHide != null)
        {
            for (int i = 0; i < uiRootsToHide.Length; i++)
            {
                if (uiRootsToHide[i] != null)
                    uiRootsToHide[i].SetActive(true);
            }
        }

        // If you hid all markers above and want them back,
        // you should re-show the current objective marker(s) here.
        // (Best approach: just show markers relevant to current phase.)
        RestoreCurrentObjectiveMarkers();
    }

    private void RestoreCurrentObjectiveMarkers()
    {
        if (compass == null) return;

        // Re-show only what should be visible for the current mission state.
        // This avoids needing a "get all active markers" function.
        if (!explorationComplete)
        {
            if (!string.IsNullOrEmpty(exploreAreaQuestID)) compass.ShowMarker(exploreAreaQuestID);
            return;
        }

        if (!phase1Complete)
        {
            ShowNPCMarkers();
            return;
        }

        if (!phase2Complete)
        {
            if (!string.IsNullOrEmpty(phase2CompassQuestID)) compass.ShowMarker(phase2CompassQuestID);
            return;
        }

        if (!phase3Complete)
        {
            if (!string.IsNullOrEmpty(phase3CompassQuestID)) compass.ShowMarker(phase3CompassQuestID);
            return;
        }

        if (phase4Active && !phase4Complete)
        {
            // No marker necessarily; do nothing
            return;
        }

        if (phase4Complete && !phase5Complete)
        {
            if (!string.IsNullOrEmpty(phase5CompassQuestID)) compass.ShowMarker(phase5CompassQuestID);
            return;
        }
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

    // --- PHASE 3 ---
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

        if (compass && !string.IsNullOrEmpty(phase3CompassQuestID))
            compass.HideMarker(phase3CompassQuestID);

        StartPhase4_KillEnemies();
    }

    // --- PHASE 4 ---
    void StartPhase4_KillEnemies()
    {
        EnemyHealthController.ResetDeathCount();
        enemiesKilled = 0;
        phase4Active = true;

        UpdateMissionUI($"Kill enemies (0/{enemiesToKill})");
        PlayMissionSound();
    }

    private void UpdateKillMissionUI(int currentGlobalDeaths)
    {
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
        StartPhase5_EnterInterior();
    }

    // --- PHASE 5 ---
    void StartPhase5_EnterInterior()
    {
        UpdateMissionUI(phase5Text);
        PlayMissionSound();

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
        if (phase5Complete || !phase4Complete) return;
        CompletePhase5();
    }

    void CompletePhase5()
    {
        phase5Complete = true;

        if (compass && !string.IsNullOrEmpty(phase5CompassQuestID))
            compass.HideMarker(phase5CompassQuestID);

        UpdateMissionUI("Mission Complete!");
        PlayMissionSound();
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
