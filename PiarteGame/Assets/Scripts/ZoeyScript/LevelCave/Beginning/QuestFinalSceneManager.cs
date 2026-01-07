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

    [Header("Optional Debug")]
    public bool logProgress = true;

    public event Action OnQuestUIChanged;
    public event Action OnDoorOpened; // ✅ 新增：CameraCut订阅这个

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
        currentStage = Stage.DoorOpened;

        if (doorAnimator)
            doorAnimator.SetBool(doorOpenBool, true);

        if (logProgress) Debug.Log("Door opened!");

        NotifyUI();

        OnDoorOpened?.Invoke(); // ✅ 关键：触发镜头切换
    }
}
