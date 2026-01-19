using System.Collections.Generic;
using UnityEngine;

public class AnswerDoor : MonoBehaviour
{
    [Header("Trigger & Interact")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("Hint UI (World Space)")]
    public GameObject hintCanvas;

    [Header("Answer UI Text (Shown on Screen Space AnswerCanvas)")]
    [TextArea(4, 10)]
    public string uiText =
        "Do you want to choose this door?\n\n" +
        "7 - Proceed\n" +
        "8 - Exit";

    [Header("Door Result")]
    public bool isCorrectDoor = true;

    [Header("Correct Door - Animator")]
    public Animator doorAnimator;
    public string openTriggerName = "AnswerDoorOpen";

    [Header("Wrong Door - Destroy Target")]
    public GameObject objectToDestroy;

    [Header("Lock After Proceed")]
    public bool lockAfterProceed = true;

    [Header("Compass Integration")]
    public string doorMarkerToHide = "answer_door";
    public bool dontShowNextMarker = true;
    public Compass compass;

    // ✅ NEW: UI to disable while Answer UI is showing
    [Header("Optional UI Lock (Hide While Answer UI Showing)")]
    [Tooltip("Drag any UI roots / Canvases you want to disable while the Answer UI is open.")]
    public List<GameObject> uiToDisableWhileAnswerShowing = new List<GameObject>();

    // Cache original active states
    private readonly Dictionary<GameObject, bool> initialUIStates = new Dictionary<GameObject, bool>();

    private bool hasProceeded = false;
    private bool playerInside = false;

    private void Start()
    {
        SetHint(false);

        // Auto-find compass
        if (compass == null)
            compass = FindObjectOfType<Compass>();

        CacheInitialUIStates();
    }

    void CacheInitialUIStates()
    {
        initialUIStates.Clear();

        if (uiToDisableWhileAnswerShowing == null) return;

        foreach (var go in uiToDisableWhileAnswerShowing)
        {
            if (!go) continue;
            if (!initialUIStates.ContainsKey(go))
                initialUIStates.Add(go, go.activeSelf);
        }
    }

    void SetOtherUIActive(bool active)
    {
        if (uiToDisableWhileAnswerShowing == null) return;

        foreach (var go in uiToDisableWhileAnswerShowing)
        {
            if (!go) continue;

            if (active)
            {
                // Restore only if it was originally active
                if (initialUIStates.TryGetValue(go, out bool wasActive) && wasActive)
                    go.SetActive(true);
            }
            else
            {
                go.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!playerInside) return;
        if (AnswerUIManager.Instance == null) return;

        if (hasProceeded && lockAfterProceed)
        {
            SetHint(false);
            return;
        }

        if (AnswerUIManager.Instance.IsShowing)
        {
            SetHint(false);
            return;
        }

        SetHint(true);

        if (Input.GetKeyDown(interactKey))
        {
            // ✅ Disable other UI before showing Answer UI
            SetOtherUIActive(false);

            AnswerUIManager.Instance.Show(this);
            SetHint(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;

        if (compass != null && !string.IsNullOrEmpty(doorMarkerToHide))
        {
            compass.HideMarker(doorMarkerToHide);
            Debug.Log($"[AnswerDoor] Player reached door, hiding marker: {doorMarkerToHide}");
        }

        if (!(hasProceeded && lockAfterProceed))
            SetHint(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        SetHint(false);
    }

    private void SetHint(bool show)
    {
        if (hintCanvas) hintCanvas.SetActive(show);
    }

    public void ConfirmProceed()
    {
        if (hasProceeded && lockAfterProceed) return;
        hasProceeded = true;
        SetHint(false);

        if (isCorrectDoor)
        {
            if (doorAnimator != null && !string.IsNullOrEmpty(openTriggerName))
            {
                doorAnimator.ResetTrigger(openTriggerName);
                doorAnimator.SetTrigger(openTriggerName);
            }
            else
            {
                Debug.LogWarning($"{name}: Correct door missing Animator or trigger name.");
            }
        }
        else
        {
            if (objectToDestroy != null)
            {
                var rev = objectToDestroy.GetComponent<RevertibleObject>();
                if (rev != null) rev.FakeDestroy();
                else objectToDestroy.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"{name}: Wrong door missing objectToDestroy.");
            }
        }

        // ✅ Restore other UI after decision is made
        SetOtherUIActive(true);
    }
}
