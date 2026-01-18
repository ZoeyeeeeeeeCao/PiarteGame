using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FS_ThirdPerson;

public class SwordCombatController : MonoBehaviour
{
    [Header("Sword References")]
    [SerializeField] private GameObject prefabSword;
    [SerializeField] private SkinnedMeshRenderer bakedSwordRenderer;
    [SerializeField] private Transform handSocket;
    [SerializeField] private Transform beltSocket;
    [SerializeField] private WeaponTrailEffect weaponTrail;
    [SerializeField] private SwordCollisionHandler swordCollisionHandler;

    [Header("Sword Transform Adjustments")]
    [SerializeField] private Vector3 handPositionOffset = new Vector3(0.021f, -0.043f, 0.026f);
    [SerializeField] private Vector3 handRotationOffset = new Vector3(-15.551f, 107.252f, 54.941f);
    [SerializeField] private Vector3 beltPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 beltRotationOffset = Vector3.zero;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private int swordMaskLayerIndex = 1;
    [SerializeField] private float layerTransitionSpeed = 0.1f;

    [Header("Combat Settings - FLUID COMBO")]
    [SerializeField] private float attackCooldown = 0.05f;
    [SerializeField] private float comboWindow = 0.4f;
    [SerializeField] private int hardAttackRequirement = 10;
    [SerializeField] private bool rotateTowardsCameraOnAttack = true;
    [SerializeField] private float rotationSpeed = 1080f;
    [SerializeField] private float animationSpeedMultiplier = 1.3f;

    [Header("Movement During Attack")]
    [Tooltip("Allow player to move during attacks")]
    [SerializeField] private bool allowMovementDuringAttack = true;
    [Tooltip("Speed multiplier when moving during attacks (0.6 = 60% speed)")]
    [Range(0f, 1f)]
    [SerializeField] private float attackMovementSpeedMultiplier = 0.6f;

    [Header("Camera Rotation During Attack")]
    [SerializeField] private bool enableCameraRotationDuringAttack = true;
    [SerializeField] private float attackRotationSpeed = 720f;
    [SerializeField] private bool updateAttackDirectionWithCamera = true;

    [Header("Queue System")]
    [SerializeField] private int maxQueueSize = 1;
    [SerializeField] private float queueTimeout = 1.0f;

    [Header("Combo Timing - FASTER")]
    [SerializeField] private float comboWindowStart = 0.6f;
    [SerializeField] private float comboExecutionDelay = 0.02f;

    [Header("Trail Settings")]
    [SerializeField] private float trailStartDelay = 0.05f;
    [SerializeField] private float trailDuration = 0.25f;
    [SerializeField] private float hardAttackTrailDuration = 0.4f;

    [Header("Collision Window Timing")]
    [Tooltip("When collision detection starts (normalized time 0-1)")]
    [SerializeField] private float collisionWindowStart = 0.3f;
    [Tooltip("When collision detection ends (normalized time 0-1)")]
    [SerializeField] private float collisionWindowEnd = 0.7f;

    [Header("Attack Animation Names")]
    [SerializeField] private string[] easyAttackAnimations = new string[] { "EasyAttack1", "EasyAttack2", "EasyAttack3" };
    [SerializeField] private string[] normalAttackAnimations = new string[] { "NormalAttack1", "NormalAttack2", "NormalAttack3" };
    [SerializeField] private string hardAttackAnimation = "HardAttack";

    [Header("Attack Damage")]
    [SerializeField] private int easyAttackDamage = 10;
    [SerializeField] private int normalAttackDamage = 20;
    [SerializeField] private int hardAttackDamage = 50;

    [Header("Attack Durations")]
    [SerializeField] private float easyAttackDuration = 0f;
    [SerializeField] private float normalAttackDuration = 0f;
    [SerializeField] private float hardAttackDuration = 0f;

    [Header("Hard Attack Cinematic")]
    [SerializeField] private bool useHardAttackCinematic = true;

    [Header("Swing VFX (Always Plays)")]
    [Tooltip("VFX that plays on every swing, triggered by Animation Events")]
    [SerializeField] private GameObject[] swingVFXPrefabs;
    [SerializeField] private Transform swingVFXSpawnPoint;
    [SerializeField] private float swingVFXLifetime = 1f;

    [Header("Audio")]
    [SerializeField] private SwordAudioManager audioManager;

