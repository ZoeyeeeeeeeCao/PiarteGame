using Unity.Cinemachine;
using PathCreation;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PathRunnerController : MonoBehaviour
{
    [Header("Path Sequence")]
    public PathCreator path1;
    public PathCreator path2;
    public PathCreator path3;

    [Header("Active Path (runtime)")]
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
    public Animator animator;
    public string stairsZoneTag = "StairsZone";
    public string obstacleTag = "Obstacle";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip runningSound;
    public AudioClip slidingSound;
    [Range(0f, 1f)] public float runningSoundVolume = 0.7f;
    [Range(0f, 1f)] public float slidingSoundVolume = 0.8f;

    [Header("Cinematics")]
    public CinemachineCamera gameplayCam;
    public CinemachineCamera camPath1End;
    public CinemachineCamera camPath2End;
    public float cinematicCamPriority = 20f;
    public float gameplayCamPriority = 10f;

    [Tooltip("Animator Trigger name for surprised reaction at end of Path 1.")]
    public string surprisedTrigger = "Surprised";

    [Tooltip("Rotate the player 180 degrees during the Path1 end cinematic.")]
    public float turnSpeed = 2.0f;

    [Header("Enemies Reveal (end of Path 2)")]
    public GameObject enemiesGroup;
    public float enemiesRevealDuration = 2.0f;

    [Header("Hit (Obstacle Fail)")]
    [Tooltip("Freeze movement briefly when Hit plays.")]
    public float hitLockTime = 0.55f;

    // Animator hashes
    static readonly int H_IsGrounded = Animator.StringToHash("IsGrounded");
    static readonly int H_IsSliding = Animator.StringToHash("IsSliding");
    static readonly int H_OnStairs = Animator.StringToHash("OnStairs");
    static readonly int H_YVel = Animator.StringToHash("YVel");
    static readonly int H_Jump = Animator.StringToHash("Jump");
    static readonly int H_Hit = Animator.StringToHash("Hit");
    static int H_Surprised;

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

    bool cinematicLock;
    int currentPathIndex = 1;

    // Sound state tracking
    bool wasGrounded;
    bool wasSliding;

    // ---------- NEW: speed control across death/respawn ----------
    float _defaultForwardSpeed;
    bool _wasDead;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // If no AudioSource exists, add one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        H_Surprised = Animator.StringToHash(surprisedTrigger);
    }

    void OnEnable()
    {
        // Subscribe to player death event
        if (PlayerHealthController.Instance != null)
        {
            PlayerHealthController.Instance.OnDeath += HandlePlayerDeath;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from player death event
        if (PlayerHealthController.Instance != null)
        {
            PlayerHealthController.Instance.OnDeath -= HandlePlayerDeath;
        }
    }

    void HandlePlayerDeath()
    {
        // NEW: stop forward movement on death
        forwardSpeed = 0f;
        StopSounds();
    }

    void Start()
    {
        // NEW: cache the "normal" speed (8 by default in inspector)
        _defaultForwardSpeed = forwardSpeed;

        // NEW: initialize dead-state tracking
        _wasDead = (PlayerHealthController.Instance != null && PlayerHealthController.Instance.IsDead);

        cc.height = standHeight;
        var c = cc.center;
        c.y = cc.height * 0.5f;
        cc.center = c;

        if (path1 != null)
        {
            pathCreator = path1;
            currentPathIndex = 1;
        }

        if (pathCreator == null || pathCreator.path == null)
        {
            Debug.LogError("PathCreator not assigned or path is null! Assign path1 (recommended).");
            enabled = false;
            return;
        }

        if (enemiesGroup) enemiesGroup.SetActive(false);

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

        SetGameplayCamera();
        PushAnimParams();

        // Initialize sound state
        wasGrounded = cc.isGrounded;
        wasSliding = false;
    }

    void Update()
    {
        if (pathCreator == null || pathCreator.path == null)
            return;

        // ---------------- NEW: Death/Respawn speed control ----------------
        var ph = PlayerHealthController.Instance;
        if (ph != null)
        {
            bool deadNow = ph.IsDead;

            // While dead: force stop
            if (deadNow)
            {
                forwardSpeed = 0f;
            }
            // Just respawned: restore speed
            else if (_wasDead)
            {
                forwardSpeed = _defaultForwardSpeed; // back to 8 (or inspector value)
            }

            _wasDead = deadNow;
        }
        // ------------------------------------------------------------------

        if (!cinematicLock)
        {
            HandleSwipeInput();
            if (allowKeyboardFallback) HandleKeyboardFallback();
        }

        UpdateSlide();
        UpdateHitLock();

        if (!hitLocked && !cinematicLock)
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

        Vector3 pos = transform.position;
        Vector3 desiredHorizontal = new Vector3(pathPos.x, pos.y, pathPos.z);
        Vector3 horizontalDelta = desiredHorizontal - pos;

        Vector3 horizontalMove = Vector3.Lerp(
            Vector3.zero,
            horizontalDelta,
            1f - Mathf.Exp(-followSharpness * Time.deltaTime)
        );

        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = -2f;

        verticalVel += gravity * Time.deltaTime;
        Vector3 verticalMove = Vector3.up * verticalVel * Time.deltaTime;

        float horizMul = (hitLocked || cinematicLock) ? 0.15f : 1f;
        Vector3 finalMove = (horizontalMove * horizMul) + verticalMove;
        cc.Move(finalMove);

        if (!cinematicLock)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                1f - Mathf.Exp(-rotateSharpness * Time.deltaTime)
            );
        }

        float desiredHeight = sliding ? slideHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, desiredHeight, 1f - Mathf.Exp(-heightLerpSpeed * Time.deltaTime));
        Vector3 center = cc.center;
        center.y = cc.height * 0.5f;
        cc.center = center;

        PushAnimParams();
        UpdateSounds();
        CheckPathEnd();
    }

    void PushAnimParams()
    {
        if (!animator) return;

        animator.SetBool(H_IsGrounded, cc.isGrounded);
        animator.SetBool(H_IsSliding, sliding);
        animator.SetBool(H_OnStairs, onStairs);
        animator.SetFloat(H_YVel, verticalVel);
    }

    // -------------------- SOUND MANAGEMENT --------------------

    void UpdateSounds()
    {
        if (!audioSource) return;

        // Check if player is dead - if so, stop all sounds
        if (PlayerHealthController.Instance != null && PlayerHealthController.Instance.IsDead)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            return;
        }

        bool isGrounded = cc.isGrounded;
        bool isSliding = sliding;
        bool isMoving = !hitLocked && !cinematicLock;

        // Determine which sound should be playing
        if (isGrounded && isMoving)
        {
            if (isSliding)
            {
                // Should play sliding sound
                if (!wasSliding || !audioSource.isPlaying || audioSource.clip != slidingSound)
                {
                    PlaySound(slidingSound, slidingSoundVolume);
                }
            }
            else
            {
                // Should play running sound
                if (wasSliding || !audioSource.isPlaying || audioSource.clip != runningSound)
                {
                    PlaySound(runningSound, runningSoundVolume);
                }
            }
        }
        else
        {
            // Not grounded or not moving - stop sounds
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        wasGrounded = isGrounded;
        wasSliding = isSliding;
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (!audioSource || !clip) return;

        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.Play();
    }

    void StopSounds()
    {
        if (audioSource && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // -------------------- PATH END SEQUENCES --------------------

    void CheckPathEnd()
    {
        if (cinematicLock) return;

        float endDist = runFromEnd ? 0f : pathCreator.path.length;

        if (Mathf.Abs(distanceTravelled - endDist) > 0.08f) return;

        if (currentPathIndex == 1)
        {
            if (path2 != null) StartCoroutine(Path1EndSequence());
        }
        else if (currentPathIndex == 2)
        {
            if (path3 != null) StartCoroutine(Path2EndSequence());
        }
    }

    IEnumerator Path1EndSequence()
    {
        cinematicLock = true;
        hitLocked = true;
        StopSounds();

        SetCamera(camPath1End);

        if (animator && !string.IsNullOrEmpty(surprisedTrigger))
            animator.SetTrigger(H_Surprised);

        yield return new WaitForSeconds(0.6f);

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(-transform.forward, Vector3.up);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * turnSpeed;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        SwitchPath(path2);
        currentPathIndex = 2;

        SetGameplayCamera();

        cinematicLock = false;
        hitLocked = false;
    }

    IEnumerator Path2EndSequence()
    {
        cinematicLock = true;
        hitLocked = true;
        StopSounds();

        SetCamera(camPath2End);

        if (enemiesGroup)
            enemiesGroup.SetActive(true);

        yield return new WaitForSeconds(enemiesRevealDuration);

        SwitchPath(path3);
        currentPathIndex = 3;

        SetGameplayCamera();

        cinematicLock = false;
        hitLocked = false;
    }

    void SwitchPath(PathCreator newPath)
    {
        if (newPath == null || newPath.path == null)
        {
            Debug.LogError("SwitchPath failed: newPath is null or has null path.");
            return;
        }

        pathCreator = newPath;

        distanceTravelled = runFromEnd ? pathCreator.path.length : 0f;

        Vector3 p = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        cc.enabled = false;
        transform.position = new Vector3(p.x, transform.position.y, p.z);
        cc.enabled = true;

        Vector3 fwd = pathCreator.path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
        if (runFromEnd) fwd = -fwd;

        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = transform.forward;
        transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
    }

    // -------------------- CINEMACHINE --------------------

    void SetGameplayCamera()
    {
        if (gameplayCam) gameplayCam.Priority = (int)gameplayCamPriority;
        if (camPath1End) camPath1End.Priority = 0;
        if (camPath2End) camPath2End.Priority = 0;
    }

    void SetCamera(CinemachineCamera cam)
    {
        if (gameplayCam) gameplayCam.Priority = 0;
        if (camPath1End) camPath1End.Priority = 0;
        if (camPath2End) camPath2End.Priority = 0;

        if (cam) cam.Priority = (int)cinematicCamPriority;
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
        if (hitLocked || cinematicLock) return;
        if (!cc.isGrounded) return;
        if (sliding) return;

        verticalVel = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);

        if (animator) animator.SetTrigger(H_Jump);
    }

    void DoSlide()
    {
        if (hitLocked || cinematicLock) return;
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

    // -------------------- HIT (OBSTACLE FAIL) --------------------

    void TriggerHit()
    {
        if (hitLocked) return;

        hitLocked = true;
        hitLockTimer = hitLockTime;

        sliding = false;
        StopSounds();

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

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.collider) return;
        if (!hit.collider.CompareTag(obstacleTag)) return;
        if (cinematicLock) return;

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
