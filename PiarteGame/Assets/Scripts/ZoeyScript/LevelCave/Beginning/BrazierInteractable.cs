using UnityEngine;

public class BrazierInteractable : MonoBehaviour
{
    [Header("Visual Parts (disabled at start except mesh)")]
    public ParticleSystem fireParticles;
    public ParticleSystem smokeParticles;
    public Light pointLight;

    [Header("Interaction")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("Optional: world-space 'Press E' hint")]
    public GameObject pressEIndicator;

    [Header("SFX")]
    [Tooltip("Optional AudioSource. If empty, one will be added automatically.")]
    public AudioSource sfxSource;

    [Tooltip("Fire ignition sound")]
    public AudioClip igniteClip;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("State (Read Only)")]
    [SerializeField] private bool isLit;
    public bool IsLit => isLit;

    bool playerInRange;

    // ===================== Unity =====================

    private void Awake()
    {
        // 初始视觉状态：火焰 / 烟 / 灯全部关
        SetLitVisual(false);

        if (pressEIndicator)
            pressEIndicator.SetActive(false);

        // 准备 AudioSource（防呆）
        if (!sfxSource)
        {
            sfxSource = GetComponent<AudioSource>();
            if (!sfxSource)
                sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 1f; // 3D 声音（非常重要）
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (isLit) return;

        // 任务阶段未允许时，不可点火
        if (QuestFinalSceneManager.Instance != null &&
            !QuestFinalSceneManager.Instance.CanLightBraziers())
            return;

        if (Input.GetKeyDown(interactKey))
        {
            LightUp();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;

        if (!isLit && pressEIndicator)
            pressEIndicator.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;

        if (pressEIndicator)
            pressEIndicator.SetActive(false);
    }

    // ===================== Logic =====================

    public void LightUp()
    {
        if (isLit) return;

        isLit = true;

        // 视觉
        SetLitVisual(true);

        // 🔊 音效
        if (igniteClip && sfxSource)
            sfxSource.PlayOneShot(igniteClip, sfxVolume);

        // UI 提示关闭
        if (pressEIndicator)
            pressEIndicator.SetActive(false);

        // 通知任务系统
        if (QuestFinalSceneManager.Instance != null)
            QuestFinalSceneManager.Instance.NotifyBrazierLit(this);
    }

    private void SetLitVisual(bool lit)
    {
        if (fireParticles)
        {
            fireParticles.gameObject.SetActive(lit);
            if (lit) fireParticles.Play(true);
            else fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (smokeParticles)
        {
            smokeParticles.gameObject.SetActive(lit);
            if (lit) smokeParticles.Play(true);
            else smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (pointLight)
            pointLight.enabled = lit;
    }
}
