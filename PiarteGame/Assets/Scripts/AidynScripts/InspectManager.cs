using UnityEngine;

public class InspectManager : MonoBehaviour
{
    public Transform spawnPoint;
    public Camera inspectCamera;

    GameObject current;

    public void Show(GameObject collectiblePrefab)
    {
        Hide();

        current = Instantiate(collectiblePrefab, spawnPoint.position, spawnPoint.rotation);
        current.layer = LayerMask.NameToLayer("Inspect");
        foreach (Transform t in current.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = LayerMask.NameToLayer("Inspect");

        var rot = current.GetComponent<InspectRotator>();
        if (rot == null) rot = current.AddComponent<InspectRotator>();
        rot.inspectCamera = inspectCamera;

        Time.timeScale = 0f; // optional pause
        // also disable player input here
    }

    public void Hide()
    {
        if (current != null) Destroy(current);
        current = null;

        Time.timeScale = 1f;
        // re-enable player input here
    }
}
