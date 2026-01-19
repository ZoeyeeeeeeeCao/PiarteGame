using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldSpaceSceneTransitionPromptLevel3 : MonoBehaviour
{
    [Header("Prompt UI")]
    [SerializeField] private GameObject root;          // canvas root (or same object)
    [SerializeField] private Transform lookAtCamera;   // optional; auto uses Camera.main

    [Header("Transition Settings")]
    public string sceneToLoad;
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("If true, loads immediately when player enters trigger (like your example). If false, requires pressing E.")]
    public bool autoLoadOnEnter = false;

    [Header("Debug")]
    public bool debugLog;

    private bool playerInRange;
    private bool hasTriggered;

    private void Reset()
    {
        // Make collider a trigger automatically
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        if (root == null) root = gameObject;
        SetVisible(false);

        // Ensure trigger
        var col = GetComponent<Collider>();
        if (col && !col.isTrigger) col.isTrigger = true;
    }

    private void Update()
    {
        if (hasTriggered) return;

        // Press-to-load mode
        if (!autoLoadOnEnter && playerInRange && Input.GetKeyDown(interactKey))
        {
            TriggerLoad();
        }
    }

    private void LateUpdate()
    {
        if (root == null || !root.activeSelf) return;

        var cam = lookAtCamera != null ? lookAtCamera : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return;

        // Billboard: face camera
        Vector3 dir = root.transform.position - cam.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        root.transform.rotation = Quaternion.LookRotation(dir);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            SetVisible(true);

            if (debugLog) Debug.Log("[WorldSpaceTransition] Player entered trigger.");

            // Auto-load mode (like your SceneTransitionTrigger example)
            if (autoLoadOnEnter)
            {
                TriggerLoad();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            SetVisible(false);

            if (debugLog) Debug.Log("[WorldSpaceTransition] Player left trigger.");
        }
    }

    private void TriggerLoad()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        SetVisible(false);

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[WorldSpaceTransition] sceneToLoad is empty.");
            return;
        }

        if (SceneLoaderHoward.Instance != null)
        {
            if (debugLog) Debug.Log($"[WorldSpaceTransition] Loading scene: {sceneToLoad}");
            SceneLoaderHoward.Instance.LoadLevel(sceneToLoad);
        }
        else
        {
            Debug.LogError("[WorldSpaceTransition] SceneLoaderHoward Instance not found! Check your GameManager.");
        }
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
