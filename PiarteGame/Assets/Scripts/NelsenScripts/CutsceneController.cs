using UnityEngine;
using UnityEngine.Playables;

public class CutsceneController : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector cutsceneDirector;
    public GameObject mainCamera;
    public GameObject Player;

    [Header("Controllers to Activate")]
    public WindTurbineController windController;
    public FogController fogController; // <--- Drag your Fog GameObject here

    private void Start()
    {
        mainCamera.SetActive(false);
    }

    private void OnEnable()
    {
        if (cutsceneDirector != null)
            cutsceneDirector.stopped += OnCutsceneFinished;
    }

    private void OnDisable()
    {
        if (cutsceneDirector != null)
            cutsceneDirector.stopped -= OnCutsceneFinished;
    }

    public void ActivateSequence()
    {
        mainCamera.SetActive(true);
        Player.SetActive(false);
        cutsceneDirector.Play();
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        if (director == cutsceneDirector)
        {
            Player.SetActive(true);
            mainCamera.SetActive(false);

            // 1. Activate Wind
            if (windController != null) windController.ActivateWind();

            // 2. Activate Fog
            if (fogController != null) fogController.ActivateFog();

            Debug.Log("Cutscene Ended: Wind and Fog activated.");
        }
    }
}