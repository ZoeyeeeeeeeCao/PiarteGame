using UnityEngine;
using UnityEngine.SceneManagement;

public class skipsceneaidyn : MonoBehaviour
{
    [Header("Cheat Key")]
    [SerializeField] private KeyCode skipKey = KeyCode.Alpha8;

    [Header("Optional: if empty, loads next build index")]
    [SerializeField] private string nextSceneName = "";

    private void Update()
    {
        if (Input.GetKeyDown(skipKey))
        {
            SkipScene();
        }
    }

    private void SkipScene()
    {
        // If a scene name is provided, use it
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        // Otherwise load next scene in Build Settings
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("skipsceneaidyn: No next scene in Build Settings.");
            return;
        }

        SceneManager.LoadScene(nextIndex);
    }
}
