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

    [Header("Quest UI SFX (2D)")]
    [Tooltip("Played when quest UI actually changes (including first load refresh).")]
    public AudioClip refreshSfx;

    [Range(0f, 1f)] public float refreshSfxVolume = 1f;

    [Tooltip("Optional. If empty, will auto-create a 2D AudioSource on this GameObject.")]
    public AudioSource uiSfxSource;

    [Tooltip("If true, also play sound when only brazier progress changes (e.g., 1/4 -> 2/4).")]
    public bool playOnProgressChange = true;

    private QuestFinalSceneManager boundMgr;

    // ✅ 用来防止重复播放（因为 Refresh 会被多次调用）
    private string lastObjectiveStr = null;
    private string lastProgressStr = null;
    private bool hasEverRefreshed = false;

    private void Start()
    {
        EnsureAudioSource2D();

        TryBindManager();
        Refresh(); // ✅ 首次刷新（你要的“loadscene就播”）
    }

    private void OnEnable()
    {
        EnsureAudioSource2D();

        TryBindManager();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    private void Update()
    {
        // ✅ 如果一开始没拿到 Instance，这里会自动等它出现并绑定
        if (boundMgr == null && QuestFinalSceneManager.Instance != null)
        {
            TryBindManager();
            Refresh();
        }
    }

    private void EnsureAudioSource2D()
    {
        if (!refreshSfx) return;

        if (!uiSfxSource)
        {
            uiSfxSource = GetComponent<AudioSource>();
            if (!uiSfxSource) uiSfxSource = gameObject.AddComponent<AudioSource>();
        }

        uiSfxSource.playOnAwake = false;
        uiSfxSource.spatialBlend = 0f; // ✅ 强制 2D
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

        // ---------- 1) 计算将要显示的文本 ----------
        string newObjective = "";
        string newProgress = "";

        switch (mgr.currentStage)
        {
            case QuestFinalSceneManager.Stage.FindTheGod:
                newObjective = taskFindTheGod;
                break;
            case QuestFinalSceneManager.Stage.NeedTalkToNpcToStart:
                newObjective = taskTalkToNpc;
                break;
            case QuestFinalSceneManager.Stage.LightAllBraziers:
                newObjective = taskLightBraziers;
                break;
            case QuestFinalSceneManager.Stage.ReturnToNpc:
                newObjective = taskReturnToNpc;
                break;
            case QuestFinalSceneManager.Stage.DoorOpened:
                newObjective = taskDoorOpened;
                break;
        }

        if (mgr.currentStage == QuestFinalSceneManager.Stage.LightAllBraziers ||
            mgr.currentStage == QuestFinalSceneManager.Stage.ReturnToNpc)
        {
            newProgress = $"Braziers: {mgr.LitCount}/{mgr.TotalCount}";
        }
        else
        {
            newProgress = "";
        }

        // ---------- 2) 判断是否需要播放声音 ----------
        bool objectiveChanged = (lastObjectiveStr != newObjective);
        bool progressChanged = (lastProgressStr != newProgress);

        bool shouldPlay =
            !hasEverRefreshed // ✅ 第一次刷新必播（包含loadscene）
            || objectiveChanged
            || (playOnProgressChange && progressChanged);

        // ---------- 3) 写入UI ----------
        if (objectiveText) objectiveText.text = newObjective;
        if (progressText) progressText.text = newProgress;

        // ---------- 4) 播放SFX ----------
        if (shouldPlay && refreshSfx && uiSfxSource)
        {
            uiSfxSource.PlayOneShot(refreshSfx, refreshSfxVolume);
        }

        // ---------- 5) 更新缓存 ----------
        hasEverRefreshed = true;
        lastObjectiveStr = newObjective;
        lastProgressStr = newProgress;
    }
}
