using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwordCombatController : MonoBehaviour
{
    [Header("Sword References")]
    [SerializeField] private GameObject prefabSword; // The sword prefab that moves to hand (NO SkinnedMeshRenderer)
    [SerializeField] private SkinnedMeshRenderer bakedSwordRenderer; // The baked sword with SkinnedMeshRenderer (stuck to pelvis)
    [SerializeField] private Transform handSocket; // Right hand bone
    [SerializeField] private Transform beltSocket; // Hip/belt position
    [SerializeField] private WeaponTrailEffect weaponTrail;

    [Header("Sword Transform Adjustments")]
    [SerializeField] private Vector3 handPositionOffset = new Vector3(0.021f, -0.043f, 0.026f);
    [SerializeField] private Vector3 handRotationOffset = new Vector3(-15.551f, 107.252f, 54.941f);
    [SerializeField] private Vector3 beltPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 beltRotationOffset = Vector3.zero;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Combat Settings")]
    [SerializeField] private float comboResetTime = 1f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private int hardAttackRequirement = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private Transform attackPoint;

    [Header("Camera Shake for Hard Attack")]
    [SerializeField] private bool useHardAttackCinematic = true;
    [SerializeField] private float cinematicDuration = 2f;

    [Header("VFX")]
    [SerializeField] private GameObject normalAttackVFX;
    [SerializeField] private GameObject mediumAttackVFX;
    [SerializeField] private GameObject hardAttackVFX;

    // State tracking
    private bool isSwordDrawn = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private float lastAttackTime = 0f;

    // Combo tracking
    private List<AttackInput> currentCombo = new List<AttackInput>();
    private float lastComboInputTime = 0f;
    private int totalAttackCount = 0;

    // Animation hashes
    private int drawSwordHash;
    private int sheathSwordHash;
    private int normalAttack1Hash;
    private int normalAttack2Hash;
    private int mediumAttack1Hash;
    private int mediumAttack2Hash;
    private int mediumAttack3Hash;
    private int hardAttackHash;
    private int isDrawnHash;
    private int attackTriggerHash;

    private enum AttackInput
    {
        Left,
        Right
    }

    private enum AttackType
    {
        Normal,
        Medium,
        Hard
    }

    private void Start()
    {
        InitializeAnimationHashes();

        // Position the prefab sword at belt initially
        if (prefabSword != null && beltSocket != null)
        {
            AttachSwordToBelt();
        }

        // Check if prefab sword has SkinnedMeshRenderer (it shouldn't!)
        if (prefabSword != null)
        {
            SkinnedMeshRenderer prefabSkinned = prefabSword.GetComponentInChildren<SkinnedMeshRenderer>();
            if (prefabSkinned != null)
            {
                Debug.LogError("WARNING: Prefab sword has a SkinnedMeshRenderer! This will cause issues. Please use a MeshRenderer instead or remove the SkinnedMeshRenderer component!");
            }
            else
            {
                Debug.Log("Prefab sword correctly has NO SkinnedMeshRenderer - using MeshRenderer");
            }
        }

        // Show baked sword (with SkinnedMeshRenderer - stuck to pelvis)
        if (bakedSwordRenderer != null)
        {
            bakedSwordRenderer.enabled = true;
        }

        // Hide prefab sword
        if (prefabSword != null)
        {
            prefabSword.SetActive(false);
        }

        // Disable trail at start
        if (weaponTrail != null)
        {
            weaponTrail.enabled = false;
        }

        Debug.Log("Sword initialized - Baked sword visible, prefab hidden");
    }

    private void InitializeAnimationHashes()
    {
        drawSwordHash = Animator.StringToHash("DrawSword");
        sheathSwordHash = Animator.StringToHash("SheathSword");
        normalAttack1Hash = Animator.StringToHash("NormalAttack1");
        normalAttack2Hash = Animator.StringToHash("NormalAttack2");
        mediumAttack1Hash = Animator.StringToHash("MediumAttack1");
        mediumAttack2Hash = Animator.StringToHash("MediumAttack2");
        mediumAttack3Hash = Animator.StringToHash("MediumAttack3");
        hardAttackHash = Animator.StringToHash("HardAttack");
        isDrawnHash = Animator.StringToHash("IsDrawn");
        attackTriggerHash = Animator.StringToHash("Attack");
    }

    private void Update()
    {
        HandleInput();
        CheckComboReset();
    }

    private void HandleInput()
    {
        // Toggle sword draw/sheath (E key)
        if (Input.GetKeyDown(KeyCode.E) && !isAttacking)
        {
            ToggleSword();
        }

        // Attack inputs (only when sword is drawn)
        if (isSwordDrawn && canAttack && !isAttacking)
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                AddComboInput(AttackInput.Left);
            }
            else if (Input.GetMouseButtonDown(1)) // Right click
            {
                AddComboInput(AttackInput.Right);
            }
        }
    }

    private void ToggleSword()
    {
        if (isSwordDrawn)
        {
            Debug.Log("Sheathing sword...");
            StartCoroutine(SheathSword());
        }
        else
        {
            Debug.Log("Drawing sword...");
            StartCoroutine(DrawSword());
        }
    }

    private IEnumerator DrawSword()
    {
        Debug.Log("=== DRAW SWORD START ===");
        Debug.Log($"Prefab Sword null? {prefabSword == null}");
        Debug.Log($"Baked Sword Renderer null? {bakedSwordRenderer == null}");
        Debug.Log($"Hand Socket null? {handSocket == null}");

        animator.SetTrigger(drawSwordHash);
        animator.SetBool(isDrawnHash, true);

        // Get the animation length dynamically
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        float animLength = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1f;

        // Move sword to hand at 20% of animation (earlier timing)
        yield return new WaitForSeconds(animLength * 0.2f);

        Debug.Log("--- 20% animation point reached ---");

        // Hide baked sword (SkinnedMeshRenderer)
        if (bakedSwordRenderer != null)
        {
            Debug.Log($"Disabling baked sword renderer (was: {bakedSwordRenderer.enabled})");
            bakedSwordRenderer.enabled = false;
            Debug.Log($"Baked sword renderer now: {bakedSwordRenderer.enabled}");
        }
        else
        {
            Debug.LogWarning("Baked sword renderer is NULL!");
        }

        // Show prefab sword and move to hand
        if (prefabSword != null)
        {
            Debug.Log($"Activating prefab sword (was: {prefabSword.activeSelf})");
            prefabSword.SetActive(true);
            Debug.Log($"Prefab sword now active: {prefabSword.activeSelf}");
        }
        else
        {
            Debug.LogWarning("Prefab sword is NULL!");
        }

        AttachSwordToHand();

        Debug.Log("=== Sword drawn - Baked disabled, prefab enabled in hand ===");

        // Wait for rest of animation
        yield return new WaitForSeconds(animLength * 0.8f);

        isSwordDrawn = true;

        // Enable trail
        if (weaponTrail != null)
        {
            weaponTrail.enabled = true;
        }
    }

    private IEnumerator SheathSword()
    {
        animator.SetTrigger(sheathSwordHash);
        animator.SetBool(isDrawnHash, false);

        // Disable trail
        if (weaponTrail != null)
        {
            weaponTrail.enabled = false;
        }

        // Get the animation length dynamically
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        float animLength = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1f;

        // Move sword to belt at 80% of animation (much later)
        yield return new WaitForSeconds(animLength * 0.8f);

        // Move it back to belt position
        AttachSwordToBelt();

        // Wait a tiny bit more before swapping (5% more)
        yield return new WaitForSeconds(animLength * 0.05f);

        // Hide prefab sword
        if (prefabSword != null)
        {
            prefabSword.SetActive(false);
        }

        // Show baked sword (SkinnedMeshRenderer)
        if (bakedSwordRenderer != null)
        {
            bakedSwordRenderer.enabled = true;
        }

        Debug.Log("Sword sheathed - Prefab disabled, baked enabled at belt");

        // Wait for rest of animation
        yield return new WaitForSeconds(animLength * 0.15f);

        isSwordDrawn = false;

        // Reset combo when sheathing
        ResetCombo();
    }

    private void AttachSwordToHand()
    {
        if (prefabSword != null && handSocket != null)
        {
            Debug.Log($"Attaching prefab to hand socket: {handSocket.name}");
            Debug.Log($"Prefab position BEFORE: {prefabSword.transform.position}");

            prefabSword.transform.SetParent(handSocket);
            prefabSword.transform.localPosition = handPositionOffset;
            prefabSword.transform.localRotation = Quaternion.Euler(handRotationOffset);

            Debug.Log($"Prefab position AFTER: {prefabSword.transform.position}");
            Debug.Log($"Prefab local position: {prefabSword.transform.localPosition}");
            Debug.Log($"Hand offset used - Pos: {handPositionOffset}, Rot: {handRotationOffset}");
        }
        else
        {
            Debug.LogWarning($"Cannot attach sword! PrefabSword null: {prefabSword == null}, HandSocket null: {handSocket == null}");
        }
    }

    private void AttachSwordToBelt()
    {
        if (prefabSword != null && beltSocket != null)
        {
            prefabSword.transform.SetParent(beltSocket);
            prefabSword.transform.localPosition = beltPositionOffset;
            prefabSword.transform.localRotation = Quaternion.Euler(beltRotationOffset);

            Debug.Log($"Prefab sword attached to belt - Pos: {beltPositionOffset}, Rot: {beltRotationOffset}");
        }
    }

    private void AddComboInput(AttackInput input)
    {
        currentCombo.Add(input);
        lastComboInputTime = Time.time;
        totalAttackCount++;

        EvaluateCombo();
    }

    private void EvaluateCombo()
    {
        int comboLength = currentCombo.Count;

        // Check for hard attack (4-5 hits)
        if (comboLength >= 4 && totalAttackCount >= hardAttackRequirement)
        {
            PerformAttack(AttackType.Hard);
        }
        // Check for medium attack (3 hits)
        else if (comboLength == 3)
        {
            PerformAttack(AttackType.Medium);
        }
        // Check for normal attack (1-2 hits)
        else if (comboLength >= 1 && comboLength <= 2)
        {
            if (comboLength == 2 || Time.time - lastComboInputTime > 0.3f)
            {
                PerformAttack(AttackType.Normal);
            }
        }
    }

    private void PerformAttack(AttackType attackType)
    {
        StartCoroutine(ExecuteAttack(attackType));
    }

    private IEnumerator ExecuteAttack(AttackType attackType)
    {
        isAttacking = true;
        canAttack = false;

        int damage = 0;
        string animationName = "";
        GameObject vfx = null;

        switch (attackType)
        {
            case AttackType.Normal:
                damage = 10;
                animationName = (currentCombo.Count == 1) ? "NormalAttack1" : "NormalAttack2";
                animator.SetTrigger(animationName);
                vfx = normalAttackVFX;
                yield return new WaitForSeconds(0.5f);
                break;

            case AttackType.Medium:
                damage = 25;
                int mediumVariant = GetMediumAttackVariant();
                animationName = $"MediumAttack{mediumVariant}";
                animator.SetTrigger(animationName);
                vfx = mediumAttackVFX;
                yield return new WaitForSeconds(0.8f);
                break;

            case AttackType.Hard:
                damage = 40;

                if (useHardAttackCinematic)
                {
                    StartCoroutine(HardAttackCinematic());
                }

                animator.SetTrigger(hardAttackHash);
                vfx = hardAttackVFX;
                yield return new WaitForSeconds(1.5f);

                totalAttackCount = 0;
                break;
        }

        DealDamageToEnemies(damage);

        if (vfx != null && attackPoint != null)
        {
            Instantiate(vfx, attackPoint.position, attackPoint.rotation);
        }

        if (weaponTrail != null)
        {
            weaponTrail.StartTrail();
            yield return new WaitForSeconds(0.3f);
            weaponTrail.StopTrail();
        }

        ResetCombo();

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
        canAttack = true;
    }

    private int GetMediumAttackVariant()
    {
        if (currentCombo[0] == AttackInput.Left &&
            currentCombo[1] == AttackInput.Left &&
            currentCombo[2] == AttackInput.Left)
        {
            return 1;
        }
        else if (currentCombo[0] == AttackInput.Right &&
                 currentCombo[1] == AttackInput.Right &&
                 currentCombo[2] == AttackInput.Right)
        {
            return 2;
        }
        else
        {
            return 3;
        }
    }

    private IEnumerator HardAttackCinematic()
    {
        Debug.Log("Hard Attack Cinematic Effect!");
        yield return new WaitForSeconds(cinematicDuration);
    }

    private void DealDamageToEnemies(int damage)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                Debug.Log($"Dealt {damage} damage to {enemy.name}");
            }
        }
    }

    private void CheckComboReset()
    {
        if (currentCombo.Count > 0 && Time.time - lastComboInputTime > comboResetTime)
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        currentCombo.Clear();
        lastComboInputTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    public void OnDrawSwordAttach()
    {
        AttachSwordToHand();
        Debug.Log("Animation event: Sword attached to hand");
    }

    public void OnSheathSwordAttach()
    {
        AttachSwordToBelt();
        Debug.Log("Animation event: Sword attached to belt");
    }

    public void OnDrawSwordComplete()
    {
        Debug.Log("Draw sword animation complete");
    }

    public void OnSheathSwordComplete()
    {
        Debug.Log("Sheath sword animation complete");
    }

    public void OnAttackHit()
    {
        Debug.Log("Attack hit frame");
    }
}