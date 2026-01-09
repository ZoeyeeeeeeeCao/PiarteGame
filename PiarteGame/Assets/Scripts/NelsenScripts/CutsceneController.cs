using UnityEngine;
using UnityEngine.Playables;

public class CutsceneController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameObject containing the PlayableDirector component.")]
    public PlayableDirector cutsceneDirector;

    [Tooltip("The main gameplay camera (e.g., Third Person Camera) to disable during the cutscene.")]
    //public GameObject thirdPersonCamera;
    public GameObject mainCamera;
    public GameObject Player;

    [SerializeField] public WindTurbineController windController;

    private void Start()
    {
        mainCamera.SetActive(false);
}
    private void OnEnable()
    {
        // Subscribe to the stopped event to know when the cutscene ends
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped += OnCutsceneFinished;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnCutsceneFinished;
        }
    }

    /// <summary>
    /// Call this function from a trigger or another script to start the cutscene.
    /// </summary>
    public void ActivateSequence()
    {
        //if (cutsceneDirector == null || thirdPersonCamera == null)
        //{
        //    Debug.LogWarning("CutsceneController: Missing references!");
        //    return;
        //}

        // 1. Hide the gameplay camera
        mainCamera.SetActive(true);
        Player.SetActive(false);
        //thirdPersonCamera.SetActive(false);


        // 2. Play the Timeline
        cutsceneDirector.Play();

        Debug.Log("Cutscene Started: Gameplay camera disabled.");
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        // Ensure we are reacting to the correct director
        if (director == cutsceneDirector)
        {
            // 3. Reactivate the gameplay camera when the timeline ends
            Player.SetActive(true);
            mainCamera.SetActive(false);
            //thirdPersonCamera.SetActive(true);
            windController.ActivateWind();
            Debug.Log("Cutscene Finished: Gameplay camera enabled.");
        }
    }
}