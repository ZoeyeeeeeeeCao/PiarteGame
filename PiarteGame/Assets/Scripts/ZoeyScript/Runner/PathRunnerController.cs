using UnityEngine;
using PathCreation;

[RequireComponent(typeof(CharacterController))]
public class PathRunnerSingleLaneController : MonoBehaviour
{
    [Header("Path")]
    public PathCreator pathCreator;
    public EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop;

    [Header("Direction")]
    [Tooltip("If true, start at the end of the path and run backwards (towards 0).")]
    public bool runFromEnd = true;

    [Header("Run")]
    public float forwardSpeed = 8f;
    public float followSharpness = 14f;
    public float rotateSharpness = 18f;

    [Header("Jump & Gravity (Y is driven by gravity, NOT by path Y)")]
    public float gravity = -28f;
    public float jumpHeight = 1.4f;

    [Header("Slide")]
    public float slideDuration = 0.75f;
    public float slideHeight = 1.0f;
    public float standHeight = 1.8f;
    public float heightLerpSpeed = 18f;

    [Header("Swipe (Mouse Drag)")]
    public float swipeMinPixels = 60f;
    public float swipeMaxTime = 0.35f;
    public bool allowKeyboardFallback = true;
    public KeyCode keyJump = KeyCode.Space;
    public KeyCode keySlide = KeyCode.LeftControl;

    [Header("Animation")]
    public Animator animator;                 // drag your Animator here
    public string stairsZoneTag = "StairsZone";
    public string obstacleTag = "Obstacle";

    [Tooltip("Freeze movement briefly when Hit plays.")]
    public float hitLockTime = 0.55f;

    // Animator hashes (faster + safer)
    static readonly int H_IsGrounded = Animator.StringToHash("IsGrounded");
    static readonly int H_IsSliding = Animator.StringToHash("IsSliding");
    static readonly int H_OnStairs = Animator.StringToHash("OnStairs");
    static readonly int H_YVel = Animator.StringToHash("YVel");
    static readonly int H_Jump = Animator.StringToHash("Jump");
    static readonly int H_Hit = Animator.StringToHash("Hit");

    CharacterController cc;

    float distanceTravelled;
    float verticalVel;

    bool sliding;
    float slideTimer;

    Vector2 swipeStartPos;
    float swipeStartTime;
    bool swiping;

    bool onStairs;
    bool hitLocked;
    float hitLockTimer;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // init controller height
        cc.height = standHeight;
        var c = cc.center;
        c.y = cc.height * 0.5f;
        cc.center = c;

        if (pathCreator == null || pathCreator.path == null)
        {
            Debug.LogError("PathCreator not assigned or path is null!");
            enabled = false;
            return;
        }

        distanceTravelled = runFromEnd ? pathCreator.path.length : 0f;

        Vector3 startPos = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        Vector3 startDir = pathCreator.path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
        if (runFromEnd) startDir = -startDir;

        startDir.y = 0f;
        if (startDir.sqrMagnitude < 0.001f) startDir = Vector3.forward;
        startDir.Normalize();

        cc.enabled = false;
        transform.position = new Vector3(startPos.x, transform.position.y + 0.2f, startPos.z);
        transform.rotation = Quaternion.LookRotation(startDir, Vector3.up);
        cc.enabled = true;

