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

    [Header("Optional Debug")]
    public bool logProgress = true;

    public event Action OnQuestUIChanged;
    public event Action OnDoorOpened; // CameraCut can subscribe this

    private bool replaced; // 防重复

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        NotifyUI();
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
        NotifyUI();
    }

    public void NotifyBrazierLit(BrazierInteractable brazier)
    {
        if (currentStage != Stage.LightAllBraziers) return;
        if (brazier == null) return;
        if (braziers == null || !braziers.Contains(brazier)) return;

        if (logProgress)
            Debug.Log($"Brazier lit: {LitCount}/{TotalCount}");

        NotifyUI();

        if (AllBraziersLit())
        {
            currentStage = Stage.ReturnToNpc;
            if (logProgress) Debug.Log("All braziers lit! Return to NPC.");
            NotifyUI();
        }
    }

    public void OnNpcTalk_StartQuest()
    {
        if (currentStage != Stage.NeedTalkToNpcToStart) return;

        currentStage = Stage.LightAllBraziers;
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

        // ✅ 在开门这一刻替换背包物品
        ReplaceInventoryMapToGlow();

        if (doorAnimator)
            doorAnimator.SetBool(doorOpenBool, true);

        if (logProgress) Debug.Log("Door opened!");

        NotifyUI();

        OnDoorOpened?.Invoke(); // trigger camera cut etc.
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

        // 如果玩家没有旧地图，也可以选择直接给新地图（这里按“没有就不替换”，你想改我也能改）
        if (!StaticInventory.Has(oldMapItem, amt))
        {
            if (logProgress) Debug.LogWarning($"[QuestFinalSceneManager] Player does not have {oldMapItem.displayName} x{amt}. Skip replacement.");
            return;
        }

        // ✅ 事务式替换：先移除旧的 -> 再添加新的
        // 如果添加失败，回滚把旧的加回去，避免背包丢物品
        bool removed = StaticInventory.Remove(oldMapItem, amt);
        if (!removed)
        {
            if (logProgress) Debug.LogWarning("[QuestFinalSceneManager] Remove old map failed. Skip replacement.");
            return;
        }

        bool added = StaticInventory.Add(newGlowMapItem, amt);
        if (!added)
        {
            // 回滚
            StaticInventory.Add(oldMapItem, amt);
            if (logProgress) Debug.LogWarning($"[QuestFinalSceneManager] Add glow map failed. Rolled back old map.");
            return;
        }

        replaced = true;

        if (logProgress)
            Debug.Log($"[QuestFinalSceneManager] Replaced inventory: -{oldMapItem.displayName} x{amt}, +{newGlowMapItem.displayName} x{amt}");
    }
}
