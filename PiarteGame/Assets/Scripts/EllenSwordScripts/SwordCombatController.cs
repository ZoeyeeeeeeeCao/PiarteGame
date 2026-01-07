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
    [SerializeField] private int swordMaskLayerIndex = 1; // Index of the Sword Mask layer

    [Header("Movement Integration")]
    [SerializeField] private MonoBehaviour movementController; // Your movement script
    [SerializeField] private float layerTransitionSpeed = 0.2f; // Smooth layer weight transitions

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private int hardAttackRequirement = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private Transform attackPoint;

    [Header("Attack Animation Names")]
    [SerializeField] private string[] easyAttackAnimations = new string[] { "EasyAttack1", "EasyAttack2", "EasyAttack3" };
    [SerializeField] private string[] normalAttackAnimations = new string[] { "NormalAttack1", "NormalAttack2", "NormalAttack3" };
    [SerializeField] private string hardAttackAnimation = "HardAttack";

    [Header("Attack Damage")]
    [SerializeField] private int easyAttackDamage = 10;
    [SerializeField] private int normalAttackDamage = 20;
    [SerializeField] private int hardAttackDamage = 50;

    [Header("Attack Durations")]
    [SerializeField] private float easyAttackDuration = 0.4f;
    [SerializeField] private float normalAttackDuration = 0.6f;
    [SerializeField] private float hardAttackDuration = 1.5f;

    [Header("Camera Shake for Hard Attack")]
    [SerializeField] private bool useHardAttackCinematic = true;
    [SerializeField] private float cinematicDuration = 2f;

    // State tracking
    private bool isSwordDrawn = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private bool isDrawingOrSheathing = false;

    // Attack tracking
    private int attackCounter = 0;

    // Animation hashes
    private int drawSwordHash;
    private int sheathSwordHash;
    private int isDrawnHash;
    private int[] easyAttackHashes;
    private int[] normalAttackHashes;
    private int hardAttackHash;

    // Movement control interface
    private IMovementController movementInterface;

    private void Start()
    {
        InitializeAnimationHashes();
        InitializeMovementController();
        InitializeSwordState();
    }

    private void InitializeAnimationHashes()
    {
        drawSwordHash = Animator.StringToHash("DrawSword");
        sheathSwordHash = Animator.StringToHash("SheathSword");
        isDrawnHash = Animator.StringToHash("IsDrawn");

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

    private void InitializeMovementController()
    {
        if (movementController != null)
        {
            movementInterface = movementController as IMovementController;
            if (movementInterface == null)
            {
                Debug.LogWarning("Movement controller doesn't implement IMovementController interface. Movement blocking during attacks won't work.");
            }
        }
    }

    private void InitializeSwordState()
    {
        // Disable sword mask layer initially (sword not drawn)
        if (animator != null)
        {
            animator.SetLayerWeight(swordMaskLayerIndex, 0f);
            animator.SetBool(isDrawnHash, false);
            Debug.Log($"Sword Mask Layer initialized with weight: {animator.GetLayerWeight(swordMaskLayerIndex)}");
        }

        // Position the prefab sword at belt initially
        if (prefabSword != null && beltSocket != null)
        {
            AttachSwordToBelt();
        }

        // Show baked sword, hide prefab sword
        if (bakedSwordRenderer != null)
        {
            bakedSwordRenderer.enabled = true;
        }

        if (prefabSword != null)
        {
            prefabSword.SetActive(false);
        }

        // Disable trail at start
        if (weaponTrail != null)
        {
            weaponTrail.enabled = false;
        }

        Debug.Log("Sword Combat Controller initialized");
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Toggle sword draw/sheath (E key)
        if (Input.GetKeyDown(KeyCode.E) && !isAttacking && !isDrawingOrSheathing)
        {
            ToggleSword();
        }

        // Attack inputs (only when sword is drawn)
        if (isSwordDrawn && canAttack && !isAttacking && !isDrawingOrSheathing)
        {
            // Left Mouse Button - Easy Attack
            if (Input.GetMouseButtonDown(0))
            {
                PerformEasyAttack();
            }
            // Right Mouse Button - Normal Attack
            else if (Input.GetMouseButtonDown(1))
            {
                PerformNormalAttack();
            }
            // Hard Attack - Both buttons or H key
            else if ((Input.GetMouseButton(0) && Input.GetMouseButtonDown(1)) ||
                     (Input.GetMouseButton(1) && Input.GetMouseButtonDown(0)) ||
                     Input.GetKeyDown(KeyCode.H))
            {
                if (attackCounter >= hardAttackRequirement)
                {
                    PerformHardAttack();
                }
                else
                {
                    Debug.Log($"Hard attack locked! Need {hardAttackRequirement - attackCounter} more attacks");
                }
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

    // ========================================
    // DRAW SWORD - Can be done while moving
    // ========================================
    private IEnumerator DrawSword()
    {
        isDrawingOrSheathing = true;

        // IMMEDIATELY enable sword mask layer
        // This allows upper body to draw while legs continue locomotion
        StartCoroutine(TransitionLayerWeight(swordMaskLayerIndex, 1f, layerTransitionSpeed));

        // Trigger draw animation on BOTH layers
        animator.SetTrigger(drawSwordHash);
        animator.SetBool(isDrawnHash, true);

        // Get animation length
        float animLength = GetAnimationLength("DrawSword");

        // Wait for hand to reach sword position (25% into animation)
        yield return new WaitForSeconds(animLength * 0.25f);

        // Switch from baked to prefab sword
        if (bakedSwordRenderer != null)
        {
            bakedSwordRenderer.enabled = false;
        }

        if (prefabSword != null)
        {
            prefabSword.SetActive(true);
        }

        AttachSwordToHand();

        // Wait for rest of draw animation
        yield return new WaitForSeconds(animLength * 0.75f);

        isSwordDrawn = true;
        isDrawingOrSheathing = false;

        // Enable weapon trail
        if (weaponTrail != null)
        {
            weaponTrail.enabled = true;
        }

        Debug.Log("Sword drawn - ready for combat");
    }

    // ========================================
    // SHEATH SWORD - Can be done while moving
    // ========================================
    private IEnumerator SheathSword()
    {
        isDrawingOrSheathing = true;

        // Layer weight stays at 1 during sheath
        // This allows upper body to sheath while legs continue locomotion

        // Trigger sheath animation on BOTH layers
        animator.SetTrigger(sheathSwordHash);
        animator.SetBool(isDrawnHash, false);

        // Disable weapon trail
        if (weaponTrail != null)
        {
            weaponTrail.enabled = false;
        }

        // Get animation length
        float animLength = GetAnimationLength("SheathSword");

        // Wait until sword reaches belt (75% into animation)
        yield return new WaitForSeconds(animLength * 0.75f);

        AttachSwordToBelt();

        yield return new WaitForSeconds(animLength * 0.05f);

        // Switch from prefab to baked sword
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

        // NOW disable sword mask layer after sheath completes
        StartCoroutine(TransitionLayerWeight(swordMaskLayerIndex, 0f, layerTransitionSpeed));

        isDrawingOrSheathing = false;

        Debug.Log("Sword sheathed");
    }

    // ========================================
    // ATTACKS - Full body, stops movement
    // ========================================
    private void PerformEasyAttack()
    {
        int randomIndex = Random.Range(0, easyAttackHashes.Length);
        int attackHash = easyAttackHashes[randomIndex];

        Debug.Log($"Performing Easy Attack: {easyAttackAnimations[randomIndex]}");

        StartCoroutine(ExecuteAttack(attackHash, easyAttackDamage, easyAttackDuration));
        attackCounter++;
    }

    private void PerformNormalAttack()
    {
        int randomIndex = Random.Range(0, normalAttackHashes.Length);
        int attackHash = normalAttackHashes[randomIndex];

        Debug.Log($"Performing Normal Attack: {normalAttackAnimations[randomIndex]}");

        StartCoroutine(ExecuteAttack(attackHash, normalAttackDamage, normalAttackDuration));
        attackCounter++;
    }

    private void PerformHardAttack()
    {
        Debug.Log("Performing Hard Attack!");

        StartCoroutine(ExecuteHardAttack());
        attackCounter = 0;
    }

    private IEnumerator ExecuteAttack(int animationHash, int damage, float duration)
    {
        isAttacking = true;
        canAttack = false;

        // STOP MOVEMENT - Dynasty Warriors style
        if (movementInterface != null)
        {
            movementInterface.SetMovementEnabled(false);
        }

        // DISABLE sword mask layer for full body attack
        animator.SetLayerWeight(swordMaskLayerIndex, 0f);
        Debug.Log($"Attack Started - Layer weight: {animator.GetLayerWeight(swordMaskLayerIndex)}");

        // Trigger attack animation
        animator.SetTrigger(animationHash);

        // Wait for attack to execute (damage frame at 50% of animation)
        yield return new WaitForSeconds(duration * 0.5f);

        // Deal damage at the peak of the swing
        DealDamageToEnemies(damage);

        // Weapon trail effect
        if (weaponTrail != null)
        {
            weaponTrail.StartTrail();
            yield return new WaitForSeconds(0.3f);
            weaponTrail.StopTrail();
        }

        // Wait for rest of animation
        yield return new WaitForSeconds(duration * 0.5f);

        // Cooldown before next attack
        yield return new WaitForSeconds(attackCooldown);

        // RE-ENABLE sword mask layer if sword is still drawn
        if (isSwordDrawn)
        {
            StartCoroutine(TransitionLayerWeight(swordMaskLayerIndex, 1f, 0.15f));
        }

        // RE-ENABLE MOVEMENT
        if (movementInterface != null)
        {
            movementInterface.SetMovementEnabled(true);
        }

        isAttacking = false;
        canAttack = true;

        Debug.Log($"Attack counter: {attackCounter}/{hardAttackRequirement}");
    }

    private IEnumerator ExecuteHardAttack()
    {
        isAttacking = true;
        canAttack = false;

        // STOP MOVEMENT
        if (movementInterface != null)
        {
            movementInterface.SetMovementEnabled(false);
        }

        // DISABLE sword mask layer for full body attack
        animator.SetLayerWeight(swordMaskLayerIndex, 0f);
        Debug.Log($"Hard Attack Started - Layer weight: {animator.GetLayerWeight(swordMaskLayerIndex)}");

        // Cinematic effect
        if (useHardAttackCinematic)
        {
            StartCoroutine(HardAttackCinematic());
        }

        // Trigger hard attack animation
        animator.SetTrigger(hardAttackHash);

        // Wait for attack to execute
        yield return new WaitForSeconds(hardAttackDuration * 0.5f);

        // Deal massive damage
        DealDamageToEnemies(hardAttackDamage);

        // Extended weapon trail
        if (weaponTrail != null)
        {
            weaponTrail.StartTrail();
            yield return new WaitForSeconds(0.5f);
            weaponTrail.StopTrail();
        }

        // Wait for rest of animation
        yield return new WaitForSeconds(hardAttackDuration * 0.5f);

        // Cooldown
        yield return new WaitForSeconds(attackCooldown);

        // RE-ENABLE sword mask layer if sword is still drawn
        if (isSwordDrawn)
        {
            StartCoroutine(TransitionLayerWeight(swordMaskLayerIndex, 1f, 0.15f));
        }

        // RE-ENABLE MOVEMENT
        if (movementInterface != null)
        {
            movementInterface.SetMovementEnabled(true);
        }

        isAttacking = false;
        canAttack = true;

        Debug.Log("Hard attack complete - Attack counter reset to 0");
    }

    private IEnumerator HardAttackCinematic()
    {
        Debug.Log("Hard Attack Cinematic Effect!");
        // TODO: Add camera shake, slow motion, screen effects here
        // Example:
        // Time.timeScale = 0.3f; // Slow motion
        // CameraShake.Shake(0.5f, 0.3f);
        yield return new WaitForSeconds(cinematicDuration);
        // Time.timeScale = 1f;
    }

    // ========================================
    // HELPER METHODS
    // ========================================
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
    }

    private float GetAnimationLength(string animationName)
    {
        // FIX: Search all clips in the controller instead of checking "current" state.
        // This ensures that even if a transition is happening, we get the correct duration.
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName) return clip.length;
            }
        }

        return 1f; // Default fallback if not found
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
                Debug.Log($"Dealt {damage} damage to {enemy.name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    // ========================================
    // ANIMATION EVENTS (called from animations)
    // ========================================
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
        Debug.Log("Attack hit frame - called from animation event");
    }

    // ========================================
    // PUBLIC GETTERS
    // ========================================
    public bool IsSwordDrawn => isSwordDrawn;
    public bool IsAttacking => isAttacking;
    public bool CanAttack => canAttack;
}

// ========================================
// INTERFACE for movement controllers
// ========================================
public interface IMovementController
{
    void SetMovementEnabled(bool enabled);
}