    // State tracking
    private bool isSwordDrawn = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private bool isDrawingOrSheathing = false;
    private bool inComboWindow = false;
    private bool isRotatingDuringAttack = false;

    // Attack tracking
    private int attackCounter = 0;

    // Queue system
    private Queue<AttackInput> attackQueue = new Queue<AttackInput>();
    private float lastInputTime = 0f;
    private Coroutine queueTimeoutCoroutine;
    private Coroutine currentLayerTransition = null;
    private Coroutine currentAttackCoroutine = null;
    private Coroutine cameraRotationCoroutine = null;

    // Animation hashes
    private int drawSwordHash;
    private int sheathSwordHash;
    private int isDrawnHash;
    private int isAttackingHash;
    private int exitAttackStateHash;
    private int[] easyAttackHashes;
    private int[] normalAttackHashes;
    private int hardAttackHash;

    // References
    private PlayerController playerController;
    private CameraController cameraController;

    private struct AttackInput
    {
        public AttackType type;
        public float timestamp;

        public AttackInput(AttackType type, float timestamp)
        {
            this.type = type;
            this.timestamp = timestamp;
        }
    }

    private enum AttackType
    {
        Easy,
        Normal,
        Hard
    }

    private void Start()
    {
        InitializeAnimationHashes();
        InitializeSwordState();

        playerController = GetComponent<PlayerController>();
        cameraController = Camera.main?.GetComponent<CameraController>();

        if (playerController == null)
        {
            Debug.LogError("❌ PlayerController not found on this GameObject!");
        }

        if (cameraController == null)
        {
            Debug.LogError("❌ CameraController not found on main camera!");
        }

        if (swordCollisionHandler == null)
        {
            Debug.LogError("❌ SwordCollisionHandler not assigned! Please assign it in the inspector.");
        }

        if (swingVFXSpawnPoint == null)
        {
            Debug.LogWarning("⚠️ Swing VFX Spawn Point not assigned. Swing VFX won't appear. Assign sword tip transform.");
        }

        if (audioManager == null)
        {
            audioManager = GetComponent<SwordAudioManager>();
            if (audioManager == null)
            {
                if (prefabSword != null)
                {
                    audioManager = prefabSword.GetComponent<SwordAudioManager>();
                }

                if (audioManager == null)
                {
                    Debug.LogWarning("⚠️ SwordAudioManager not found. Audio will not play. Add SwordAudioManager component.");
                }
            }
        }

        if (animator != null)
        {
            animator.speed = animationSpeedMultiplier;
        }
    }

    private void InitializeAnimationHashes()
    {
        drawSwordHash = Animator.StringToHash("DrawSword");
        sheathSwordHash = Animator.StringToHash("SheathSword");
        isDrawnHash = Animator.StringToHash("IsDrawn");
        isAttackingHash = Animator.StringToHash("IsAttacking");
        exitAttackStateHash = Animator.StringToHash("ExitAttackState");

        easyAttackHashes = new int[easyAttackAnimations.Length];
        for (int i = 0; i < easyAttackAnimations.Length; i++)
        {
            easyAttackHashes[i] = Animator.StringToHash(easyAttackAnimations[i]);
        }

        normalAttackHashes = new int[normalAttackAnimations.Length];
        for (int i = 0; i < normalAttackAnimations.Length; i++)
        {
            normalAttackHashes[i] = Animator.StringToHash(normalAttackAnimations[i]);
        }

        hardAttackHash = Animator.StringToHash(hardAttackAnimation);
    }

    private void InitializeSwordState()
    {
        if (animator != null)
        {
            animator.SetLayerWeight(swordMaskLayerIndex, 0f);
            animator.SetBool(isDrawnHash, false);
            animator.SetBool(isAttackingHash, false);
        }

        if (prefabSword != null && beltSocket != null)
        {
            AttachSwordToBelt();
        }

        if (bakedSwordRenderer != null)
        {
            bakedSwordRenderer.enabled = true;
        }

        if (prefabSword != null)
        {
            prefabSword.SetActive(false);
        }

        if (weaponTrail != null)
        {
            weaponTrail.StopTrail();
        }

        Debug.Log("Combat System initialized - FLUID COMBAT MODE with CAMERA ROTATION & MOVEMENT");
    }

