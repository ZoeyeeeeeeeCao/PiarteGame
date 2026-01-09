using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events; // Required for UnityEvent

public class CutsceneController : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector cutsceneDirector;
    public GameObject mainCamera;
    public GameObject Player;

    [Header("Controllers to Activate")]
    public WindTurbineController windController;
    public FogController fogController;

    [Header("Events")]
    [Tooltip("Actions to trigger exactly when the cutscene ends.")]
    public UnityEvent onCutsceneFinished;

    private void Start()
    {
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

    public void ActivateSequence()
    {
        mainCamera.SetActive(true);
        Player.SetActive(false);
        cutsceneDirector.Play();
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director == cutsceneDirector)
        {
            // 1. Standard Logic
            Player.SetActive(true);
            mainCamera.SetActive(false);

            if (windController != null) windController.ActivateWind();
            if (fogController != null) fogController.ActivateFog();

            // 2. Trigger the Custom Event
            if (onCutsceneFinished != null)
            {
                onCutsceneFinished.Invoke();
            }

            Debug.Log("Cutscene Ended: Wind, Fog, and Custom Events triggered.");
        }
    }
}