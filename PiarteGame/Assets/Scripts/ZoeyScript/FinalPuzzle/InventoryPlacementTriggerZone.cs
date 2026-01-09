using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InventoryPlacementTriggerZone : MonoBehaviour
{
    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";
    public GameObject hintUI;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Goal Check (Optional)")]
    [Tooltip("If enabled, compare placed prefab with idealPrefab and play particles when correct.")]
    public bool enableGoalCheck = true;

    [Tooltip("Drag the correct world prefab ASSET here (from Project). Can be empty if no goal check needed.")]
    public GameObject idealPrefab;

    [Header("Success Particles")]
    [Tooltip("Particles to play when correct. Put your PS_SmokeParticle here.")]
    public ParticleSystem[] successParticles;

    [Tooltip("If true, only play once per correct placement (wrong placement resets so it can play again when corrected).")]
    public bool playOnce = true;

    [Header("Particle Initial State")]
    [Tooltip("If true, hide particle GameObjects at start (recommended if you don't want to see them before success).")]
    public bool hideParticlesUntilSuccess = true;

    [Header("Debug")]
    public bool debugCompareLog = false;

    // ✅ NEW: exposed solved state for manager/door
    [HideInInspector]
    public bool IsSolved;

    bool inRange;
    bool successPlayed;
    bool subscribed;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void Start()
    {
        if (hintUI) hintUI.SetActive(false);

        // ✅ Ensure particles are NOT visible/playing at start
        if (hideParticlesUntilSuccess)
        {
            SetParticlesActive(false);
        }
        else
        {
            StopSuccessParticles(clear: true);
        }

        // In case context is created after us
        TrySubscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        // auto-subscribe if missed due to script execution order
        if (!subscribed) TrySubscribe();

        if (!inRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!InventorySelectContext.Instance)
            {
                Debug.LogError($"[{name}] No InventorySelectContext.Instance found in scene!");
                return;
            }

            InventorySelectContext.Instance.BeginSelect(spawnPoint);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = true;
        if (hintUI) hintUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = false;
        if (hintUI) hintUI.SetActive(false);

        if (InventorySelectContext.Instance)
            InventorySelectContext.Instance.EndSelect();
    }

    void TrySubscribe()
    {
        if (subscribed) return;

        var ctx = InventorySelectContext.Instance;
        if (!ctx) return;

        ctx.OnPlacedChanged -= HandlePlacedChanged;
        ctx.OnPlacedChanged += HandlePlacedChanged;

        subscribed = true;

        if (debugCompareLog)
            Debug.Log($"[{name}] ✅ Subscribed to InventorySelectContext.OnPlacedChanged ({ctx.name})");
    }

    void Unsubscribe()
    {
        var ctx = InventorySelectContext.Instance;
        if (!ctx) { subscribed = false; return; }

        ctx.OnPlacedChanged -= HandlePlacedChanged;
        subscribed = false;
    }

    // Called when player places/replaces something via inventory select
    void HandlePlacedChanged(Transform placedPoint, ItemData item, GameObject sourcePrefab, GameObject instance)
    {
        // Only respond if it's placed to THIS zone's spawn point
        if (placedPoint != spawnPoint)
            return;

        // If goal check is off, this zone is just a placement zone
        if (!enableGoalCheck)
            return;

        // Goal check enabled but no ideal prefab assigned => do nothing (safe for other scenes)
        if (!idealPrefab)
            return;

        bool correct = (sourcePrefab == idealPrefab);

        if (debugCompareLog)
        {
            Debug.Log("========== Placement Compare ==========");
            Debug.Log($"[Zone] {name}");
            Debug.Log($"[Ideal Prefab] {idealPrefab.name} | id={idealPrefab.GetInstanceID()}");
            Debug.Log($"[Source Prefab] {(sourcePrefab ? sourcePrefab.name : "NULL")} | id={(sourcePrefab ? sourcePrefab.GetInstanceID() : 0)}");
            Debug.Log($"[COMPARE] correct? {correct}");
            Debug.Log("======================================");
        }

        if (correct)
        {
            IsSolved = true; // ✅ NEW: mark solved

            if (playOnce && successPlayed)
            {
                if (debugCompareLog) Debug.Log($"[{name}] Already played once, skip.");
                return;
            }

            successPlayed = true;

            // show (if hidden) and play
            SetParticlesActive(true);
            PlaySuccessParticles();
        }
        else
        {
            IsSolved = false; // ✅ NEW: mark unsolved if wrong

            // wrong -> allow correcting later to play again
            if (playOnce) successPlayed = false;

            // keep hidden/stopped if wrong
            if (hideParticlesUntilSuccess)
                SetParticlesActive(false);
            else
                StopSuccessParticles(clear: true);
        }
    }

    void PlaySuccessParticles()
    {
        if (debugCompareLog) Debug.Log($"[{name}] ✅ Playing success particles!");

        if (successParticles == null) return;

        foreach (var ps in successParticles)
        {
            if (!ps) continue;

            // make sure it is active
            if (!ps.gameObject.activeInHierarchy)
                ps.gameObject.SetActive(true);

            // force restart so you can SEE it trigger
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    void StopSuccessParticles(bool clear)
    {
        if (successParticles == null) return;

        foreach (var ps in successParticles)
        {
            if (!ps) continue;

            if (clear)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void SetParticlesActive(bool active)
    {
        if (successParticles == null) return;

        foreach (var ps in successParticles)
        {
            if (!ps) continue;
            ps.gameObject.SetActive(active);
        }
    }
}
