using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

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
    public GameObject missionBox;
    public Image missionImage;
    public TextMeshProUGUI missionText;

    [Header("Exploration Phase")]
    public string triggerTag = "Player";
    public GameObject explorationEndTrigger;

    [Header("Phase 1: Talk to NPCs")]
    public List<NPCMissionData> npcMissions;

    [Header("Phase 2: Investigate")]
    public string phase2Text = "Investigate the strange particles.";
    public GameObject newObjectiveMarker;
    public GameObject phase2ObjectToReveal;
    public GameObject phase2ObjectToHide1;
    public GameObject phase2ObjectToHide2;
    public MonoBehaviour scriptToUnlock;
    public string phase2CompassQuestID = "SilasInteraction";

    [Header("Phase 3: Follow Cart")]
    public string phase3Text = "Follow the wine cart";
    public string phase3CompassQuestID = "FollowCart";

    [Header("Phase 4: Kill Enemies")]
    public int enemiesToKill = 10;
    private int enemiesKilled = 0;
    private bool phase4Active = false;
    private bool phase4Complete = false;

    public static event Action<int> OnMissionEnemyCountUpdated;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip missionUpdateSound;

    [Header("Compass Integration")]
    public Compass compass;
    public string exploreAreaQuestID = "Explore_Area";

    private int npcsTalkedTo = 0;
    private bool explorationComplete = false;
    private bool phase1Complete = false;
    private bool phase2Complete = false;
    private bool phase3Complete = false;

    private void OnEnable()
    {
        EnemyHealthController.OnEnemyCountUpdated += OnEnemyCountUpdated;
    }

    private void OnDisable()
    {
        EnemyHealthController.OnEnemyCountUpdated -= OnEnemyCountUpdated;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (compass == null) compass = FindObjectOfType<Compass>();

        if (newObjectiveMarker) newObjectiveMarker.SetActive(false);
        if (phase2ObjectToReveal) phase2ObjectToReveal.SetActive(false);
        if (phase2ObjectToHide1) phase2ObjectToHide1.SetActive(false);
        if (phase2ObjectToHide2) phase2ObjectToHide2.SetActive(false);
        if (scriptToUnlock) scriptToUnlock.enabled = false;

        foreach (var npc in npcMissions) npc.hasBeenTalkedTo = false;

        SetupEndTriggerListener();
    }

    void Update()
    {
        if (!explorationComplete) return;
        if (!phase1Complete) CheckNPCProgress();
    }

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
        if (compass && !string.IsNullOrEmpty(exploreAreaQuestID)) compass.HideMarker(exploreAreaQuestID);
        UpdateMissionUI($"Talk to the villagers (0/{npcMissions.Count})");
        PlayMissionSound();
        ShowNPCMarkers();
    }

    void ShowNPCMarkers()
    {
        if (!compass) return;
        foreach (var npc in npcMissions)
            if (!npc.hasBeenTalkedTo && !string.IsNullOrEmpty(npc.compassQuestID))
                compass.ShowMarker(npc.compassQuestID);
    }

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
            UpdateMissionUI($"Talk to the villagers ({count}/{npcMissions.Count})");
            PlayMissionSound();
            if (count >= npcMissions.Count)
            {
                phase1Complete = true;
                StartPhase2();
            }
        }
    }

    void StartPhase2()
    {
        UpdateMissionUI(phase2Text);
        PlayMissionSound();
        if (phase2ObjectToHide1) phase2ObjectToHide1.SetActive(true);
        if (phase2ObjectToHide2) phase2ObjectToHide2.SetActive(true);
        if (phase2ObjectToReveal) phase2ObjectToReveal.SetActive(true);
        if (newObjectiveMarker) newObjectiveMarker.SetActive(true);
        if (scriptToUnlock) scriptToUnlock.enabled = true;
        if (compass && !string.IsNullOrEmpty(phase2CompassQuestID)) compass.ShowMarker(phase2CompassQuestID);
    }

    public void CompletePhase2()
    {
        if (phase2Complete) return;
        phase2Complete = true;
        if (compass && !string.IsNullOrEmpty(phase2CompassQuestID)) compass.HideMarker(phase2CompassQuestID);
        UpdateMissionUI(phase3Text);
        PlayMissionSound();
        if (compass && !string.IsNullOrEmpty(phase3CompassQuestID)) compass.ShowMarker(phase3CompassQuestID);
    }

    public void CompletePhase3()
    {
        if (phase3Complete) return;
        phase3Complete = true;
        if (compass && !string.IsNullOrEmpty(phase3CompassQuestID)) compass.HideMarker(phase3CompassQuestID);

        // Ensure this is called to enable Phase 4 logic
        StartPhase4_KillEnemies();
    }

    void StartPhase4_KillEnemies()
    {
        EnemyHealthController.ResetDeathCount();
        enemiesKilled = 0;
        phase4Active = true;
        phase4Complete = false;

        UpdateMissionUI($"Kill enemies (0/{enemiesToKill})");
        PlayMissionSound();

        OnMissionEnemyCountUpdated?.Invoke(enemiesKilled);
        Debug.Log("[Mission] Phase 4 started: Kill Enemies");
    }

    void OnEnemyCountUpdated(int enemyDeathCount)
    {
        // CRITICAL: Only update if Phase 4 is actually running
        if (!phase4Active || phase4Complete) return;

        enemiesKilled = enemyDeathCount;

        UpdateMissionUI($"Kill enemies ({enemiesKilled}/{enemiesToKill})");
        PlayMissionSound();

        OnMissionEnemyCountUpdated?.Invoke(enemiesKilled);

        if (enemiesKilled >= enemiesToKill)
        {
            phase4Complete = true;
            phase4Active = false;
            Debug.Log($"[Mission] Phase 4 COMPLETE: Kill Enemies ({enemiesKilled}/{enemiesToKill})");

            // Trigger final mission completion or next phase here
            UpdateMissionUI("All enemies defeated!");
        }
    }

    void UpdateMissionUI(string text)
    {
        if (missionText) missionText.text = text;
    }

    void PlayMissionSound()
    {
        if (audioSource && missionUpdateSound) audioSource.PlayOneShot(missionUpdateSound);
    }

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