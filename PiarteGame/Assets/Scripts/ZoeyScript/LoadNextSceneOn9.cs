using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadNextSceneOn9 : MonoBehaviour
{
    [Tooltip("Scene name to load when pressing key 9")]
    public string sceneToLoad;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
