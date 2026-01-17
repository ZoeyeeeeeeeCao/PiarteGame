using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChainPullDoor : MonoBehaviour
{
    [Header("Input")]
    public KeyCode tapKey = KeyCode.P;

    [Header("Progress (0..1)")]
    [Range(0f, 1f)] public float progress = 0f;
    public float tapGainBase = 0.08f;
    public float decayPerSecondBase = 0.12f;

    [Header("Resistance (last 20% harder)")]
    [Range(0.05f, 0.5f)] public float lastSection = 0.20f;
    [Range(0.1f, 1f)] public float lastSectionGainMultiplier = 0.45f;
    [Range(1f, 5f)] public float lastSectionDecayMultiplier = 2.2f;

    [Header("Door Motion")]
    public Transform door;
    public Vector3 doorClosedLocalPos;
    public Vector3 doorOpenLocalPos;
    public float doorSmooth = 10f;

    [Header("Chain Visual Motion (move the PARENT)")]
    public Transform chainParent;                 // empty parent with mesh children
    public Vector3 chainRestLocalPos;
    public Vector3 chainPulledLocalPos;
    public float chainPullSnap = 18f;
    public float chainRelaxSmooth = 10f;
    [Range(0f, 1f)] public float chainPullAmount = 0f;

    [Header("Player Lock (fix 'pulling air')")]
    public Transform pullStandPoint;
    public Transform lookAtTarget;                // chain parent or a target near chain
    public float snapToStandSpeed = 12f;
    public float rotateToTargetSpeed = 12f;
    public bool hardLockPosition = true;          // true = keep player glued to stand point
    public bool disableCharacterController = true;

    [Header("UI (small 'P' popup + optional bar)")]
    public CanvasGroup uiGroup;
    public TMP_Text keyText;
    public Slider progressBar;

    [Header("Animation (single loop)")]
    public Animator playerAnimator;
    public string pullingBool = "IsPulling";

    [Header("Audio Sources")]
    public AudioSource chainSource;        // loop: pull/drop (swap)
    public AudioSource doorLoopSource;     // loop: opening
    public AudioSource doorOneShotSource;  // one-shot: closing bam

    [Header("Audio Clips")]
    public AudioClip chainPullLoop;
    public AudioClip chainDropLoop;
    public AudioClip stoneDoorOpenLoop;
    public AudioClip stoneDoorCloseBam;

    [Header("Audio Tuning")]
    public float chainVol = 0.6f;
    public float doorOpenVol = 0.65f;

    [Header("Interaction Behavior")]
    [Tooltip("If true: when door fully opens, interaction stops and player is released.")]
    public bool releaseOnComplete = true;
    [Tooltip("If true: tapping starts interaction. If false: interaction is always active while in zone.")]
    public bool requireTapToStart = true;
    [Tooltip("Stop pulling animation if no tap happens for this many seconds (but progress can still decay).")]
    public float idleStopAnimAfter = 0.35f;

    // runtime
    private bool playerInZone;
    private bool completed;
    private float lastTapTime = -999f;

    private bool isInteracting; // <-- KEY FIX: only lock/animate when true

    private Transform playerRoot;
    private CharacterController cachedCC;
    private bool ccWasEnabled;

    private enum ChainState { None, Pulling, Dropping }
    private ChainState chainState = ChainState.None;

    private void Awake()
    {
        if (door != null) doorClosedLocalPos = door.localPosition;
        if (chainParent != null) chainRestLocalPos = chainParent.localPosition;

        SetUI(false);
        SetPullingAnim(false);
        StopChain();
        StopDoorOpenLoop();
    }

    private void Update()
    {
        if (!playerInZone) return;

        // Door already opened -> do nothing (player can freely leave)
        if (completed)
        {
            // keep UI/anim off just in case
            SetUI(false);
            SetPullingAnim(false);
            StopChain();
            StopDoorOpenLoop();
            return;
        }

        float dt = Time.deltaTime;
        bool tapped = Input.GetKeyDown(tapKey);

        // Start interaction only when player taps (prevents auto animation on enter)
        if (requireTapToStart && !isInteracting && tapped)
        {
            isInteracting = true;
        }
        else if (!requireTapToStart)
        {
            isInteracting = true;
        }

        // If not interacting yet: show only "P" popup (optional), but don't lock/animate/door-move.
        // (If you want ZERO UI until they tap, change SetUI(true) to SetUI(false) here.)
        if (!isInteracting)
        {
            SetUI(true);
            if (keyText != null) keyText.text = "P";
            if (progressBar != null) progressBar.value = progress;

            SetPullingAnim(false);
            StopChain();
            StopDoorOpenLoop();
            return;
        }

        // While interacting, we lock the player + rotate to chain
        UpdatePlayerLock(dt);

        // UI + animation ON only when interacting
        SetUI(true);
        if (keyText != null) keyText.text = "P";

        // Animation should NOT run forever if they stop tapping
        bool recentlyTapped = (Time.time - lastTapTime) <= idleStopAnimAfter;
        SetPullingAnim(recentlyTapped);

        float gainMult = 1f;
        float decayMult = 1f;
        if (progress >= 1f - lastSection)
        {
            gainMult *= lastSectionGainMultiplier;
            decayMult *= lastSectionDecayMultiplier;
        }

        float oldProgress = progress;

        if (tapped)
        {
            lastTapTime = Time.time;
            progress = Mathf.Clamp01(progress + tapGainBase * gainMult);

            // Chain visual: yank a bit
            chainPullAmount = Mathf.Clamp01(chainPullAmount + 0.55f);

            EnsureChainLoop(ChainState.Pulling);
        }
        else
        {
            // decay even while interacting (door slides back if you stop)
            progress = Mathf.Clamp01(progress - (decayPerSecondBase * decayMult * dt));

            // relax
            chainPullAmount = Mathf.MoveTowards(chainPullAmount, 0f, dt * 1.2f);

            if (progress < oldProgress - 0.00001f && progress > 0f)
                EnsureChainLoop(ChainState.Dropping);

            if (progress <= 0.0001f)
                StopChain();
        }

        UpdateChainVisual(dt);
        UpdateDoor(dt);

        // door opening loop only when moving UP
        bool movingUp = progress > oldProgress + 0.00001f;
        UpdateDoorAudio(movingUp);

        // BAM when fully drops
        if (oldProgress > 0.01f && progress <= 0.0001f)
        {
            PlayDoorCloseBam();
            StopDoorOpenLoop();
            StopChain();
        }

        if (progressBar != null) progressBar.value = progress;

        // COMPLETE -> release player so they can walk away
        if (progress >= 0.999f)
        {
            completed = true;
            progress = 1f;
            if (progressBar != null) progressBar.value = 1f;

            StopChain();
            StopDoorOpenLoop();
            SetPullingAnim(false);

            if (door != null) door.localPosition = doorOpenLocalPos;

            if (releaseOnComplete)
            {
                isInteracting = false;
                ReleasePlayerLock();  // <-- KEY FIX: unstuck
                SetUI(false);
            }
        }
    }

    private void UpdatePlayerLock(float dt)
    {
        if (playerRoot == null || pullStandPoint == null) return;

        // Position
        Vector3 targetPos = pullStandPoint.position;

        if (disableCharacterController && cachedCC != null && cachedCC.enabled)
        {
            cachedCC.enabled = false;
        }

        if (hardLockPosition)
        {
            playerRoot.position = Vector3.Lerp(playerRoot.position, targetPos, 1f - Mathf.Exp(-snapToStandSpeed * dt));
        }
        else
        {
            float dist = Vector3.Distance(playerRoot.position, targetPos);
            if (dist > 0.15f)
                playerRoot.position = Vector3.Lerp(playerRoot.position, targetPos, 1f - Mathf.Exp(-snapToStandSpeed * dt));
        }

        // Rotation
        Transform look = lookAtTarget != null ? lookAtTarget : chainParent;
        if (look != null)
        {
            Vector3 flatDir = look.position - playerRoot.position;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
                playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, targetRot, 1f - Mathf.Exp(-rotateToTargetSpeed * dt));
            }
        }
    }

    private void ReleasePlayerLock()
    {
        if (disableCharacterController && cachedCC != null)
        {
            cachedCC.enabled = ccWasEnabled;
        }
    }

    private void UpdateDoor(float dt)
    {
        if (door == null) return;
        Vector3 target = Vector3.Lerp(doorClosedLocalPos, doorOpenLocalPos, progress);
        door.localPosition = Vector3.Lerp(door.localPosition, target, 1f - Mathf.Exp(-doorSmooth * dt));
    }

    private void UpdateChainVisual(float dt)
    {
        if (chainParent == null) return;

        Vector3 target = Vector3.Lerp(chainRestLocalPos, chainPulledLocalPos, chainPullAmount);

        float speed = (chainPullAmount > 0.01f && Time.time - lastTapTime < 0.25f)
            ? chainPullSnap
            : chainRelaxSmooth;

        chainParent.localPosition = Vector3.Lerp(chainParent.localPosition, target, 1f - Mathf.Exp(-speed * dt));
    }

    private void UpdateDoorAudio(bool movingUp)
    {
        if (doorLoopSource == null || stoneDoorOpenLoop == null) return;

        if (movingUp && !completed)
        {
            if (!doorLoopSource.isPlaying || doorLoopSource.clip != stoneDoorOpenLoop)
            {
                doorLoopSource.clip = stoneDoorOpenLoop;
                doorLoopSource.loop = true;
                doorLoopSource.volume = doorOpenVol;
                doorLoopSource.Play();
            }
        }
        else
        {
            StopDoorOpenLoop();
        }
    }

    private void StopDoorOpenLoop()
    {
        if (doorLoopSource != null && doorLoopSource.isPlaying)
            doorLoopSource.Stop();
    }

    private void PlayDoorCloseBam()
    {
        if (doorOneShotSource == null || stoneDoorCloseBam == null) return;
        doorOneShotSource.PlayOneShot(stoneDoorCloseBam);
    }

    private void EnsureChainLoop(ChainState desired)
    {
        if (chainSource == null) return;

        AudioClip desiredClip = desired switch
        {
            ChainState.Pulling => chainPullLoop,
            ChainState.Dropping => chainDropLoop,
            _ => null
        };

        if (desiredClip == null)
        {
            StopChain();
            return;
        }

        if (!chainSource.isPlaying || chainSource.clip != desiredClip)
        {
            chainSource.clip = desiredClip;
            chainSource.loop = true;
            chainSource.volume = chainVol;
            chainSource.Play();
        }

        chainState = desired;
    }

    private void StopChain()
    {
        if (chainSource != null && chainSource.isPlaying)
            chainSource.Stop();
        chainState = ChainState.None;
    }

    private void SetUI(bool show)
    {
        if (uiGroup == null) return;
        uiGroup.alpha = show ? 1f : 0f;
        uiGroup.blocksRaycasts = false;
        uiGroup.interactable = false;
    }

    private void SetPullingAnim(bool pulling)
    {
        if (playerAnimator == null) return;
        if (!string.IsNullOrEmpty(pullingBool))
            playerAnimator.SetBool(pullingBool, pulling);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = true;

        // IMPORTANT: do NOT auto-interact here
        isInteracting = false;

        playerRoot = other.transform;
        if (playerAnimator == null)
            playerAnimator = other.GetComponentInChildren<Animator>();

        cachedCC = other.GetComponent<CharacterController>();
        if (cachedCC != null)
        {
            ccWasEnabled = cachedCC.enabled;
            // do NOT disable CC on enter anymore — only when interaction starts
        }

        SetUI(true);          // show small "P" hint immediately
        SetPullingAnim(false);
        StopChain();
        StopDoorOpenLoop();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = false;
        isInteracting = false;

        SetUI(false);
        SetPullingAnim(false);

        StopChain();
        StopDoorOpenLoop();

        chainPullAmount = 0f;

        ReleasePlayerLock();
        playerRoot = null;
        cachedCC = null;
    }
}
