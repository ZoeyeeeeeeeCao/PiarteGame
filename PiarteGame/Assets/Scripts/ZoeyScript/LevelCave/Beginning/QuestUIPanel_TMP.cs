using UnityEngine;
using TMPro;

public class QuestUIPanel_TMP : MonoBehaviour
{
    [Header("UI (TMP)")]
    public TMP_Text objectiveText;
    public TMP_Text progressText;

    [Header("Task Text")]
    [TextArea(2, 4)] public string taskFindTheGod = "Find the God.";
    [TextArea(2, 4)] public string taskTalkToNpc = "Talk to the NPC.";
    [TextArea(2, 4)] public string taskLightBraziers = "Light all braziers.";
    [TextArea(2, 4)] public string taskReturnToNpc = "Return to the NPC.";
    [TextArea(2, 4)] public string taskDoorOpened = "Door opened. Proceed.";

    private QuestFinalSceneManager boundMgr;

    private void Start()
    {
        // Start 比 OnEnable 更晚一点，拿 Instance 的成功率更高
        TryBindManager();
        Refresh();
    }

    private void OnEnable()
    {
        TryBindManager();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    private void Update()
    {
        // ✅ 关键：如果一开始没拿到 Instance，这里会自动等它出现并绑定
        if (boundMgr == null && QuestFinalSceneManager.Instance != null)
        {
            TryBindManager();
            Refresh();
        }
    }

    private void TryBindManager()
    {
        var mgr = QuestFinalSceneManager.Instance;
        if (mgr == null) return;

        if (boundMgr == mgr) return;

        UnbindManager();
        boundMgr = mgr;
        boundMgr.OnQuestUIChanged += Refresh;
    }

    private void UnbindManager()
    {
        if (boundMgr != null)
        {
            boundMgr.OnQuestUIChanged -= Refresh;
            boundMgr = null;
        }
    }

    public void Refresh()
    {
        var mgr = boundMgr != null ? boundMgr : QuestFinalSceneManager.Instance;

        if (mgr == null)
        {
            if (objectiveText) objectiveText.text = "";
            if (progressText) progressText.text = "";
            return;
        }

        // Objective
        if (objectiveText)
        {
            switch (mgr.currentStage)
            {
                case QuestFinalSceneManager.Stage.FindTheGod:
                    objectiveText.text = taskFindTheGod;
                    break;
                case QuestFinalSceneManager.Stage.NeedTalkToNpcToStart:
                    objectiveText.text = taskTalkToNpc;
                    break;
                case QuestFinalSceneManager.Stage.LightAllBraziers:
                    objectiveText.text = taskLightBraziers;
                    break;
                case QuestFinalSceneManager.Stage.ReturnToNpc:
                    objectiveText.text = taskReturnToNpc;
                    break;
                case QuestFinalSceneManager.Stage.DoorOpened:
                    objectiveText.text = taskDoorOpened;
                    break;
            }
        }

        // Progress
        if (progressText)
        {
            if (mgr.currentStage == QuestFinalSceneManager.Stage.LightAllBraziers ||
                mgr.currentStage == QuestFinalSceneManager.Stage.ReturnToNpc)
            {
                progressText.text = $"Braziers: {mgr.LitCount}/{mgr.TotalCount}";
            }
            else
            {
                progressText.text = "";
            }
        }
    }
}
