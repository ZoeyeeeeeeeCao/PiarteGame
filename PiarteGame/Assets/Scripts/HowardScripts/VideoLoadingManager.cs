using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoLoadingManager : MonoBehaviour
{
    [Header("Settings")]
    public VideoPlayer videoPlayer;
    public string sceneToLoad = "Level2"; // Name of your next level

    void Start()
    {
        if (videoPlayer != null)
        {
            // Prepare the video and wait for it to be ready
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += (vp) =>
            {
                vp.Play();
                StartCoroutine(LoadNextSceneRoutine());
            };
        }
    }

    IEnumerator LoadNextSceneRoutine()
    {
        // Start loading the scene in the background
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);

        // Don't let the scene switch yet
        operation.allowSceneActivation = false;

        // Wait until the video is finished
        while (videoPlayer.isPlaying)
        {
            yield return null;
        }

        // Optional: Wait for the scene to actually finish loading 
        // (operation.progress reaches 0.9 when it's ready)
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // Finally switch scenes
        operation.allowSceneActivation = true;
    }
}