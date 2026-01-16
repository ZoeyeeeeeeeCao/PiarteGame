using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

public class CutsceneController : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector cutsceneDirector;
    public GameObject mainCamera;
    public GameObject Player;

    [Header("Events")]
    [Tooltip("Triggered the moment the cutscene begins.")]
    public UnityEvent onCutsceneStart;

    [Tooltip("Triggered the moment the cutscene ends.")]
    public UnityEvent onCutsceneFinished;

    private void Start()
    {
        // Setup initial state: Cutscene camera off
        if (mainCamera != null)
            mainCamera.SetActive(false);
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

    /// <summary>
    /// Call this function to start everything.
    /// </summary>
    public void ActivateSequence()
    {
        // 1. Setup Camera/Player states
        if (mainCamera != null) mainCamera.SetActive(true);
        if (Player != null) Player.SetActive(false);

        // 2. Trigger the Start Event
        if (onCutsceneStart != null)
        {
            onCutsceneStart.Invoke();
        }

        // 3. Play the Timeline
        if (cutsceneDirector != null)
        {
            cutsceneDirector.Play();
        }

        Debug.Log("Cutscene Started: onCutsceneStart event invoked.");
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director == cutsceneDirector)
        {
            // 1. Reset Camera and Player states
            if (Player != null) Player.SetActive(true);
            if (mainCamera != null) mainCamera.SetActive(false);

            // 2. Trigger the Finished Event (where your Fog/Wind should be)
            if (onCutsceneFinished != null)
            {
                onCutsceneFinished.Invoke();
            }

            Debug.Log("Cutscene Finished: onCutsceneFinished event invoked.");
        }
    }
}