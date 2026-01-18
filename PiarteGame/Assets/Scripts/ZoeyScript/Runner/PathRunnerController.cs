using UnityEngine;
using PathCreation;  // Path Creator namespace

[RequireComponent(typeof(CharacterController))]
public class PathRunnerController : MonoBehaviour
{
    [Header("Path")]
    public PathCreator pathCreator;
    public EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop;

    [Header("Run")]
    public float forwardSpeed = 8f;          // speed along path (m/s)
    public float followSharpness = 14f;      // how strongly we stick to lane target
    public float rotateSharpness = 18f;      // how fast we face forward along path

    [Header("Lanes")]
    [Tooltip("Distance between lanes (meters).")]
    public float laneWidth = 1.6f;
    [Tooltip("How fast we move to the target lane offset.")]
    public float laneSwitchSpeed = 14f;      // smoothing for lane offset
    [Range(3, 3)] public int laneCount = 3;  // fixed to 3 lanes
    [Tooltip("0=Left, 1=Center, 2=Right")]
    public int startLaneIndex = 1;

    [Header("Jump & Gravity")]
    public float gravity = -28f;
    public float jumpHeight = 1.4f;

    [Header("Slide")]
    public float slideDuration = 0.75f;
    public float slideHeight = 1.0f;         // CharacterController height during slide
    public float standHeight = 1.8f;         // CharacterController height normally
    public float heightLerpSpeed = 18f;

    [Header("Swipe (Mouse Drag)")]
    public float swipeMinPixels = 60f;       // min drag distance to count as swipe
    public float swipeMaxTime = 0.35f;       // must swipe within this time
    public bool allowKeyboardFallback = true;
    public KeyCode keyLeft = KeyCode.A;
    public KeyCode keyRight = KeyCode.D;
    public KeyCode keyJump = KeyCode.Space;
    public KeyCode keySlide = KeyCode.LeftControl;

    CharacterController cc;

    // path distance traveled
    float distanceTravelled;

    // vertical motion
    float verticalVel;

    // lanes
    int laneIndex;                 // 0..2
    float currentLaneOffset;
    float targetLaneOffset;

    // slide
    bool sliding;
    float slideTimer;

    // swipe tracking
    Vector2 swipeStartPos;
    float swipeStartTime;
    bool swiping;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        laneIndex = Mathf.Clamp(startLaneIndex, 0, 2);
        targetLaneOffset = LaneIndexToOffset(laneIndex);
        currentLaneOffset = targetLaneOffset;

        // init controller height
        cc.height = standHeight;
        var c = cc.center;
        c.y = cc.height * 0.5f;
        cc.center = c;

        // optional: start from nearest point on path
        if (pathCreator != null)
        {
            distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        }
    }

    void Update()
    {
        if (pathCreator == null || pathCreator.path == null)
            return;

        HandleSwipeInput();
        if (allowKeyboardFallback) HandleKeyboardFallback();

        UpdateSlide();

        // 1) advance along path
        distanceTravelled += forwardSpeed * Time.deltaTime;

        // 2) sample path
        Vector3 centerPos = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        Vector3 forward = pathCreator.path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);

        // keep things horizontal for running (if your path has slopes and you want slopes, tell me to upgrade)
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // 3) lane offset smoothing
        targetLaneOffset = LaneIndexToOffset(laneIndex);
        currentLaneOffset = Mathf.Lerp(
            currentLaneOffset,
            targetLaneOffset,
            1f - Mathf.Exp(-laneSwitchSpeed * Time.deltaTime)
        );

        Vector3 targetPos = centerPos + right * currentLaneOffset;

        // 4) stick to targetPos on XZ plane
        Vector3 pos = transform.position;
        Vector3 desiredHorizontal = new Vector3(targetPos.x, pos.y, targetPos.z);
        Vector3 horizontalDelta = desiredHorizontal - pos;

        Vector3 horizontalMove = Vector3.Lerp(
            Vector3.zero,
            horizontalDelta,
            1f - Mathf.Exp(-followSharpness * Time.deltaTime)
        );

        // 5) gravity + jump
        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = -2f;

        verticalVel += gravity * Time.deltaTime;
        Vector3 verticalMove = Vector3.up * verticalVel * Time.deltaTime;

        // 6) move
        cc.Move(horizontalMove + verticalMove);

        // 7) face forward
        Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            1f - Mathf.Exp(-rotateSharpness * Time.deltaTime)
        );

        // 8) height blend (slide/stand)
        float desiredHeight = sliding ? slideHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, desiredHeight, 1f - Mathf.Exp(-heightLerpSpeed * Time.deltaTime));
        Vector3 center = cc.center;
        center.y = cc.height * 0.5f;
        cc.center = center;
    }

    // -------------------- INPUT --------------------

    void HandleSwipeInput()
    {
        // Mouse swipe: click + drag
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

            // timing constraint (feels more like mobile)
            if (dt > swipeMaxTime) return;

            if (delta.magnitude < swipeMinPixels) return;

            // decide direction
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // horizontal
                if (delta.x > 0) SwitchLane(+1);
                else SwitchLane(-1);
            }
            else
            {
                // vertical
                if (delta.y > 0) DoJump();
                else DoSlide();
            }
        }
    }

    void HandleKeyboardFallback()
    {
        if (Input.GetKeyDown(keyLeft)) SwitchLane(-1);
        if (Input.GetKeyDown(keyRight)) SwitchLane(+1);
        if (Input.GetKeyDown(keyJump)) DoJump();
        if (Input.GetKeyDown(keySlide)) DoSlide();
    }

    // -------------------- ACTIONS --------------------

    void SwitchLane(int dir)
    {
        laneIndex = Mathf.Clamp(laneIndex + dir, 0, 2);
    }

    void DoJump()
    {
        if (!cc.isGrounded) return;
        if (sliding) return;

        // v = sqrt(2 * |g| * h)
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
        {
            sliding = false;
        }
    }

    float LaneIndexToOffset(int idx)
    {
        // idx: 0,1,2 -> offsets: -laneWidth, 0, +laneWidth
        return (idx - 1) * laneWidth;
    }
}
