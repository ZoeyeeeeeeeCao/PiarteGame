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
    public float forwardSpeed = 8f;          // speed along path (m/s)
    public float followSharpness = 14f;      // how strongly we stick to path center (XZ)
    public float rotateSharpness = 18f;      // how fast we face forward along path

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

    CharacterController cc;

    float distanceTravelled;
    float verticalVel;

    bool sliding;
    float slideTimer;

    Vector2 swipeStartPos;
    float swipeStartTime;
    bool swiping;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
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

        // ✅ Key: choose start distance explicitly
        distanceTravelled = runFromEnd ? pathCreator.path.length : 0f;

        // Place player to the chosen start point (XZ), Y stays as current + small lift to avoid clipping
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

        verticalVel = -2f; // stable grounded behaviour
    }

    void Update()
    {
        if (pathCreator == null || pathCreator.path == null)
            return;

        HandleSwipeInput();
        if (allowKeyboardFallback) HandleKeyboardFallback();

        UpdateSlide();

        float maxLen = pathCreator.path.length;
        float dir = runFromEnd ? -1f : 1f;

        // 1) advance along path (forwardSpeed always positive in Inspector)
        distanceTravelled += dir * forwardSpeed * Time.deltaTime;

        // clamp to path range
        distanceTravelled = Mathf.Clamp(distanceTravelled, 0f, maxLen);

        // 2) sample path at current distance
        Vector3 pathPos = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        Vector3 forward = pathCreator.path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);

        // if running from end, invert forward so we face our moving direction
        if (runFromEnd) forward = -forward;

        // IMPORTANT: only use path for XZ (do NOT follow path Y)
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        forward.Normalize();

        // 3) horizontal "magnet" to the path centerline (XZ only)
        Vector3 pos = transform.position;

        Vector3 desiredHorizontal = new Vector3(pathPos.x, pos.y, pathPos.z);
        Vector3 horizontalDelta = desiredHorizontal - pos;

        Vector3 horizontalMove = Vector3.Lerp(
            Vector3.zero,
            horizontalDelta,
            1f - Mathf.Exp(-followSharpness * Time.deltaTime)
        );

        // 4) gravity + jump (Y ONLY comes from physics/gravity)
        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = -2f;

        verticalVel += gravity * Time.deltaTime;
        Vector3 verticalMove = Vector3.up * verticalVel * Time.deltaTime;

        // 5) move
        cc.Move(horizontalMove + verticalMove);

        // 6) face forward (XZ)
        Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            1f - Mathf.Exp(-rotateSharpness * Time.deltaTime)
        );

        // 7) height blend (slide/stand)
        float desiredHeight = sliding ? slideHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, desiredHeight, 1f - Mathf.Exp(-heightLerpSpeed * Time.deltaTime));
        Vector3 center = cc.center;
        center.y = cc.height * 0.5f;
        cc.center = center;
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

            // only Up/Down now
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
        if (!cc.isGrounded) return;
        if (sliding) return;

        verticalVel = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
    }

    void DoSlide()
    {
        if (sliding) return;
        if (!cc.isGrounded) return;

        sliding = true;
        slideTimer = slideDuration;
    }

    void UpdateSlide()
    {
        if (!sliding) return;

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f)
            sliding = false;
    }
}