    private void Update()
    {
        HandleInput();
        CleanupQueueTimeout();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.X) && !isAttacking && !isDrawingOrSheathing)
        {
            ToggleSword();
        }

        if (isSwordDrawn && !isDrawingOrSheathing)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (attackCounter >= hardAttackRequirement)
                {
                    QueueAttack(AttackType.Hard);
                }
                else
                {
                    Debug.Log($"Hard attack locked! Need {hardAttackRequirement - attackCounter} more attacks.");
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                QueueAttack(AttackType.Easy);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                QueueAttack(AttackType.Normal);
            }
        }
    }

    private void QueueAttack(AttackType type)
    {
        lastInputTime = Time.time;

        if (!isAttacking && canAttack)
        {
            ExecuteAttackFromQueue(type);
            return;
        }

        if (inComboWindow && attackQueue.Count < maxQueueSize)
        {
            attackQueue.Enqueue(new AttackInput(type, Time.time));
            Debug.Log($"⏳ Queued {type} attack (Queue size: {attackQueue.Count})");

            if (queueTimeoutCoroutine != null)
            {
                StopCoroutine(queueTimeoutCoroutine);
            }
            queueTimeoutCoroutine = StartCoroutine(QueueTimeoutCheck());
        }
    }

    private void ExecuteAttackFromQueue(AttackType type)
    {
        switch (type)
        {
            case AttackType.Easy:
                PerformAttack(easyAttackHashes, easyAttackAnimations, easyAttackDamage, easyAttackDuration, trailDuration, false);
                break;
            case AttackType.Normal:
                PerformAttack(normalAttackHashes, normalAttackAnimations, normalAttackDamage, normalAttackDuration, trailDuration, false);
                break;
            case AttackType.Hard:
                if (attackCounter >= hardAttackRequirement)
                {
                    PerformAttack(new int[] { hardAttackHash }, new string[] { hardAttackAnimation }, hardAttackDamage, hardAttackDuration, hardAttackTrailDuration, true);
                }
                break;
        }
    }

    private void PerformAttack(int[] attackHashes, string[] attackNames, int damage, float durationOverride, float customTrailDuration, bool isHardAttack)
    {
        if (currentAttackCoroutine != null)
        {
            Debug.LogWarning("⚠️ Attack already in progress");
            return;
        }

        int randomIndex = Random.Range(0, attackHashes.Length);
        int attackHash = attackHashes[randomIndex];
        string attackName = attackNames[randomIndex];

        float duration = durationOverride > 0 ? durationOverride : GetAnimationLength(attackName);
        duration /= animationSpeedMultiplier;

        currentAttackCoroutine = StartCoroutine(ExecuteAttack(attackHash, attackName, damage, duration, customTrailDuration, isHardAttack));
    }

    private IEnumerator ExecuteAttack(int animationHash, string animationName, int damage, float duration, float customTrailDuration, bool isHardAttack)
    {
        isAttacking = true;
        canAttack = false;
        animator.SetBool(isAttackingHash, true);

        StartSmoothLayerTransition(swordMaskLayerIndex, 0f, 0.05f);

        if (rotateTowardsCameraOnAttack && cameraController != null)
        {
            Quaternion targetRotation = cameraController.PlanarRotation;
            float rotationTime = 0f;
            float maxRotationTime = 0.1f;

            while (rotationTime < maxRotationTime)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
                rotationTime += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRotation;
        }

        ResetAllAttackTriggers();
        yield return null;

        if (isHardAttack && useHardAttackCinematic && HardAttackCinematic.Instance != null)
        {
            HardAttackCinematic.Instance.PlayHardAttackCinematic();
        }

        animator.SetTrigger(animationHash);
        yield return null;

        if (enableCameraRotationDuringAttack && cameraController != null)
        {
            if (cameraRotationCoroutine != null)
            {
                StopCoroutine(cameraRotationCoroutine);
            }
            cameraRotationCoroutine = StartCoroutine(ContinuousCameraRotation(duration));
        }

        float collisionStartTime = duration * collisionWindowStart;
        float collisionDuration = duration * (collisionWindowEnd - collisionWindowStart);

        Debug.Log($"🎬 Animation: {animationName}, Duration: {duration:F2}s");
        Debug.Log($"⏱️ Collision will be active from {collisionStartTime:F2}s to {(collisionStartTime + collisionDuration):F2}s");

        yield return new WaitForSeconds(trailStartDelay);
        if (weaponTrail != null) weaponTrail.StartTrail();

        float soundDelay = duration * 0.35f;
        yield return new WaitForSeconds(soundDelay - trailStartDelay);

        if (audioManager != null)
        {
            audioManager.PlaySwingSound(isHardAttack);
        }

        float waitUntilCollision = collisionStartTime - trailStartDelay - soundDelay;
        if (waitUntilCollision > 0)
        {
            yield return new WaitForSeconds(waitUntilCollision);
        }

        if (swordCollisionHandler != null)
        {
            swordCollisionHandler.EnableCollision(damage, isHardAttack);
        }

        yield return new WaitForSeconds(collisionDuration);

        if (swordCollisionHandler != null)
        {
            swordCollisionHandler.DisableCollision();
        }

        float comboStartTime = duration * comboWindowStart;
        float timeUntilCombo = comboStartTime - (collisionStartTime + collisionDuration);
        if (timeUntilCombo > 0)
        {
            yield return new WaitForSeconds(timeUntilCombo);
        }

        OpenComboWindow();

        yield return new WaitForSeconds(customTrailDuration);
        if (weaponTrail != null) weaponTrail.StopTrail();

        yield return new WaitForSeconds(comboWindow - customTrailDuration);
        CloseComboWindow();

        if (cameraRotationCoroutine != null)
        {
            StopCoroutine(cameraRotationCoroutine);
            cameraRotationCoroutine = null;
        }

        bool hasQueuedAttack = attackQueue.Count > 0;
        float recoveryTime = hasQueuedAttack ? comboExecutionDelay : attackCooldown;
        yield return new WaitForSeconds(recoveryTime);

        if (isHardAttack)
        {
            ResetAttackCounter();
            attackQueue.Clear();
        }

        if (!hasQueuedAttack)
        {
            animator.SetBool(isAttackingHash, false);
            animator.SetTrigger(exitAttackStateHash);

            if (isSwordDrawn)
            {
                StartSmoothLayerTransition(swordMaskLayerIndex, 1f, 0.1f);
            }

            currentAttackCoroutine = null;
            isAttacking = false;
            canAttack = true;
        }
        else
        {
            AttackInput nextAttack = attackQueue.Dequeue();
            currentAttackCoroutine = null;
            canAttack = true;
            ExecuteAttackFromQueue(nextAttack.type);
        }
    }

    private IEnumerator ContinuousCameraRotation(float duration)
    {
        isRotatingDuringAttack = true;
        float elapsed = 0f;

        Debug.Log("🎥 Started continuous camera rotation during attack");

        while (elapsed < duration && cameraController != null)
        {
            Quaternion targetRotation = cameraController.PlanarRotation;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                attackRotationSpeed * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        isRotatingDuringAttack = false;
        cameraRotationCoroutine = null;
        Debug.Log("🎥 Stopped camera rotation");
    }

    private IEnumerator QueueTimeoutCheck()
    {
        yield return new WaitForSeconds(queueTimeout);
        if (Time.time - lastInputTime >= queueTimeout)
        {
            if (attackQueue.Count > 0)
            {
                Debug.Log($"⏱️ Queue timeout - clearing {attackQueue.Count} queued attacks");
                attackQueue.Clear();
            }
        }
    }

    private void CleanupQueueTimeout()
    {
        if (!isAttacking && attackQueue.Count > 0 && Time.time - lastInputTime >= queueTimeout)
        {
            attackQueue.Clear();
        }
    }

    private void ToggleSword()
    {
        if (isSwordDrawn)
        {
            StartCoroutine(SheathSword());
        }
        else
        {
            StartCoroutine(DrawSword());
        }
    }

    private IEnumerator DrawSword()
    {
        isDrawingOrSheathing = true;
        StartSmoothLayerTransition(swordMaskLayerIndex, 1f, layerTransitionSpeed);

        animator.SetTrigger(drawSwordHash);
        animator.SetBool(isDrawnHash, true);

        if (audioManager != null)
        {
            audioManager.PlayDrawSound();
        }

        float animLength = GetAnimationLength("DrawSword");
        yield return new WaitForSeconds(animLength * 0.25f);

        if (bakedSwordRenderer != null) bakedSwordRenderer.enabled = false;
        if (prefabSword != null) prefabSword.SetActive(true);

        AttachSwordToHand();
        yield return new WaitForSeconds(animLength * 0.75f);

        isSwordDrawn = true;
        isDrawingOrSheathing = false;
    }

    private IEnumerator SheathSword()
    {
        isDrawingOrSheathing = true;
        attackQueue.Clear();

        animator.SetTrigger(sheathSwordHash);
        animator.SetBool(isDrawnHash, false);

        if (audioManager != null)
        {
            audioManager.PlaySheathSound();
        }

        if (weaponTrail != null) weaponTrail.StopTrail();
        if (swordCollisionHandler != null) swordCollisionHandler.DisableCollision();

        float animLength = GetAnimationLength("SheathSword");
        yield return new WaitForSeconds(animLength * 0.75f);

        AttachSwordToBelt();
        yield return new WaitForSeconds(animLength * 0.05f);

        if (prefabSword != null) prefabSword.SetActive(false);
        if (bakedSwordRenderer != null) bakedSwordRenderer.enabled = true;

        yield return new WaitForSeconds(animLength * 0.20f);

        isSwordDrawn = false;
        ResetAttackCounter();

        StartSmoothLayerTransition(swordMaskLayerIndex, 0f, layerTransitionSpeed);
        isDrawingOrSheathing = false;
    }

    private void StartSmoothLayerTransition(int layerIndex, float targetWeight, float duration)
    {
        if (currentLayerTransition != null) StopCoroutine(currentLayerTransition);
        currentLayerTransition = StartCoroutine(TransitionLayerWeight(layerIndex, targetWeight, duration));
    }

    private IEnumerator TransitionLayerWeight(int layerIndex, float targetWeight, float duration)
    {
        float startWeight = animator.GetLayerWeight(layerIndex);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newWeight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
            animator.SetLayerWeight(layerIndex, newWeight);
            yield return null;
        }

        animator.SetLayerWeight(layerIndex, targetWeight);
        currentLayerTransition = null;
    }

    private float GetAnimationLength(string animationName)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName) return clip.length;
            }
        }
        return 1f;
    }

    private void AttachSwordToHand()
    {
        if (prefabSword != null && handSocket != null)
        {
            prefabSword.transform.SetParent(handSocket);
            prefabSword.transform.localPosition = handPositionOffset;
            prefabSword.transform.localRotation = Quaternion.Euler(handRotationOffset);
        }
    }

    private void AttachSwordToBelt()
    {
        if (prefabSword != null && beltSocket != null)
        {
            prefabSword.transform.SetParent(beltSocket);
            prefabSword.transform.localPosition = beltPositionOffset;
            prefabSword.transform.localRotation = Quaternion.Euler(beltRotationOffset);
        }
    }

    private void OpenComboWindow()
    {
        inComboWindow = true;
        Debug.Log("🟢 COMBO WINDOW OPEN");
    }

    private void CloseComboWindow()
    {
        inComboWindow = false;
        Debug.Log("🔴 COMBO WINDOW CLOSED");
    }

    public void ResetAllAttackTriggers()
    {
        foreach (int hash in easyAttackHashes) animator.ResetTrigger(hash);
        foreach (int hash in normalAttackHashes) animator.ResetTrigger(hash);
        animator.ResetTrigger(hardAttackHash);
        animator.Update(0f);
    }

    public void SpawnSwingVFX()
    {
        if (swordCollisionHandler != null)
        {
            swordCollisionHandler.CheckForHitAtEvent(easyAttackDamage);
        }
    }

    // Animation events
    public void OnDrawSwordAttach() => AttachSwordToHand();
    public void OnSheathSwordAttach() => AttachSwordToBelt();

    public void IncrementAttackCounter()
    {
        attackCounter++;
        Debug.Log($"⚔️ Attack Counter increased to: {attackCounter}/{hardAttackRequirement}");
    }

    public void ResetAttackCounter()
    {
        attackCounter = 0;
        Debug.Log($"🔄 Attack Counter reset to 0");
    }

    // Public properties for LocomotionController to access
    public bool IsSwordDrawn => isSwordDrawn;
    public bool IsAttacking => isAttacking;
    public bool CanAttack => canAttack;
    public bool IsRotatingDuringAttack => isRotatingDuringAttack;
    public int AttackCounter => attackCounter;
    public int HardAttackRequirement => hardAttackRequirement;

    // ✅ NEW: Movement control properties
    public bool AllowMovementDuringAttack => allowMovementDuringAttack;
    public float AttackMovementSpeedMultiplier => attackMovementSpeedMultiplier;
}