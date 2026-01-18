using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

public class CutsceneController : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector cutsceneDirector;

    [Header("Cutscene START")]
    public GameObject[] hideOnCutsceneStart;
    public GameObject[] showOnCutsceneStart;

    [Header("Cutscene END")]
    public GameObject[] hideOnCutsceneEnd;
    public GameObject[] showOnCutsceneEnd;

    [Header("Events")]
    public UnityEvent onCutsceneStart;
    public UnityEvent onCutsceneFinished;

    private void Start()
    {
        // Ensure start-only objects are disabled initially
        SetActiveArray(showOnCutsceneStart, false);
    }

    private void OnEnable()
    {
        if (cutsceneDirector != null)
            cutsceneDirector.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        if (cutsceneDirector != null)
            cutsceneDirector.stopped -= OnDirectorStopped;
    }

    public void ActivateSequence()
    {
        // ----- CUTSCENE START -----
        SetActiveArray(hideOnCutsceneStart, false);
        SetActiveArray(showOnCutsceneStart, true);

        onCutsceneStart?.Invoke();
        cutsceneDirector?.Play();

        Debug.Log("Cutscene Started");
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director != cutsceneDirector) return;

        // ----- CUTSCENE END -----
        SetActiveArray(hideOnCutsceneEnd, false);
        SetActiveArray(showOnCutsceneEnd, true);

        onCutsceneFinished?.Invoke();

        Debug.Log("Cutscene Finished");
    }

    private void SetActiveArray(GameObject[] objects, bool state)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}
