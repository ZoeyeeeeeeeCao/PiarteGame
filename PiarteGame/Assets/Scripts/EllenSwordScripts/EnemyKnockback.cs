using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private float normalKnockbackForce = 3f;
    [SerializeField] private float hardKnockbackForce = 10f;
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Hard Attack Spread")]
    [SerializeField] private float hardAttackSpreadAngle = 15f; // Degrees to spread enemies apart

    private Rigidbody rb;
    private bool isBeingKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector3 knockbackVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void FixedUpdate()
    {
        if (isBeingKnockedBack)
        {
            knockbackTimer += Time.fixedDeltaTime;
            float progress = knockbackTimer / knockbackDuration;

            if (progress >= 1f)
            {
                isBeingKnockedBack = false;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
            else
            {
                float curveValue = knockbackCurve.Evaluate(progress);
                Vector3 frameVelocity = knockbackVelocity * curveValue;
                rb.linearVelocity = new Vector3(frameVelocity.x, rb.linearVelocity.y, frameVelocity.z);
            }
        }
    }

    public void ApplyNormalKnockback(Vector3 attackerPosition)
    {
        Vector3 direction = (transform.position - attackerPosition).normalized;
        direction.y = 0; // Keep knockback horizontal

        ApplyKnockback(direction, normalKnockbackForce, false);

        Debug.Log($"💥 Normal knockback applied to {gameObject.name}");
    }

    public void ApplyHardKnockback(Vector3 attackerPosition, Vector3 attackerForward)
    {
        // Calculate base direction from attacker to enemy
        Vector3 directionToEnemy = (transform.position - attackerPosition).normalized;
        directionToEnemy.y = 0;

        // Calculate angle between attacker's forward and direction to enemy
        float angleToEnemy = Vector3.SignedAngle(attackerForward, directionToEnemy, Vector3.up);

        // Add spread angle to push enemies apart in a semicircle
        float spreadModifier = Mathf.Sign(angleToEnemy) * hardAttackSpreadAngle;
        Quaternion spreadRotation = Quaternion.Euler(0, spreadModifier, 0);
        Vector3 spreadDirection = spreadRotation * directionToEnemy;

        ApplyKnockback(spreadDirection, hardKnockbackForce, true);

        Debug.Log($"💥💥 HARD knockback applied to {gameObject.name} with spread!");
    }

    private void ApplyKnockback(Vector3 direction, float force, bool isHardAttack)
    {
        if (rb == null) return;

        // Reset any existing knockback
        isBeingKnockedBack = true;
        knockbackTimer = 0f;

        // Calculate knockback velocity
        knockbackVelocity = direction * force;

        // Add slight upward force for hard attacks
        if (isHardAttack)
        {
            rb.AddForce(Vector3.up * (force * 0.5f), ForceMode.Impulse);
        }
    }

    // Public method to check if currently being knocked back
    public bool IsBeingKnockedBack()
    {
        return isBeingKnockedBack;
    }
}