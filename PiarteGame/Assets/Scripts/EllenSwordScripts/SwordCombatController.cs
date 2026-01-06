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

    [Header("Sword Collider - NEW")]
    [SerializeField] private Collider swordCollider; // Assign the sword's trigger collider here
    [SerializeField] private bool debugCollisions = true;

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

    [Header("Collision Window Timing - NEW")]
    [Tooltip("When collision detection starts (0.4 = 40% into animation)")]
    [SerializeField] private float collisionWindowStart = 0.4f;
    [Tooltip("When collision detection ends (0.7 = 70% into animation)")]
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
    [Tooltip("Leave at 0 to auto-detect from animations")]
    [SerializeField] private float easyAttackDuration = 0f;
    [SerializeField] private float normalAttackDuration = 0f;
    [SerializeField] private float hardAttackDuration = 0f;

    [Header("Hit VFX Settings")]
    [SerializeField] private bool useHitVFX = true;

    [Header("Contact VFX (Along Sword Blade)")]
    [Tooltip("VFX that spawns along the sword blade where it contacts enemy")]
    [SerializeField] private GameObject[] enemyContactVFXPrefabs;
    [SerializeField] private GameObject[] generalContactVFXPrefabs;
    [SerializeField] private float contactVFXLifetime = 2f;
    [SerializeField] private int vfxSpawnCount = 3;
    [SerializeField] private bool scaleVFXToSwordLength = true;
    [SerializeField] private float vfxSizeMultiplier = 1f;

    [Header("Sword Transform Points (For VFX)")]
    [Tooltip("Same transforms used by WeaponTrailEffect - tip and bottom of blade")]
    [SerializeField] private Transform swordTipTransform;
    [SerializeField] private Transform swordBottomTransform;

    [Header("Impact Wave VFX (Around Player)")]
    [Tooltip("Large VFX that spawns around player and affects area")]
    [SerializeField] private GameObject[] enemyImpactWaveVFXPrefabs;
    [SerializeField] private GameObject[] generalImpactWaveVFXPrefabs;
    [SerializeField] private float impactWaveVFXLifetime = 3f;
    [SerializeField] private Vector3 impactWaveOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] private bool spawnImpactWaveOnEveryHit = true;

    [Header("Camera Shake Settings")]
    [SerializeField] private bool useCameraShakeOnHit = true;
    [SerializeField] private float hitShakeDuration = 0.15f;
    [SerializeField] private float hitShakeMagnitude = 0.08f;
    [SerializeField] private float hitShakeRotation = 1.5f;

    [Header("Hard Attack Cinematic")]
    [SerializeField] private bool useHardAttackCinematic = true;
    [SerializeField] private float cinematicDuration = 2f;

    // State tracking
    private bool isSwordDrawn = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private bool isDrawingOrSheathing = false;
    private bool inComboWindow = false;

    // NEW: Collision tracking
    private bool isCollisionActive = false;
    private int currentAttackDamage = 0;
    private HashSet<Collider> hitEnemiesThisAttack = new HashSet<Collider>();

    // Attack tracking
    private int attackCounter = 0;

    // Queue system
    private Queue<AttackInput> attackQueue = new Queue<AttackInput>();
    private float lastInputTime = 0f;
    private Coroutine queueTimeoutCoroutine;

    // Layer weight management
    private Coroutine currentLayerTransition = null;
    private Coroutine currentAttackCoroutine = null;

    // Animation hashes
    private int drawSwordHash;
    private int sheathSwordHash;
    private int isDrawnHash;
    private int isAttackingHash;
    private int exitAttackStateHash;
    private int[] easyAttackHashes;
    private int[] normalAttackHashes;
    private int hardAttackHash;

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
        InitializeSwordCollider();
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

        Debug.Log("Combat System initialized - Collision-based damage system ready");
    }

    private void InitializeSwordCollider()
    {
        if (swordCollider == null)
        {
            Debug.LogError("❌ SWORD COLLIDER NOT ASSIGNED! Please assign the sword's collider in the inspector.");
            Debug.LogError("   1. Select your sword prefab");
            Debug.LogError("   2. Add a Capsule/Box Collider along the blade");
            Debug.LogError("   3. Check 'Is Trigger'");
            Debug.LogError("   4. Assign it to 'Sword Collider' field");
            return;
        }

        // Make sure it's a trigger
        if (!swordCollider.isTrigger)
        {
            swordCollider.isTrigger = true;
            Debug.LogWarning("⚠️ Sword collider was not set as trigger. Fixed automatically.");
        }

        // Disable collision by default
        swordCollider.enabled = false;
        Debug.Log("✅ Sword collider initialized and disabled (will activate during attacks)");
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

    // NEW: Handle sword collisions
    private void OnTriggerEnter(Collider other)
    {
        // This should be on the SWORD object, not the player
        // If you add this script to the player, you need to handle it differently
        // Better to put this logic here and call it from a separate SwordCollider script
        HandleSwordCollision(other);
    }

    private void HandleSwordCollision(Collider other)
    {
        if (!isCollisionActive)
        {
            if (debugCollisions)
                Debug.Log($"🚫 Collision ignored - not in damage window: {other.gameObject.name}");
            return;
        }

        // Check layer
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
        {
            if (debugCollisions)
                Debug.Log($"🚫 Collision ignored - wrong layer: {other.gameObject.name}");
            return;
        }

        // Check if already hit
        if (hitEnemiesThisAttack.Contains(other))
        {
            if (debugCollisions)
                Debug.Log($"🚫 Already hit: {other.gameObject.name}");
            return;
        }

        // Mark as hit
        hitEnemiesThisAttack.Add(other);

        Debug.Log($"⚔️ SWORD HIT: {other.gameObject.name}!");

        // Calculate hit data
        Vector3 hitPosition = other.ClosestPoint(swordCollider.transform.position);
        Vector3 hitDirection = (hitPosition - swordCollider.transform.position).normalized;
        bool isEnemy = other.CompareTag("Enemy");

        // Deal damage
        if (isEnemy)
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(currentAttackDamage);
                Debug.Log($"💔 Dealt {currentAttackDamage} damage to {other.gameObject.name}");

                // Camera shake
                if (useCameraShakeOnHit && CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(hitShakeDuration, hitShakeMagnitude, hitShakeRotation);
                    Debug.Log($"📹 Camera shake triggered!");
                }
            }
            else
            {
                Debug.LogError($"❌ {other.gameObject.name} has 'Enemy' tag but no EnemyHealth component!");
            }
        }

        // Spawn VFX
        if (useHitVFX)
        {
            if (isEnemy && enemyContactVFXPrefabs != null && enemyContactVFXPrefabs.Length > 0)
            {
                SpawnContactVFXAlongBlade(enemyContactVFXPrefabs, hitPosition, hitDirection);
                SpawnImpactWaveVFX(enemyImpactWaveVFXPrefabs);
            }
            else if (!isEnemy && generalContactVFXPrefabs != null && generalContactVFXPrefabs.Length > 0)
            {
                SpawnContactVFXAlongBlade(generalContactVFXPrefabs, hitPosition, hitDirection);
                SpawnImpactWaveVFX(generalImpactWaveVFXPrefabs);
            }
        }
    }

    private void EnableSwordCollision(int damage)
    {
        if (swordCollider == null)
        {
            Debug.LogError("❌ Cannot enable sword collision - collider not assigned!");
            return;
        }

        isCollisionActive = true;
        currentAttackDamage = damage;
        hitEnemiesThisAttack.Clear();
        swordCollider.enabled = true;

        Debug.Log($"⚔️ COLLISION ACTIVE - Damage: {damage}");
    }

    private void DisableSwordCollision()
    {
        if (swordCollider == null) return;

        isCollisionActive = false;
        currentAttackDamage = 0;
        swordCollider.enabled = false;
        hitEnemiesThisAttack.Clear();

        Debug.Log($"🛡️ COLLISION DISABLED");
    }

    private void DebugAnimatorState()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        string currentClip = clipInfo.Length > 0 ? clipInfo[0].clip.name : "None";

        Debug.Log($"📊 Current State: {stateInfo.fullPathHash} | Clip: {currentClip} | IsAttacking Param: {animator.GetBool(isAttackingHash)} | IsAttacking: {isAttacking} | Normalized Time: {stateInfo.normalizedTime:F2}");
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

        // Make sure collision is disabled
        DisableSwordCollision();

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
        isAttacking = true;
        canAttack = false;

        animator.SetBool(isAttackingHash, true);

        Debug.Log($"🔒 Movement LOCKED - {animationName} started");

        StartSmoothLayerTransition(swordMaskLayerIndex, 0f, 0.1f);

        ResetAllAttackTriggers();
        yield return null;

        animator.SetTrigger(animationHash);

        yield return null;

        // Start trail
        yield return new WaitForSeconds(trailStartDelay);

        if (weaponTrail != null)
        {
            weaponTrail.StartTrail();
        }

        // Enable collision at the right time (WHEN SWORD ACTUALLY SWINGS)
        float collisionStartTime = duration * collisionWindowStart;
        yield return new WaitForSeconds(collisionStartTime - trailStartDelay);

        EnableSwordCollision(damage);
        Debug.Log($"⚔️ Collision window OPENED at {collisionWindowStart * 100}% of animation");

        // Keep collision active during swing
        float collisionDuration = duration * (collisionWindowEnd - collisionWindowStart);
        yield return new WaitForSeconds(collisionDuration);

        DisableSwordCollision();
        Debug.Log($"🛡️ Collision window CLOSED at {collisionWindowEnd * 100}% of animation");

        // Continue animation timing
        float comboStartTime = duration * comboWindowStart;
        float timeUntilCombo = comboStartTime - (duration * collisionWindowEnd);
        yield return new WaitForSeconds(timeUntilCombo);

        OpenComboWindow();

        yield return new WaitForSeconds(customTrailDuration);

        if (weaponTrail != null)
        {
            weaponTrail.StopTrail();
        }

        yield return new WaitForSeconds(comboWindow - customTrailDuration);

        CloseComboWindow();

        bool hasQueuedAttack = attackQueue.Count > 0;

        float recoveryTime = hasQueuedAttack ? comboExecutionDelay : attackCooldown;
        yield return new WaitForSeconds(recoveryTime);

        if (!hasQueuedAttack)
        {
            animator.SetBool(isAttackingHash, false);
            animator.SetTrigger(exitAttackStateHash);

            if (isSwordDrawn)
            {
                StartSmoothLayerTransition(swordMaskLayerIndex, 1f, 0.15f);
            }

            currentAttackCoroutine = null;
            isAttacking = false;
            canAttack = true;

            Debug.Log("🔓 Movement UNLOCKED - Attack complete");
        }
        else
        {
            AttackInput nextAttack = attackQueue.Dequeue();
            Debug.Log($"✅ Executing queued {nextAttack.type} attack (Remaining: {attackQueue.Count})");

            currentAttackCoroutine = null;

            canAttack = true;

            ExecuteAttackFromQueue(nextAttack.type);
        }
    }

    private IEnumerator ExecuteHardAttack(float duration)
    {
        isAttacking = true;
        canAttack = false;

        animator.SetBool(isAttackingHash, true);

        Debug.Log("🔒 Movement LOCKED - Hard Attack started");

        StartSmoothLayerTransition(swordMaskLayerIndex, 0f, 0.1f);

        ResetAllAttackTriggers();
        yield return null;

        if (useHardAttackCinematic && HardAttackCinematic.Instance != null)
        {
            HardAttackCinematic.Instance.PlayHardAttackCinematic();
        }

        animator.SetTrigger(hardAttackHash);

        yield return null;

        yield return new WaitForSeconds(trailStartDelay);

        if (weaponTrail != null)
        {
            weaponTrail.StartTrail();
        }

        // Enable collision for hard attack
        float collisionStartTime = duration * collisionWindowStart;
        yield return new WaitForSeconds(collisionStartTime - trailStartDelay);

        EnableSwordCollision(hardAttackDamage);
        Debug.Log($"💥 Hard attack collision window OPENED");

        float collisionDuration = duration * (collisionWindowEnd - collisionWindowStart);
        yield return new WaitForSeconds(collisionDuration);

        DisableSwordCollision();
        Debug.Log($"💥 Hard attack collision window CLOSED");

        yield return new WaitForSeconds(hardAttackTrailDuration);

        if (weaponTrail != null)
        {
            weaponTrail.StopTrail();
        }

        float remainingTime = duration - (duration * collisionWindowEnd) - hardAttackTrailDuration;
        yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(attackCooldown);

        animator.SetBool(isAttackingHash, false);
        animator.SetTrigger(exitAttackStateHash);

        if (isSwordDrawn)
        {
            StartSmoothLayerTransition(swordMaskLayerIndex, 1f, 0.15f);
        }

        currentAttackCoroutine = null;
        isAttacking = false;
        canAttack = true;

        attackQueue.Clear();

        Debug.Log("🔓 Movement UNLOCKED - Hard attack complete");
        Debug.Log("💥 Hard attack complete - Attack counter reset");
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

    private void SpawnContactVFXAlongBlade(GameObject[] vfxArray, Vector3 hitPosition, Vector3 hitDirection)
    {
        if (vfxArray == null || vfxArray.Length == 0) return;

        if (swordTipTransform == null || swordBottomTransform == null)
        {
            Debug.LogWarning("Sword transform points not assigned! Falling back to single VFX at hit point.");
            SpawnSingleContactVFX(vfxArray, hitPosition, hitDirection);
            return;
        }

        Vector3 tipPos = swordTipTransform.position;
        Vector3 bottomPos = swordBottomTransform.position;
        Vector3 bladeDirection = (tipPos - bottomPos).normalized;
        float bladeLength = Vector3.Distance(tipPos, bottomPos);

        float vfxScale = scaleVFXToSwordLength ? (bladeLength / vfxSpawnCount) * vfxSizeMultiplier : vfxSizeMultiplier;

        int randomIndex = Random.Range(0, vfxArray.Length);
        GameObject vfxPrefab = vfxArray[randomIndex];

        if (vfxPrefab == null) return;

        for (int i = 0; i < vfxSpawnCount; i++)
        {
            float t = vfxSpawnCount > 1 ? (float)i / (vfxSpawnCount - 1) : 0.5f;
            Vector3 spawnPosition = Vector3.Lerp(bottomPos, tipPos, t);

            Quaternion vfxRotation = Quaternion.LookRotation(-hitDirection, bladeDirection);

            GameObject vfx = Instantiate(vfxPrefab, spawnPosition, vfxRotation);
            vfx.transform.localScale = Vector3.one * vfxScale;
            Destroy(vfx, contactVFXLifetime);

            Debug.Log($"💥 Spawned contact VFX {i + 1}/{vfxSpawnCount} at blade position: {t:F2}");
        }
    }

    private void SpawnSingleContactVFX(GameObject[] vfxArray, Vector3 hitPosition, Vector3 hitDirection)
    {
        int randomIndex = Random.Range(0, vfxArray.Length);
        GameObject vfxPrefab = vfxArray[randomIndex];

        if (vfxPrefab != null)
        {
            Quaternion vfxRotation = Quaternion.LookRotation(-hitDirection);
            GameObject vfx = Instantiate(vfxPrefab, hitPosition, vfxRotation);
            vfx.transform.localScale = Vector3.one * vfxSizeMultiplier;
            Destroy(vfx, contactVFXLifetime);
        }
    }

    private void SpawnImpactWaveVFX(GameObject[] vfxArray)
    {
        if (vfxArray == null || vfxArray.Length == 0) return;

        int randomIndex = Random.Range(0, vfxArray.Length);
        GameObject vfxPrefab = vfxArray[randomIndex];

        if (vfxPrefab != null)
        {
            Vector3 spawnPosition = transform.position + impactWaveOffset;
            Quaternion spawnRotation = Quaternion.Euler(-90, 0, 0);

            GameObject impactVFX = Instantiate(vfxPrefab, spawnPosition, spawnRotation);
            Destroy(impactVFX, impactWaveVFXLifetime);

            Debug.Log($"🌊 Spawned impact wave VFX: {vfxPrefab.name}");
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
        foreach (int hash in easyAttackHashes)
        {
            animator.ResetTrigger(hash);
        }
        foreach (int hash in normalAttackHashes)
        {
            animator.ResetTrigger(hash);
        }
        animator.ResetTrigger(hardAttackHash);

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

    // Public properties
    public bool IsSwordDrawn => isSwordDrawn;
    public bool IsAttacking => isAttacking;
    public bool CanAttack => canAttack;
    public int QueuedAttacks => attackQueue.Count;
}