        verticalVel = -2f;
        PushAnimParams();
    }

    void Update()
    {
        if (pathCreator == null || pathCreator.path == null)
            return;

        HandleSwipeInput();
        if (allowKeyboardFallback) HandleKeyboardFallback();

        UpdateSlide();
        UpdateHitLock();

        // If hit is playing/locked, don’t advance along path (optional but feels better)
        if (!hitLocked)
        {
            float maxLen = pathCreator.path.length;
            float dir = runFromEnd ? -1f : 1f;

            distanceTravelled += dir * forwardSpeed * Time.deltaTime;
            distanceTravelled = Mathf.Clamp(distanceTravelled, 0f, maxLen);
        }

        Vector3 pathPos = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        Vector3 forward = pathCreator.path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
        if (runFromEnd) forward = -forward;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        forward.Normalize();

        // Horizontal magnet (XZ only)
        Vector3 pos = transform.position;
        Vector3 desiredHorizontal = new Vector3(pathPos.x, pos.y, pathPos.z);
        Vector3 horizontalDelta = desiredHorizontal - pos;

        Vector3 horizontalMove = Vector3.Lerp(
            Vector3.zero,
            horizontalDelta,
            1f - Mathf.Exp(-followSharpness * Time.deltaTime)
        );

        // Gravity + jump
        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = -2f;

        verticalVel += gravity * Time.deltaTime;
        Vector3 verticalMove = Vector3.up * verticalVel * Time.deltaTime;

        // Move (reduce horizontal during hit to avoid jitter)
        Vector3 finalMove = (hitLocked ? horizontalMove * 0.15f : horizontalMove) + verticalMove;
        cc.Move(finalMove);

        // Face forward
        Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            1f - Mathf.Exp(-rotateSharpness * Time.deltaTime)
        );

        // Height blend
        float desiredHeight = sliding ? slideHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, desiredHeight, 1f - Mathf.Exp(-heightLerpSpeed * Time.deltaTime));
        Vector3 center = cc.center;
        center.y = cc.height * 0.5f;
        cc.center = center;

        // Animator parameters every frame
        PushAnimParams();
    }

    void PushAnimParams()
    {
        if (!animator) return;

        animator.SetBool(H_IsGrounded, cc.isGrounded);
        animator.SetBool(H_IsSliding, sliding);
        animator.SetBool(H_OnStairs, onStairs);
        animator.SetFloat(H_YVel, verticalVel);
    }

    // -------------------- INPUT --------------------

    void HandleSwipeInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swiping = true;
            swipeStartPos = Input.mousePosition;
            swipeStartTime = Time.time;
        }

        if (!swiping) return;

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 endPos = Input.mousePosition;
            float dt = Time.time - swipeStartTime;
            Vector2 delta = endPos - swipeStartPos;
            swiping = false;

            if (dt > swipeMaxTime) return;
            if (delta.magnitude < swipeMinPixels) return;

            if (Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
            {
                if (delta.y > 0) DoJump();
                else DoSlide();
            }
        }
    }

    void HandleKeyboardFallback()
    {
        if (Input.GetKeyDown(keyJump)) DoJump();
        if (Input.GetKeyDown(keySlide)) DoSlide();
    }

    // -------------------- ACTIONS --------------------

    void DoJump()
    {
        if (hitLocked) return;
        if (!cc.isGrounded) return;
        if (sliding) return;

        verticalVel = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);

        if (animator) animator.SetTrigger(H_Jump);
    }

    void DoSlide()
    {
        if (hitLocked) return;
        if (sliding) return;
        if (!cc.isGrounded) return;

        sliding = true;
        slideTimer = slideDuration;
        // Slide uses bool IsSliding, so no trigger needed
    }

    void UpdateSlide()
    {
        if (!sliding) return;

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f)
            sliding = false;
    }

    // -------------------- HIT (OBSTACLE FAIL) --------------------

    void TriggerHit()
    {
        if (hitLocked) return;

        hitLocked = true;
        hitLockTimer = hitLockTime;

        // cancel slide if we got hit
        sliding = false;

        if (animator)
        {
            animator.ResetTrigger(H_Jump);
            animator.SetTrigger(H_Hit);
        }
    }

    void UpdateHitLock()
    {
        if (!hitLocked) return;

        hitLockTimer -= Time.deltaTime;
        if (hitLockTimer <= 0f)
            hitLocked = false;
    }

    // Called when CharacterController hits a non-trigger collider
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.collider) return;
        if (!hit.collider.CompareTag(obstacleTag)) return;

        // If you were sliding OR clearly airborne (jumping), don’t count it as a fail.
        bool airborne = !cc.isGrounded && verticalVel > -0.5f;

        if (sliding) return;
        if (airborne) return;

        TriggerHit();
    }

    // -------------------- STAIRS ZONE --------------------

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(stairsZoneTag))
            onStairs = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(stairsZoneTag))
            onStairs = false;
    }
}
