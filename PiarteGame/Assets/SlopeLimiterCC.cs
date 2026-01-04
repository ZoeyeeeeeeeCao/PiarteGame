using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SlopeLimiterCC : MonoBehaviour
{
    [Header("Slope Settings")]
    [Range(0f, 89f)]
    public float maxWalkableSlope = 45f;
    public float groundCheckExtra = 0.25f;
    public LayerMask groundMask; // Terrain / Ground ONLY

    [Header("Slide Settings")]
    public bool slideOnSteepSlope = true;
    public float slideSpeed = 4f;
    public float extraDownForce = 2f;

    private CharacterController cc;
    private RaycastHit groundHit;

    public bool OnValidGround { get; private set; }
    public bool OnSteepSlope { get; private set; }
    public float CurrentSlopeAngle { get; private set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        CheckSlope();

        // If standing on a steep slope, force slide / drop
        if (OnSteepSlope)
        {
            Vector3 move = Vector3.down * extraDownForce;

            if (slideOnSteepSlope)
            {
                Vector3 downSlope =
                    Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                move += downSlope * slideSpeed;
            }

            cc.Move(move * Time.deltaTime);
        }
    }

    void CheckSlope()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float rayDistance = (cc.height * 0.5f) + groundCheckExtra;

        if (Physics.Raycast(
                origin,
                Vector3.down,
                out groundHit,
                rayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            CurrentSlopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);

            OnValidGround = CurrentSlopeAngle <= maxWalkableSlope;
            OnSteepSlope = cc.isGrounded && !OnValidGround;
        }
        else
        {
            OnValidGround = false;
            OnSteepSlope = false;
            CurrentSlopeAngle = 0f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = OnSteepSlope ? Color.red : Color.green;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(origin, origin + Vector3.down * 1.5f);
    }
#endif
}
