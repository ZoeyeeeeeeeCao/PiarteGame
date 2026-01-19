using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestFinalSceneManager : MonoBehaviour
{
    public static QuestFinalSceneManager Instance { get; private set; }

    public enum Stage
    {
        FindTheGod,
        NeedTalkToNpcToStart,
        LightAllBraziers,
        ReturnToNpc,
        DoorOpened
    }

    [Header("Stage (Read Only)")]
    public Stage currentStage = Stage.FindTheGod;

    [Header("Braziers (assign in Inspector)")]
    public List<BrazierInteractable> braziers = new List<BrazierInteractable>();

    [Header("Door")]
    public Animator doorAnimator;
    public string doorOpenBool = "Door2Open";

    [Header("Inventory Replacement (Map -> Map_Glow)")]
    [Tooltip("Drag your initial Map ItemData here.")]
    public ItemData oldMapItem;

    [Tooltip("Drag your Map_Glow ItemData here.")]
    public ItemData newGlowMapItem;

    [Min(1)]
    public int replaceAmount = 1;

    [Tooltip("If true, only replace once even if OpenDoor is called again.")]
    public bool replaceOnlyOnce = true;

    [Header("Compass Integration")]
    [Tooltip("Reference to Compass component")]
    public Compass compass;

    [Header("Compass Marker IDs")]
    public string godCaveMarkerID = "god_cave";
    public string[] brazierMarkerIDs = new string[]
    {
        "brazier_1", "brazier_2", "brazier_3",
        "brazier_4", "brazier_5", "brazier_6"
    };
    public string npcMarkerID = "npc_return";
    public string beforeDoorMarkerID = "before_answer_door";
    public string doorMarkerID = "answer_door";
    public string afterDoorMarkerID = "after_answer_door";

    [Header("Optional Debug")]
    public bool logProgress = true;

    public event Action OnQuestUIChanged;
    public event Action OnDoorOpened;
    public event Action<int> OnBrazierLitWithIndex; // New: sends brazier index

    private bool replaced;
    private bool[] brazierWasLit; // Track which braziers were already lit

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize brazier tracking
        if (braziers != null && braziers.Count > 0)
        {
            brazierWasLit = new bool[braziers.Count];
        }

        // Auto-find compass if not assigned
        if (compass == null)
            compass = FindObjectOfType<Compass>();

        NotifyUI();
    }

    private void Start()
    {
        // Initialize compass markers based on current stage
        UpdateCompassMarkers();
    }

    private void NotifyUI()
    {
        OnQuestUIChanged?.Invoke();
    }

    public int LitCount
    {
        get
        {
            int lit = 0;
            if (braziers == null) return 0;
            for (int i = 0; i < braziers.Count; i++)
                if (braziers[i] != null && braziers[i].IsLit) lit++;
            return lit;
        }
    }

    public int TotalCount
    {
        get
        {
            int total = 0;
            if (braziers == null) return 0;
            for (int i = 0; i < braziers.Count; i++)
                if (braziers[i] != null) total++;
            return total;
        }
    }

    public bool AllBraziersLit()
    {
        int total = TotalCount;
        if (total <= 0) return false;
        return LitCount >= total;
    }

    public bool CanLightBraziers()
    {
        return currentStage == Stage.LightAllBraziers;
    }

    public void CompleteFindTheGod()
    {
        if (currentStage != Stage.FindTheGod) return;

        currentStage = Stage.NeedTalkToNpcToStart;

        if (logProgress) Debug.Log("Objective complete: Find the God -> Talk to NPC");

        // ✅ Step 2: Remove god cave marker (will show braziers when NPC starts quest)
        if (compass != null)
        {
            compass.HideMarker(godCaveMarkerID);
            if (logProgress) Debug.Log("[Compass] God cave marker removed");
        }

        NotifyUI();
    }

    public void NotifyBrazierLit(BrazierInteractable brazier)
    {
        if (currentStage != Stage.LightAllBraziers) return;
        if (brazier == null) return;
        if (braziers == null || !braziers.Contains(brazier)) return;

        // Find which brazier was lit
        int brazierIndex = braziers.IndexOf(brazier);

        if (logProgress)
            Debug.Log($"Brazier lit: {LitCount}/{TotalCount}");

        // ✅ Step 3: Hide this specific brazier's marker
        if (compass != null && brazierIndex >= 0 && brazierIndex < brazierMarkerIDs.Length)
        {
            if (!brazierWasLit[brazierIndex]) // Only hide once
            {
                compass.HideMarker(brazierMarkerIDs[brazierIndex]);
                brazierWasLit[brazierIndex] = true;
                if (logProgress) Debug.Log($"[Compass] Brazier {brazierIndex + 1} marker removed");
            }
        }

        OnBrazierLitWithIndex?.Invoke(brazierIndex);
        NotifyUI();

        if (AllBraziersLit())
        {
            currentStage = Stage.ReturnToNpc;

            // ✅ Step 4: Show NPC marker when all braziers lit
            if (compass != null)
            {
                compass.ShowMarker(npcMarkerID);
                if (logProgress) Debug.Log("[Compass] NPC return marker shown");
            }

            if (logProgress) Debug.Log("All braziers lit! Return to NPC.");
            NotifyUI();
        }
    }

    public void OnNpcTalk_StartQuest()
    {
        if (currentStage != Stage.NeedTalkToNpcToStart) return;

        currentStage = Stage.LightAllBraziers;

        // ✅ Step 2: Show all brazier markers
        if (compass != null)
        {
            for (int i = 0; i < Mathf.Min(brazierMarkerIDs.Length, braziers.Count); i++)
            {
                // Only show markers for unlit braziers
                if (braziers[i] != null && !braziers[i].IsLit)
                {
                    compass.ShowMarker(brazierMarkerIDs[i]);
                }
            }
            if (logProgress) Debug.Log("[Compass] All brazier markers shown");
        }

        if (logProgress) Debug.Log("Quest started: Light all braziers.");
        NotifyUI();
    }

    public void OnNpcTalk_OpenDoorIfReady()
    {
        if (currentStage != Stage.ReturnToNpc) return;

        if (!AllBraziersLit())
        {
            if (logProgress) Debug.Log("Not ready: Some braziers are still unlit.");
            NotifyUI();
            return;
        }

        OpenDoor();
    }

    private void OpenDoor()
    {
        if (currentStage == Stage.DoorOpened) return;

        currentStage = Stage.DoorOpened;

        ReplaceInventoryMapToGlow();

        if (doorAnimator)
            doorAnimator.SetBool(doorOpenBool, true);

        // ✅ Step 5: Hide NPC marker, show "before door" marker
        if (compass != null)
        {
            compass.HideMarker(npcMarkerID);
            compass.ShowMarker(beforeDoorMarkerID);
            if (logProgress) Debug.Log("[Compass] Before-door marker shown, NPC marker removed");
        }

        if (logProgress) Debug.Log("Door opened!");

        NotifyUI();
        OnDoorOpened?.Invoke();
    }

    private void UpdateCompassMarkers()
    {
        if (compass == null) return;

        // Update markers based on current stage (useful for scene reload)
        switch (currentStage)
        {
            case Stage.FindTheGod:
                // Step 1: Show god cave marker
                compass.ShowMarker(godCaveMarkerID);
                break;

            case Stage.NeedTalkToNpcToStart:
                // Waiting for NPC interaction
                compass.HideAllMarkers();
                break;

            case Stage.LightAllBraziers:
                // Show unlit brazier markers
                for (int i = 0; i < Mathf.Min(brazierMarkerIDs.Length, braziers.Count); i++)
                {
                    if (braziers[i] != null && !braziers[i].IsLit)
                    {
                        compass.ShowMarker(brazierMarkerIDs[i]);
                    }
                }
                break;

            case Stage.ReturnToNpc:
                // Step 4: Show NPC marker
                compass.ShowMarker(npcMarkerID);
                break;

            case Stage.DoorOpened:
                // Step 5: Show before-door marker
                compass.ShowMarker(beforeDoorMarkerID);
                break;
        }
    }

    private void ReplaceInventoryMapToGlow()
    {
        if (replaceOnlyOnce && replaced) return;

        if (oldMapItem == null || newGlowMapItem == null)
        {
            if (logProgress) Debug.LogWarning("[QuestFinalSceneManager] Map replacement skipped: oldMapItem/newGlowMapItem not assigned.");
            return;
        }

        int amt = Mathf.Max(1, replaceAmount);

        if (!StaticInventory.Has(oldMapItem, amt))
        {
            if (logProgress) Debug.LogWarning($"[QuestFinalSceneManager] Player does not have {oldMapItem.displayName} x{amt}. Skip replacement.");
            return;
        }

        bool removed = StaticInventory.Remove(oldMapItem, amt);
        if (!removed)
        {
            if (logProgress) Debug.LogWarning("[QuestFinalSceneManager] Remove old map failed. Skip replacement.");
            return;
        }

        bool added = StaticInventory.Add(newGlowMapItem, amt);
        if (!added)
        {
            StaticInventory.Add(oldMapItem, amt);
            if (logProgress) Debug.LogWarning($"[QuestFinalSceneManager] Add glow map failed. Rolled back old map.");
            return;
        }

        replaced = true;

        if (logProgress)
            Debug.Log($"[QuestFinalSceneManager] Replaced inventory: -{oldMapItem.displayName} x{amt}, +{newGlowMapItem.displayName} x{amt}");
    }
}