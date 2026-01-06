using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwordCombatController : MonoBehaviour
{
    [Header("Sword References")]
    [SerializeField] private GameObject prefabSword;
    [SerializeField] private SkinnedMeshRenderer bakedSwordRenderer;
    [SerializeField] private Transform handSocket;
    [SerializeField] private Transform beltSocket;
    [SerializeField] private WeaponTrailEffect weaponTrail;

    [Header("Sword Transform Adjustments")]
    [SerializeField] private Vector3 handPositionOffset = new Vector3(0.021f, -0.043f, 0.026f);
    [SerializeField] private Vector3 handRotationOffset = new Vector3(-15.551f, 107.252f, 54.941f);
    [SerializeField] private Vector3 beltPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 beltRotationOffset = Vector3.zero;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private int swordMaskLayerIndex = 1;

    [Header("Movement Integration")]
    [SerializeField] private float layerTransitionSpeed = 0.2f;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float comboWindow = 0.6f;
    [SerializeField] private int hardAttackRequirement = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private Transform attackPoint;

    [Header("Queue System")]
    [Tooltip("Maximum attacks that can be queued (1 = only queue next attack)")]
    [SerializeField] private int maxQueueSize = 1;
    [Tooltip("Clear queue if no input within this time")]
    [SerializeField] private float queueTimeout = 1.5f;

    [Header("Combo Timing")]
    [Tooltip("When combo window opens (0.75 = 75% into animation)")]
    [SerializeField] private float comboWindowStart = 0.75f;
    [Tooltip("Small delay before executing queued combo attack")]
    [SerializeField] private float comboExecutionDelay = 0.05f;

    [Header("Trail Settings")]
    [SerializeField] private float trailStartDelay = 0.1f;
    [SerializeField] private float trailDuration = 0.3f;
    [SerializeField] private float hardAttackTrailDuration = 0.5f;

    [Header("Attack Animation Names")]
    [SerializeField] private string[] easyAttackAnimations = new string[] { "EasyAttack1", "EasyAttack2", "EasyAttack3" };
    [SerializeField] private string[] normalAttackAnimations = new string[] { "NormalAttack1", "NormalAttack2", "NormalAttack3" };
    [SerializeField] private string hardAttackAnimation = "HardAttack";

    [Header("Attack Damage")]
    [SerializeField] private int easyAttackDamage = 10;
    [SerializeField] private int normalAttackDamage = 20;
    [SerializeField] private int hardAttackDamage = 50;

    [Header("Attack Durations")]
    [Tooltip("Leave at 0 to auto-detect from animations")]
    [SerializeField] private float easyAttackDuration = 0f;
    [SerializeField] private float normalAttackDuration = 0f;
    [SerializeField] private float hardAttackDuration = 0f;

    [Header("Camera Shake for Hard Attack")]
    [SerializeField] private bool useHardAttackCinematic = true;
    [SerializeField] private float cinematicDuration = 2f;

    // State tracking
    private bool isSwordDrawn = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private bool isDrawingOrSheathing = false;
    private bool inComboWindow = false;

    // Attack tracking
    private int attackCounter = 0;

    // Queue system
    private Queue<AttackInput> attackQueue = new Queue<AttackInput>();
    private float lastInputTime = 0f;
    private Coroutine queueTimeoutCoroutine;

    // Layer weight management
    private Coroutine currentLayerTransition = null;
    private Coroutine currentAttackCoroutine = null; // Track the current attack

    // Animation hashes
    private int drawSwordHash;
    private int sheathSwordHash;
    private int isDrawnHash;
    private int isAttackingHash;
    private int exitAttackStateHash;
    private int[] easyAttackHashes;
    private int[] normalAttackHashes;
    private int hardAttackHash;

    // Attack input structure
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

        Debug.Log("Combat System initialized - Movement will be locked during attacks");
    }

    private void Update()
    {
        HandleInput();
        CleanupQueueTimeout();

        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugAnimatorState();
        }
    }

    private void DebugAnimatorState()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        string currentClip = clipInfo.Length > 0 ? clipInfo[0].clip.name : "None";

        Debug.Log($"📊 Current State: {stateInfo.fullPathHash} | Clip: {currentClip} | IsAttacking Param: {animator.GetBool(isAttackingHash)} | IsAttacking: {isAttacking} | Normalized Time: {stateInfo.normalizedTime:F2}");

        // Check if any triggers are still set
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                Debug.Log($"  Trigger '{param.name}': {animator.GetBool(param.nameHash)}");
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isAttacking && !isDrawingOrSheathing)
        {
            ToggleSword();
        }

        if (isSwordDrawn && !isDrawingOrSheathing)
        {
            if (Input.GetKeyDown(KeyCode.H))
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

        // If not attacking, execute immediately
        if (!isAttacking && canAttack)
        {
            ExecuteAttackFromQueue(type);
            return;
        }

        // If in combo window, queue the attack
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
        else if (!inComboWindow)
        {
            Debug.Log("❌ Not in combo window - attack ignored");
        }
        else
        {
            Debug.Log("❌ Queue full - attack ignored");
        }
    }

    private void ExecuteAttackFromQueue(AttackType type)
    {
        switch (type)
        {
            case AttackType.Easy:
                PerformEasyAttack();
                break;
            case AttackType.Normal:
                PerformNormalAttack();
                break;
            case AttackType.Hard:
                if (attackCounter >= hardAttackRequirement)
                {
                    PerformHardAttack();
                }
                break;
        }
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
            Debug.Log("🧹 Cleaning up stale queue");
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

        float animLength = GetAnimationLength("DrawSword");

        yield return new WaitForSeconds(animLength * 0.25f);

        if (bakedSwordRenderer != null)
        {
            bakedSwordRenderer.enabled = false;
        }

        if (prefabSword != null)
        {
            prefabSword.SetActive(true);
        }

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

        if (weaponTrail != null)
        {
            weaponTrail.StopTrail();
        }

        float animLength = GetAnimationLength("SheathSword");

        yield return new WaitForSeconds(animLength * 0.75f);

        AttachSwordToBelt();

        yield return new WaitForSeconds(animLength * 0.05f);

        if (prefabSword != null)
        {
            prefabSword.SetActive(false);
        }

        if (bakedSwordRenderer != null)
        {
            bakedSwordRenderer.enabled = true;
        }

        yield return new WaitForSeconds(animLength * 0.20f);

        isSwordDrawn = false;
        attackCounter = 0;

        StartSmoothLayerTransition(swordMaskLayerIndex, 0f, layerTransitionSpeed);

        isDrawingOrSheathing = false;
    }

    private void PerformEasyAttack()
    {
        // Safety check: Don't start if already attacking
        if (currentAttackCoroutine != null)
        {
            Debug.LogWarning("⚠️ Attack already in progress, skipping Easy Attack");
            return;
        }

        int randomIndex = Random.Range(0, easyAttackHashes.Length);
        int attackHash = easyAttackHashes[randomIndex];
        string attackName = easyAttackAnimations[randomIndex];

        Debug.Log($"⚔️ Performing Easy Attack: {attackName}");

        float duration = easyAttackDuration > 0 ? easyAttackDuration : GetAnimationLength(attackName);

        currentAttackCoroutine = StartCoroutine(ExecuteAttack(attackHash, attackName, easyAttackDamage, duration, trailDuration));
        attackCounter++;
    }

    private void PerformNormalAttack()
    {
        // Safety check: Don't start if already attacking
        if (currentAttackCoroutine != null)
        {
            Debug.LogWarning("⚠️ Attack already in progress, skipping Normal Attack");
            return;
        }

        int randomIndex = Random.Range(0, normalAttackHashes.Length);
        int attackHash = normalAttackHashes[randomIndex];
        string attackName = normalAttackAnimations[randomIndex];

        Debug.Log($"⚔️ Performing Normal Attack: {attackName}");

        float duration = normalAttackDuration > 0 ? normalAttackDuration : GetAnimationLength(attackName);

        currentAttackCoroutine = StartCoroutine(ExecuteAttack(attackHash, attackName, normalAttackDamage, duration, trailDuration));
        attackCounter++;
    }

    private void PerformHardAttack()
    {
        // Safety check: Don't start if already attacking
        if (currentAttackCoroutine != null)
        {
            Debug.LogWarning("⚠️ Attack already in progress, skipping Hard Attack");
            return;
        }

        Debug.Log("💥 Performing Hard Attack!");

        float duration = hardAttackDuration > 0 ? hardAttackDuration : GetAnimationLength(hardAttackAnimation);

        currentAttackCoroutine = StartCoroutine(ExecuteHardAttack(duration));
        attackCounter = 0;
    }

    private IEnumerator ExecuteAttack(int animationHash, string animationName, int damage, float duration, float customTrailDuration)
    {
        // Ensure we're in attacking state
        isAttacking = true;
        canAttack = false;

        animator.SetBool(isAttackingHash, true);

        // PlayerController will now block all locomotion automatically
        Debug.Log($"🔒 Movement LOCKED - {animationName} started");

        StartSmoothLayerTransition(swordMaskLayerIndex, 0f, 0.1f);

        // CRITICAL: Reset ALL triggers and wait a frame before setting new trigger
        ResetAllAttackTriggers();
        yield return null; // Wait one frame for animator to process the reset

        animator.SetTrigger(animationHash);

        // Wait another frame to ensure trigger is processed
        yield return null;

        // Start trail effect
        yield return new WaitForSeconds(trailStartDelay);

        if (weaponTrail != null)
        {
            weaponTrail.StartTrail();
        }

        // Deal damage at midpoint
        float waitForDamage = (duration * 0.5f) - trailStartDelay;
        yield return new WaitForSeconds(waitForDamage);

        DealDamageToEnemies(damage);

        // Open combo window
        float comboStartTime = duration * comboWindowStart;
        float timeUntilCombo = comboStartTime - (duration * 0.5f);
        yield return new WaitForSeconds(timeUntilCombo);

        OpenComboWindow();

        // Keep trail active
        yield return new WaitForSeconds(customTrailDuration);

        if (weaponTrail != null)
        {
            weaponTrail.StopTrail();
        }

        // Wait for combo window to close
        yield return new WaitForSeconds(comboWindow - customTrailDuration);

        CloseComboWindow();

        bool hasQueuedAttack = attackQueue.Count > 0;

        // Recovery time
        float recoveryTime = hasQueuedAttack ? comboExecutionDelay : attackCooldown;
        yield return new WaitForSeconds(recoveryTime);

        // Only restore state if no queued attack
        if (!hasQueuedAttack)
        {
            animator.SetBool(isAttackingHash, false);
            animator.SetTrigger(exitAttackStateHash);

            if (isSwordDrawn)
            {
                StartSmoothLayerTransition(swordMaskLayerIndex, 1f, 0.15f);
            }

            // IMPORTANT: Clear coroutine reference and set states
            currentAttackCoroutine = null;
            isAttacking = false;
            canAttack = true;

            Debug.Log("🔓 Movement UNLOCKED - Attack complete");
        }
        else
        {
            // Execute queued attack while STAYING in attack state
            AttackInput nextAttack = attackQueue.Dequeue();
            Debug.Log($"✅ Executing queued {nextAttack.type} attack (Remaining: {attackQueue.Count})");

            // Clear current coroutine reference before starting new one
            currentAttackCoroutine = null;

            // Stay in attacking state
            canAttack = true; // Allow the next attack to execute

            // Execute immediately without exiting attack state
            ExecuteAttackFromQueue(nextAttack.type);
        }
    }

    private IEnumerator ExecuteHardAttack(float duration)
    {
        isAttacking = true;
        canAttack = false;

        animator.SetBool(isAttackingHash, true);

        // PlayerController will now block all locomotion automatically
        Debug.Log("🔒 Movement LOCKED - Hard Attack started");

        StartSmoothLayerTransition(swordMaskLayerIndex, 0f, 0.1f);

        // CRITICAL: Reset ALL triggers and wait a frame before setting new trigger
        ResetAllAttackTriggers();
        yield return null; // Wait one frame for animator to process the reset

        if (useHardAttackCinematic)
        {
            StartCoroutine(HardAttackCinematic());
        }

        animator.SetTrigger(hardAttackHash);

        // Wait another frame to ensure trigger is processed
        yield return null;

        yield return new WaitForSeconds(trailStartDelay);

        if (weaponTrail != null)
        {
            weaponTrail.StartTrail();
        }

        float waitForDamage = (duration * 0.5f) - trailStartDelay;
        yield return new WaitForSeconds(waitForDamage);

        DealDamageToEnemies(hardAttackDamage);

        yield return new WaitForSeconds(hardAttackTrailDuration);

        if (weaponTrail != null)
        {
            weaponTrail.StopTrail();
        }

        float remainingTime = duration - (duration * 0.5f) - hardAttackTrailDuration;
        yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(attackCooldown);

        animator.SetBool(isAttackingHash, false);
        animator.SetTrigger(exitAttackStateHash);

        if (isSwordDrawn)
        {
            StartSmoothLayerTransition(swordMaskLayerIndex, 1f, 0.15f);
        }

        // Clear coroutine reference and states
        currentAttackCoroutine = null;
        isAttacking = false;
        canAttack = true;

        attackQueue.Clear();

        Debug.Log("🔓 Movement UNLOCKED - Hard attack complete");
        Debug.Log("💥 Hard attack complete - Attack counter reset");
    }

    private IEnumerator HardAttackCinematic()
    {
        Debug.Log("🎬 Hard Attack Cinematic Effect!");
        yield return new WaitForSeconds(cinematicDuration);
    }

    private void StartSmoothLayerTransition(int layerIndex, float targetWeight, float duration)
    {
        if (currentLayerTransition != null)
        {
            StopCoroutine(currentLayerTransition);
        }
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

    private void DealDamageToEnemies(int damage)
    {
        if (attackPoint == null) return;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
    }

    public void OpenComboWindow()
    {
        inComboWindow = true;
        Debug.Log("🟢 COMBO WINDOW OPEN");
    }

    public void CloseComboWindow()
    {
        inComboWindow = false;
        Debug.Log("🔴 COMBO WINDOW CLOSED");
    }

    public void ResetAllAttackTriggers()
    {
        // Force clear all attack triggers
        foreach (int hash in easyAttackHashes)
        {
            animator.ResetTrigger(hash);
        }
        foreach (int hash in normalAttackHashes)
        {
            animator.ResetTrigger(hash);
        }
        animator.ResetTrigger(hardAttackHash);

        // Force animator to update immediately
        animator.Update(0f);
    }

    public void OnDrawSwordAttach()
    {
        AttachSwordToHand();
    }

    public void OnSheathSwordAttach()
    {
        AttachSwordToBelt();
    }

    public void OnAttackHit()
    {
        Debug.Log("💥 Attack hit frame - called from animation event");
    }

    public void OnAttackComplete()
    {
        Debug.Log("✅ Attack animation complete");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    // Public properties - PlayerController uses IsAttacking to block movement
    public bool IsSwordDrawn => isSwordDrawn;
    public bool IsAttacking => isAttacking;
    public bool CanAttack => canAttack;
    public int QueuedAttacks => attackQueue.Count;
}